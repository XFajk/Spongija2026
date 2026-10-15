using Godot;
using System;

public partial class LookAround : Node3D
{
	private CharacterBody3D _player;
	private float _pitch;

	[Export]
	public float MouseSensitivity = 0.1f;

	public override void _Ready()
	{
		_player = GetParent<CharacterBody3D>();
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _Input(InputEvent @event)
	{
		if (_player == null)
		{
			_player = GetParent<CharacterBody3D>();
		}

		if (@event.IsActionPressed("ui_cancel"))
		{
			if (Input.MouseMode == Input.MouseModeEnum.Captured)
			{
				Input.MouseMode = Input.MouseModeEnum.Visible;
			}
			else
			{
				Input.MouseMode = Input.MouseModeEnum.Captured;
			}

			return;
		}

		if (_player == null || @event is not InputEventMouseMotion mouseMotion)
		{
			return;
		}

		if (Input.MouseMode != Input.MouseModeEnum.Captured)
		{
			return;
		}

		float yaw = -mouseMotion.Relative.X * MouseSensitivity;
		_pitch -= mouseMotion.Relative.Y * MouseSensitivity;
		_pitch = Mathf.Clamp(_pitch, -89.0f, 89.0f);

		_player.RotateY(Mathf.DegToRad(yaw));
		RotationDegrees = new Vector3(_pitch, RotationDegrees.Y, RotationDegrees.Z);
	}
}
