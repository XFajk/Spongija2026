@tool
extends Node3D

@export var player: CharacterBody3D
@export var objects: Array[Node3D] ## Each group should contain 2+ MultiMeshInstance3D LODs and optionally a StaticBody3D collision template.
@export var coll_script: Script = preload("res://src/environment/RangedCollisions.gd")

@export var collisions_enabled: bool = true:
	set(value):
		collisions_enabled = value
		if not value:
			_clear_spawned_collisions()

# Toggle this property in the Inspector to regenerate the editor preview.
@export var regenerate_in_editor: bool = false:
	set(value):
		regenerate_in_editor = false
		if value and Engine.is_editor_hint():
			call_deferred("_editor_regenerate")

@export var obj_ammount: int = 1000
@export var max_rotation: Vector3 = Vector3(0, 180, 0)
@export var scale_min: float = 0.8
@export var scale_max: float = 1.3
@export var random_offset_max: float = 6.0
@export var z_offset: float = -0.15
@export var close_correction_range: float = 1.5
@export_range(1, 200, 1) var max_placement_attempts: int = 30

@export var lod_0_end: float = 50.0
@export var lod_1_end: float = 180.0
@export var lod_2_end: float = 500.0
@export var collision_generation_range: float = 15.0

@export var terrain: Terrain3D
@export var placement_map: Image
@export var scaling: float = 4.57
@export var image_offset_x: float = 0.0
@export var image_offset_z: float = 0.0

var white_pixel_positions: Array[Vector2] = []
var unpacked_groups: Array = []
# Each item: [group_name, instance_index, Transform3D]
var used_positions: Array = []
# Each dictionary entry: group_name -> [[instance_index, Transform3D], ...]
var grouped_data: Dictionary = {}
var group_names: Array[String] = []
var collision_shapes: Dictionary = {}
var generated: bool = false
var data_loaded: bool = false
var all_collisions: bool = false
var failed_objects: int = 0
var ray_cast: RayCast3D
var spawned_collision_ids: Dictionary = {}

signal all_loaded

func _ready() -> void:
	randomize()
	if Engine.is_editor_hint():
		# Editor preview is intentionally static: no player/LOD or runtime collision bodies needed.
		call_deferred("_editor_initialize")
		set_process(false)
		return

	_initialize_scatter()
	set_process(true)

func _editor_initialize() -> void:
	if not is_inside_tree():
		return
	if placement_map == null or terrain == null or terrain.data == null:
		return
	if not generated or grouped_data.is_empty():
		_generate_scatter_data()
	_prepare_multimeshes()
	_apply_editor_preview()

func _editor_regenerate() -> void:
	if not Engine.is_editor_hint() or not is_inside_tree():
		return
	_clear_spawned_collisions()
	generated = false
	grouped_data.clear()
	used_positions.clear()
	_generate_scatter_data()
	_prepare_multimeshes()
	_apply_editor_preview()

func _initialize_scatter() -> void:
	unpacked_groups = unpack_groups(objects)

	if not generated or grouped_data.is_empty():
		_generate_scatter_data()
	else:
		for group in objects:
			if not group_names.has(group.name):
				group_names.append(group.name)

	unpacked_groups = unpack_groups(objects)
	_prepare_multimeshes()

	# Keep source collision templates out of the way at runtime only.
	for key in collision_shapes:
		if is_instance_valid(collision_shapes[key]):
			collision_shapes[key].global_position = Vector3(0, -10000, 0)

func _generate_scatter_data() -> void:
	if placement_map == null:
		push_error("Scatter: Assign a placement_map Image in the Inspector.")
		return
	if terrain == null or terrain.data == null:
		push_error("Scatter: Assign your Terrain3D node and ensure its data is loaded.")
		return

	load_placement_map()
	if white_pixel_positions.is_empty():
		push_error("Scatter: Placement map contains no nearly-white pixels.")
		return

	group_names.clear()
	used_positions.clear()
	failed_objects = 0
	unpacked_groups = unpack_groups(objects)
	for group in objects:
		group_names.append(group.name)
	for group in unpacked_groups:
		spawn_objects(group)
	grouped_data = group_data(used_positions)
	generated = true

func _prepare_multimeshes() -> void:
	unpacked_groups = unpack_groups(objects)
	for group in unpacked_groups:
		var mesh_group: Array = group[0]
		var group_name: String = group[1]
		var data: Array = grouped_data.get(group_name, [])
		var needed_count := 0
		for entry in data:
			needed_count = max(needed_count, int(entry[0]) + 1)
		for mmi in mesh_group:
			if mmi.multimesh == null:
				push_error("Scatter: A MultiMeshInstance3D has no MultiMesh: " + str(mmi.get_path()))
				continue
			mmi.multimesh.transform_format = MultiMesh.TRANSFORM_3D
			mmi.multimesh.instance_count = max(obj_ammount, needed_count)

