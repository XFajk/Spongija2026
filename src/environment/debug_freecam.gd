
extends CharacterBody3D

@export var move_speed: float = 10.0
@export var min_speed: float = 1.0
@export var max_speed: float = 200.0
@export var speed_step: float = 2.0
@export var mouse_sensitivity: float = 0.002

@onready var cam: Camera3D = $Camera3D

var mouse_captured := true


func _ready() -> void:
	cam.current = true
	Input.mouse_mode = Input.MOUSE_MODE_CAPTURED


func _unhandled_input(event: InputEvent) -> void:
	# Look around with the mouse
	if event is InputEventMouseMotion and mouse_captured:
		rotate_y(-event.relative.x * mouse_sensitivity)

		cam.rotate_x(-event.relative.y * mouse_sensitivity)
		cam.rotation.x = clamp(
			cam.rotation.x,
			deg_to_rad(-89.0),
			deg_to_rad(89.0)
		)

	# Change speed using the scroll wheel
	if event is InputEventMouseButton and event.pressed:
		if event.button_index == MOUSE_BUTTON_WHEEL_UP:
			move_speed = min(move_speed + speed_step, max_speed)

		elif event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
			move_speed = max(move_speed - speed_step, min_speed)

		# Re-capture the mouse with a click
		elif event.button_index == MOUSE_BUTTON_LEFT:
			mouse_captured = true
			Input.mouse_mode = Input.MOUSE_MODE_CAPTURED

	# Release mouse with Escape
	if event is InputEventKey and event.pressed:
		if event.keycode == KEY_ESCAPE:
			mouse_captured = false
			Input.mouse_mode = Input.MOUSE_MODE_VISIBLE


func _physics_process(delta: float) -> void:
	var input_dir := Vector3.ZERO

	# Movement relative to the camera's horizontal facing direction
	var forward := -global_transform.basis.z
	var right := global_transform.basis.x

	if Input.is_key_pressed(KEY_W):
		input_dir += forward
	if Input.is_key_pressed(KEY_S):
		input_dir -= forward
	if Input.is_key_pressed(KEY_D):
		input_dir += right
	if Input.is_key_pressed(KEY_A):
		input_dir -= right

	# Vertical movement
	if Input.is_key_pressed(KEY_SPACE):
		input_dir += Vector3.UP
	if Input.is_key_pressed(KEY_CTRL):
		input_dir += Vector3.DOWN

	# Normalize so diagonal movement isn't faster
	if input_dir.length_squared() > 0.0:
		input_dir = input_dir.normalized()

	# Hold Shift for a temporary speed boost
	var current_speed := move_speed
	if Input.is_key_pressed(KEY_SHIFT):
		current_speed *= 3.0

	# Fly freely without gravity
	velocity = input_dir * current_speed
	global_position += velocity * delta
