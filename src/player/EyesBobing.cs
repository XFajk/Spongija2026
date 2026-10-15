using Godot;

public partial class EyesBobing : Camera3D
{
	private Node3D _head;

	[Export]
	public float RotationSmoothness = 8.0f;

	[Export]
	public float RollBobWalkAmplitude = 0.025f;
	[Export]
	public float RollBobRunAmplitude = 0.05f;

	[Export]
	public float RollBobWalkFrequency = 7.5f;
	[Export]
	public float RollBobRunFrequency = 10.0f;
	[Export]
	public float RollBobCrouchFrequency = 2.5f;

	private float _bobTime;

	private FpsController _fpsController;

	public override void _Ready()
	{
		_head = GetParent<Node3D>();
		_fpsController = _head.GetParent<FpsController>();
		TopLevel = true;
		GlobalPosition = _head.GlobalPosition;
		GlobalBasis = _head.GlobalBasis;
	}

	public override void _Process(double delta)
	{ 
		GlobalPosition = _head.GlobalPosition;

		float weight = 1.0f - Mathf.Exp(-RotationSmoothness * (float)delta);
		Quaternion targetRotation = _head.GlobalBasis.GetRotationQuaternion();

		if (_fpsController.GetMovementState() == FpsController.MovementState.Walking)
		{
			_bobTime += (float)delta * GetBobFrequency();
			float roll = Mathf.Sin(_bobTime) * GetRollBobAmplitude();
			Quaternion bobRotation = new Quaternion(Vector3.Forward, roll);
			targetRotation *= bobRotation;
		}

		// Continue following the player's look rotation when not walking.
		Quaternion = Quaternion.Slerp(targetRotation, weight);
	}

	public float GetBobFrequency()
	{
        switch (_fpsController.GetBodyState())
        {
            case FpsController.BodyState.Standing:
                return _fpsController.SprintKeyPressed() ? RollBobRunFrequency : RollBobWalkFrequency;
            case FpsController.BodyState.Crouching:
                return RollBobCrouchFrequency;
            default:
                return RollBobWalkFrequency;
        }
	}

	public float GetRollBobAmplitude()
	{
		switch (_fpsController.GetBodyState())
		{
			case FpsController.BodyState.Standing:
				return _fpsController.SprintKeyPressed() ? RollBobRunAmplitude : RollBobWalkAmplitude;
			case FpsController.BodyState.Crouching:
				return RollBobWalkAmplitude;
			default:
				return RollBobWalkAmplitude;
		}
	}


}
