@tool
extends EditorScenePostImport
## Import script for foliage GLBs that sway in the wind (set as Import Script in their import
## settings). Swaps the see-through materials for a ShaderMaterial on foliage.gdshader, keeping
## their texture and roughness, and gives it the mesh's size so the tips sway the most.

const SHADER_PATH := "res://src/shaders/foliage.gdshader"
const WIND_NOISE_PATH := "res://assets/terrain/foliage/wind_noise.tres"
const ALPHA_THRESHOLD := 0.5
const AMOUNT := 0.6


func _post_import(scene: Node) -> Object:
	var meshes := scene.find_children("*", "MeshInstance3D", true, false)
	if scene is MeshInstance3D:
		meshes.append(scene)

	for mesh_instance: MeshInstance3D in meshes:
		if mesh_instance.mesh == null:
			continue
		var radius := _get_radius(mesh_instance.mesh)
		for surface in mesh_instance.mesh.get_surface_count():
			var material := mesh_instance.mesh.surface_get_material(surface) as BaseMaterial3D
			if material != null and material.transparency != BaseMaterial3D.TRANSPARENCY_DISABLED:
				mesh_instance.mesh.surface_set_material(surface, _make_material(material, radius))

	return scene


func _get_radius(mesh: Mesh) -> float:
	var radius := 0.0
	for surface in mesh.get_surface_count():
		for vertex: Vector3 in mesh.surface_get_arrays(surface)[Mesh.ARRAY_VERTEX]:
			radius = maxf(radius, vertex.length())
	return radius


func _make_material(source: BaseMaterial3D, radius: float) -> ShaderMaterial:
	var material := ShaderMaterial.new()
	material.shader = load(SHADER_PATH)
	material.set_shader_parameter("albedo_texture", source.albedo_texture)
	material.set_shader_parameter("alpha_threshold", ALPHA_THRESHOLD)
	material.set_shader_parameter("roughness", source.roughness)
	material.set_shader_parameter("foliage_radius", radius)
	material.set_shader_parameter("wind_noise", load(WIND_NOISE_PATH))
	material.set_shader_parameter("amount", AMOUNT)
	return material
