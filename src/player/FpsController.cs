using Godot;

public partial class FpsController : CharacterBody3D
{
    public enum MovementState
    {
        Standing,
        Walking,
        Jumping,
        Falling,
    }

    public enum BodyState
    {
        Standing,
        Crouching,
    }

    [ExportGroup("Movement")]
    [ExportSubgroup("Speed")]
    [Export]
    public float WalkingSpeed = 2.5f;

    [Export]
    public float SprintingSpeed = 5.0f;

    [Export]
    public float CrouchingSpeed = 1.0f;

    [ExportSubgroup("Acceleration")]
    [Export]
    public float Acceleration = 10.0f;

    [Export]
    public float Deceleration = 10.0f;

    [Export]
    public float Gravity = -9.8f;

    [ExportSubgroup("Jumping")]
    [Export]
    public float JumpVelocity = 2.0f;

    [ExportGroup("Body")]
    [Export]
    public float BodyTransitionDuration = 0.2f;

    [Export]
    private float _standingCapsuleHeight = 1.8f;

    [Export]
    private float _crouchingCapsuleHeight = 0.6f;

    private MovementState _currentMovementState = MovementState.Standing;
    private BodyState _currentBodyState = BodyState.Standing;

    private CapsuleShape3D _collisionShape;
    private CapsuleMesh _mesh;
    private Node3D _head;
    private Tween _bodyTween;

    public override void _Ready()
    {
        _collisionShape = GetNode<CollisionShape3D>("CollisionShape3D").Shape as CapsuleShape3D;
        _mesh = GetNode<MeshInstance3D>("MeshInstance3D").Mesh as CapsuleMesh;
        _head = GetNode<Node3D>("Head");
    }

    public override void _PhysicsProcess(double delta)
    {
        switch (_currentBodyState)
        {
            case BodyState.Standing:
                if (CrouchKeyPressed())
                {
                    _currentBodyState = BodyState.Crouching;
                    CrouchComponents();
                }
                break;
            case BodyState.Crouching:
                if (!CrouchKeyPressed())
                {
                    _currentBodyState = BodyState.Standing;
                    StandComponents();
                }
                break;
        }

        switch (GetMovementState())
        {
            case MovementState.Standing:
                OnStandingState((float)delta);
                break;
            case MovementState.Walking:
                OnWalkingState((float)delta);
                break;
            case MovementState.Jumping:
                OnJumpingState((float)delta);
                break;
            case MovementState.Falling:
                OnFallingState((float)delta);
                break;
        }
    }

    public MovementState GetMovementState()
    {
        return _currentMovementState;
    }

    public BodyState GetBodyState()
    {
        return _currentBodyState;
    }

    #region Movement State Methods

    public void OnStandingState(float delta)
    {
        ApplyGravity(delta);
        Move(GetMovementSpeed(), delta);

        if (IsOnFloor() && MovementKeyPressed() && !Jumped())
        {
            _currentMovementState = MovementState.Walking;
            return;
        }

        if (!IsOnFloor())
        {
            _currentMovementState = MovementState.Falling;
            return;
        }

        if (Jumped())
        {
            _currentMovementState = MovementState.Jumping;
            return;
        }
    }

    public void OnWalkingState(float delta)
    {
        Move(GetMovementSpeed(), delta);
        ApplyGravity(delta);

        if (!MovementKeyPressed() && IsOnFloor() && !Jumped())
        {
            _currentMovementState = MovementState.Standing;
            return;
        }

        if (!IsOnFloor())
        {
            _currentMovementState = MovementState.Falling;
            return;
        }

        if (Jumped())
        {
            _currentMovementState = MovementState.Jumping;
            return;
        }
    }

    public void OnJumpingState(float delta)
    {
        Move(GetMovementSpeed(), delta);
        Velocity = new Vector3(Velocity.X, JumpVelocity, Velocity.Z); // Set the jump velocity
        _currentMovementState = MovementState.Falling;
    }

