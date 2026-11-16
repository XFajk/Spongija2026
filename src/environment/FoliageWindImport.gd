@tool
extends EditorScenePostImport
## Import script for foliage GLBs that sway in the wind (set as Import Script in their import
## settings). Swaps their materials for ShaderMaterials keeping texture and roughness, and gives
## them the mesh's size so the tips sway the most. See-through surfaces get foliage.gdshader.
## Solid surfaces get tree_bark.gdshader, with the same bend, so leaves stay on their branches;
## a mesh with a solid surface counts as a tree and sways less.

const LEAVES_SHADER_PATH := "res://src/shaders/foliage.gdshader"
const BARK_SHADER_PATH := "res://src/shaders/tree_bark.gdshader"
const WIND_NOISE_PATH := "res://assets/terrain/foliage/wind_noise.tres"
const ALPHA_THRESHOLD := 0.5
const FERN_AMOUNT := 0.6
const TREE_AMOUNT := 0.3
const TREE_LEAF_FLUTTER := 0.04


func _post_import(scene: Node) -> Object:
	var meshes := scene.find_children("*", "MeshInstance3D", true, false)
	if scene is MeshInstance3D:
		meshes.append(scene)

	for mesh_instance: MeshInstance3D in meshes:
		if mesh_instance.mesh != null:
			_convert_mesh(mesh_instance.mesh)

	return scene


func _convert_mesh(mesh: Mesh) -> void:
	var radius := _get_radius(mesh)
	var is_tree := _has_solid_surface(mesh)
	for surface in mesh.get_surface_count():
		var source := mesh.surface_get_material(surface) as BaseMaterial3D
		if source == null:
			continue
		var material := ShaderMaterial.new()
		var solid := source.transparency == BaseMaterial3D.TRANSPARENCY_DISABLED
		material.shader = load(BARK_SHADER_PATH if solid else LEAVES_SHADER_PATH)
		material.set_shader_parameter("albedo_texture", source.albedo_texture)
		material.set_shader_parameter("roughness", source.roughness)
		material.set_shader_parameter("foliage_radius", radius)
		material.set_shader_parameter("wind_noise", load(WIND_NOISE_PATH))
		material.set_shader_parameter("amount", TREE_AMOUNT if is_tree else FERN_AMOUNT)
		if not solid:
			material.set_shader_parameter("alpha_threshold", ALPHA_THRESHOLD)
			material.set_shader_parameter("leaf_flutter", TREE_LEAF_FLUTTER if is_tree else 0.0)
		mesh.surface_set_material(surface, material)


func _get_radius(mesh: Mesh) -> float:
	var radius := 0.0
	for surface in mesh.get_surface_count():
		for vertex: Vector3 in mesh.surface_get_arrays(surface)[Mesh.ARRAY_VERTEX]:
			radius = maxf(radius, vertex.length())
	return radius


func _has_solid_surface(mesh: Mesh) -> bool:
	for surface in mesh.get_surface_count():
		var material := mesh.surface_get_material(surface) as BaseMaterial3D
		if material != null and material.transparency == BaseMaterial3D.TRANSPARENCY_DISABLED:
			return true
	return false