func _apply_editor_preview() -> void:
	# Show LOD 0 in the editor so the scatter is visible without running the game.
	for group in unpacked_groups:
		var mesh_group: Array = group[0]
		var data: Array = grouped_data.get(group[1], [])
		for entry in data:
			var index: int = int(entry[0])
			var instance_transform: Transform3D = entry[1]
			for lod_index in range(mesh_group.size()):
				var mmi: MultiMeshInstance3D = mesh_group[lod_index]
				if mmi.multimesh == null or index >= mmi.multimesh.instance_count:
					continue
				if lod_index == 0:
					mmi.multimesh.set_instance_transform(index, instance_transform)
				else:
					mmi.multimesh.set_instance_transform(index, Transform3D(instance_transform.basis, Vector3(0, -10000, 0)))

func _clear_spawned_collisions() -> void:
	for key in spawned_collision_ids.keys():
		var body = spawned_collision_ids[key]
		if is_instance_valid(body):
			body.queue_free()
	spawned_collision_ids.clear()

func _process(_delta: float) -> void:
	if Engine.is_editor_hint() or player == null:
		return
	for group in unpacked_groups:
		calculate_lods(group, grouped_data.get(group[1], []))
	if all_collisions and collisions_enabled:
		all_loaded.emit()

func load_placement_map() -> void:
	white_pixel_positions.clear()
	if placement_map == null:
		return

	for x in range(placement_map.get_width()):
		for y in range(placement_map.get_height()):
			var color := placement_map.get_pixel(x, y)
			if color.r >= 0.95 and color.g >= 0.95 and color.b >= 0.95:
				white_pixel_positions.append(Vector2(x, y))

func get_random_point() -> Vector2:
	if white_pixel_positions.is_empty():
		return Vector2.ZERO
	return white_pixel_positions[randi_range(0, white_pixel_positions.size() - 1)]

func get_placement_point(at_position: Vector2) -> Variant:
	if terrain == null or terrain.data == null:
		return null

	# Terrain3DData.get_height() expects a GLOBAL X/Z position.
	# at_position.x is world X and at_position.y is world Z.
	var world_position := Vector3(at_position.x, 0.0, at_position.y)
	var height: float = terrain.data.get_height(world_position)

	# Terrain3D returns NAN outside loaded regions or inside terrain holes.
	if is_nan(height) or is_inf(height):
		return null

	return Vector3(at_position.x, height, at_position.y)

func unpack_groups(input_objects: Array) -> Array:
	var result: Array = []
	collision_shapes.clear()

	for group in input_objects:
		var mesh_group: Array = []
		for node in group.get_children():
			if node is MultiMeshInstance3D:
				mesh_group.append(node)
			elif node is StaticBody3D:
				collision_shapes[group.name] = node

		if mesh_group.size() < 2:
			push_warning("Scatter group '%s' should contain at least 2 MultiMeshInstance3D LODs." % group.name)
		result.append([mesh_group, group.name])
	return result

func group_data(positions: Array) -> Dictionary:
	var result: Dictionary = {}
	for group_name in group_names:
		result[group_name] = []

	for used_position in positions:
		var name: String = used_position[0]
		if not result.has(name):
			result[name] = []
		result[name].append([used_position[1], used_position[2]])
	return result

