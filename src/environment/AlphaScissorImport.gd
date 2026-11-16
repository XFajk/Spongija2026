@tool
extends EditorScenePostImport
## Import script for foliage GLBs (set as Import Script in their import settings).
## glTF BLEND materials import as Alpha + Depth Pre-Pass; this turns them into Alpha Scissor,
## so a leaf's pixels are either solid or gone and nothing has to blend.

const THRESHOLD := 0.5


func _post_import(scene: Node) -> Object:
	var meshes := scene.find_children("*", "MeshInstance3D", true, false)
	if scene is MeshInstance3D:
		meshes.append(scene)

	for mesh_instance: MeshInstance3D in meshes:
		if mesh_instance.mesh == null:
			continue
		for surface in mesh_instance.mesh.get_surface_count():
			var material := mesh_instance.mesh.surface_get_material(surface) as BaseMaterial3D
			if material != null and material.transparency != BaseMaterial3D.TRANSPARENCY_DISABLED:
				material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA_SCISSOR
				material.alpha_scissor_threshold = THRESHOLD

	return scene