    public void OnFallingState(float delta)
    {
        ApplyGravity(delta);
        Move(GetMovementSpeed(), delta);

        if (IsOnFloor() && !MovementKeyPressed())
        {
            _currentMovementState = MovementState.Standing;
        }
        else if (IsOnFloor() && MovementKeyPressed())
        {
            _currentMovementState = MovementState.Walking;
        }
    }

    #endregion

    #region Input Methods

    public bool MovementKeyPressed()
    {
        if (
            Input.IsActionPressed("forward")
            || Input.IsActionPressed("backward")
            || Input.IsActionPressed("left")
            || Input.IsActionPressed("right")
        )
        {
            return true;
        }
        return false;
    }

    public float GetMovementSpeed()
    {
        switch (_currentBodyState)
        {
            case BodyState.Standing:
                return SprintKeyPressed() ? SprintingSpeed : WalkingSpeed;
            case BodyState.Crouching:
                return CrouchingSpeed;
            default:
                return WalkingSpeed;
        }
    }

    public bool SprintKeyPressed()
    {
        return Input.IsActionPressed("sprint");
    }

    public bool CrouchKeyPressed()
    {
        return Input.IsActionPressed("crouch");
    }

    public Vector2 MovementInput()
    {
        Vector2 input = new Vector2();

        if (Input.IsActionPressed("forward"))
        {
            input.Y -= 1;
        }
        if (Input.IsActionPressed("backward"))
        {
            input.Y += 1;
        }
        if (Input.IsActionPressed("left"))
        {
            input.X -= 1;
        }
        if (Input.IsActionPressed("right"))
        {
            input.X += 1;
        }

        return input.Normalized();
    }

    public bool Jumped()
    {
        return Input.IsActionJustPressed("jump");
    }

    #endregion

    #region Component Methods

    public void CrouchComponents()
    {
        TweenBodyComponents(_crouchingCapsuleHeight);
    }

    public void StandComponents()
    {
        TweenBodyComponents(_standingCapsuleHeight);
    }

    private void TweenBodyComponents(float targetCapsuleHeight)
    {
        _bodyTween?.Kill();
        _bodyTween = GetTree().CreateTween();
        _bodyTween.SetParallel();
        _bodyTween.TweenProperty(_head, "position", Vector3.Up * targetCapsuleHeight / 3, BodyTransitionDuration);
        _bodyTween.TweenProperty(_mesh, "height", targetCapsuleHeight, BodyTransitionDuration);
        _bodyTween.TweenProperty(_collisionShape, "height", targetCapsuleHeight, BodyTransitionDuration);
    }

    public void Move(float speed, float delta)
    {
        Vector3 movementDirection = Transform.Basis.Z * MovementInput().Y + Transform.Basis.X * MovementInput().X;
        movementDirection = movementDirection.Normalized();

        float currentAcceleration = MovementKeyPressed() ? Acceleration : Deceleration;

        Vector3 targetVelocity = movementDirection * speed;
        targetVelocity.Y = Velocity.Y;

        Vector3 simpleDeltaVector = targetVelocity - Velocity;
        Vector3 accelerationVector = simpleDeltaVector.Normalized() * currentAcceleration * delta;

        float wantedMagnitude = Mathf.Min(accelerationVector.Length(), simpleDeltaVector.Length());
        Vector3 finalAccelerationVector = accelerationVector.Normalized() * wantedMagnitude;

        Velocity += finalAccelerationVector;

        MoveAndSlide();
    }

    public void ApplyGravity(float delta)
    {
        // Velocity is a property (getter/setter), not a mutable field.
        // We copy it into a local Vector3 so we can safely change its Y value
        // and then assign the modified copy back to Velocity afterwards.
        Vector3 velocity = Velocity;

        if (!IsOnFloor())
        {
            velocity.Y += Gravity * delta;
        }
        else
        {
            velocity.Y += -0.2f;
        }

        Velocity = velocity;
    }

    #endregion
}