func spawn_objects(group: Array) -> void:
	var mesh_group: Array = group[0]
	var group_name: String = group[1]

	if mesh_group.is_empty():
		push_warning("Scatter group '%s' has no MultiMeshInstance3D nodes." % group_name)
		return

	for mmi in mesh_group:
		if mmi.multimesh == null:
			push_error("Scatter: MultiMeshInstance3D has no MultiMesh: " + str(mmi.get_path()))
			return
		mmi.multimesh.transform_format = MultiMesh.TRANSFORM_3D
		mmi.multimesh.instance_count = obj_ammount

	var group_failed := 0

	for i in range(obj_ammount):
		var accepted := false
		var placing_position := Vector3.ZERO

		# Each retry chooses a NEW white map pixel. Previously retries only
		# nudged the same pixel, so they could repeatedly sample a bad spot.
		for attempt in range(max_placement_attempts):
			var pixel: Vector2 = get_random_point()
			pixel -= Vector2(
				placement_map.get_width() / 2.0,
				placement_map.get_height() / 2.0
			)
			pixel *= scaling
			pixel += Vector2(image_offset_x, image_offset_z)
			pixel += Vector2(
				randf_range(-random_offset_max, random_offset_max),
				randf_range(-random_offset_max, random_offset_max)
			)

			var hit_position = get_placement_point(pixel)
			if hit_position == null:
				continue

			var candidate: Vector3 = hit_position
			candidate.y += z_offset

			# Compare horizontal X/Z distance only, not 3D distance.
			# A tree on a slope should still respect the same spacing.
			var too_close := false
			var minimum_distance_sq := close_correction_range * close_correction_range
			for used_position in used_positions:
				var existing: Vector3 = used_position[2].origin
				var dx := candidate.x - existing.x
				var dz := candidate.z - existing.z
				if dx * dx + dz * dz < minimum_distance_sq:
					too_close = true
					break

			if too_close:
				continue

			placing_position = candidate
			accepted = true
			break

		if not accepted:
			group_failed += 1
			failed_objects += 1
			_hide_instance(mesh_group, i, Basis.IDENTITY)
			continue

		var rotation := Vector3(
			deg_to_rad(randf_range(-max_rotation.x, max_rotation.x)),
			deg_to_rad(randf_range(-max_rotation.y, max_rotation.y)),
			deg_to_rad(randf_range(-max_rotation.z, max_rotation.z))
		)
		var scale_multiplier := randf_range(scale_min, scale_max)
		var basis := Basis.IDENTITY
		basis = basis.rotated(Vector3.UP, rotation.y)
		basis = basis.rotated(Vector3.RIGHT, rotation.x)
		basis = basis.rotated(Vector3.FORWARD, rotation.z)
		basis = basis.scaled(Vector3.ONE * scale_multiplier)

		var instance_transform := Transform3D(basis, placing_position)
		# Hide until calculate_lods selects the correct LOD.
		_hide_instance(mesh_group, i, basis)
		used_positions.append([group_name, i, instance_transform])

	print("Scatter group '%s': placed %d / %d; failed %d" % [
		group_name,
		obj_ammount - group_failed,
		obj_ammount,
		group_failed
	])

func _hide_instance(mesh_group: Array, index: int, basis: Basis) -> void:
	var hidden_transform := Transform3D(basis, Vector3(0, -10000, 0))
	for mmi in mesh_group:
		if mmi.multimesh != null and index < mmi.multimesh.instance_count:
			mmi.multimesh.set_instance_transform(index, hidden_transform)

func calculate_lods(group: Array, data: Array) -> void:
	var mesh_group: Array = group[0]
	var group_name: String = group[1]
	if mesh_group.is_empty() or player == null:
		return

	var lod0_sq := lod_0_end * lod_0_end
	var lod1_sq := lod_1_end * lod_1_end
	var lod2_sq := lod_2_end * lod_2_end
	var collision_range_sq := collision_generation_range * collision_generation_range

	for position_data in data:
		if position_data.size() < 2:
			continue

		var index: int = int(position_data[0])
		var saved_transform: Transform3D = position_data[1]
		var saved_position := saved_transform.origin
		var distance_sq := player.global_position.distance_squared_to(saved_position)
		var hidden_transform := Transform3D(saved_transform.basis, Vector3(0, -10000, 0))

		# Hide this instance in every LOD first, then show it in one appropriate LOD.
		for mmi in mesh_group:
			if mmi.multimesh == null or index < 0 or index >= mmi.multimesh.instance_count:
				continue
			mmi.multimesh.set_instance_transform(index, hidden_transform)

		var selected_lod := -1
		if distance_sq < lod0_sq:
			selected_lod = 0
		elif distance_sq < lod1_sq:
			selected_lod = 1
		elif distance_sq < lod2_sq:
			selected_lod = 2

		if selected_lod >= 0 and selected_lod < mesh_group.size():
			var selected_mmi: MultiMeshInstance3D = mesh_group[selected_lod]
			if selected_mmi.multimesh != null and index < selected_mmi.multimesh.instance_count:
				selected_mmi.multimesh.set_instance_transform(index, saved_transform)

		# Generate each collision body at most once per group/instance.
		var collision_key := "%s:%d" % [group_name, index]
		if not Engine.is_editor_hint() and collisions_enabled and (distance_sq < collision_range_sq or all_collisions) and not spawned_collision_ids.has(collision_key):
			if collision_shapes.has(group_name):
				var collision_body: StaticBody3D = collision_shapes[group_name].duplicate() as StaticBody3D
				collision_body.transform = saved_transform
				if coll_script != null:
					collision_body.set_script(coll_script)
				collision_body.set("player", player)
				collision_body.set("range_squared", collision_range_sq)
				add_child(collision_body)
				spawned_collision_ids[collision_key] = collision_body

func saveout() -> Dictionary:
	return {"grouped_data": grouped_data}

func loadin(save_data: Dictionary) -> void:
	generated = true
	grouped_data = save_data.get("grouped_data", {})
	group_names.clear()
	for group in objects:
		group_names.append(group.name)

	# This expects saved data to contain actual Transform3D values, not strings.
	for key in group_names:
		if not grouped_data.has(key):
			grouped_data[key] = []
	data_loaded = true
