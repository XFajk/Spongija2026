using Godot;
using System;
using System.Diagnostics;

public partial class AdventureCamera : PlayerCamera
{
	Rid map;
	[Export] public AdventureCharacter? Agent;

	[Export] public float Distance = 6.0f;
	[Export] public float Height = 3.0f;
	[Export] public float FollowHeight;
	[Export] public float MinHeight;
	[Export] public float MaxHeight;
	[Export] public float MouseSensitivity = 0.005f;
	[Export] public float MouseMovingSpeed = 0.5f;
	[Export] public float SmoothSpeed = 10.0f;
	[Export] Label? returnPanningButton;

	private float _yaw = 0.0f;
	private bool _rotating = false;
	private Vector2 _panningDirection;
	private bool _panning;
	private bool _panningChanged;
	Vector3 _rotatingCenter;
	RayCast3D? _groundRaycast;
	private bool _ignoringClick;
	
	[Export] Control? _cursorContainer;
	private Sprite2D? _spellCursor;

	[Export] Texture2D? cantGo;
	[Export] Texture2D? canGo;

	public override void _Ready()
	{
		base._Ready();

		Godot.Collections.Array<Node> group = GetTree().GetNodesInGroup("Terrain");
		Node terrain = group[0];
		terrain.Call("set_camera", this);

		map = GetWorld3D().NavigationMap;
		_rotatingCenter = Agent!.GlobalPosition;
		_groundRaycast = GetNode<RayCast3D>("/root/Map/SubViewport/RayCast3D");
		_groundRaycast.CollisionMask = (1u << 2) | (1u << 3);
		_groundRaycast.CollideWithAreas = true;
		_groundRaycast.CollideWithBodies = true;

		_spellCursor = new Sprite2D
		{
			Name = "SpellCursor",
			Visible = false,
		};
		_cursorContainer?.AddChild(_spellCursor);
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		Debug.Assert(_interactionRayCast != null);

		if (_ignoringClick && !Input.IsActionJustPressed("interact"))
			_ignoringClick = false;
		bool interactPressed = Input.IsActionJustPressed("interact") && !_ignoringClick;

		if (_interactionRayCast.IsColliding())
		{
			if (_interactionRayCast.GetCollider() is PointOfInterest p)
			{
				ShowCustomCursor(p.cursorIcon);
			}
			else if (_interactionRayCast.GetCollider() is Node obj)
			{
				if (obj.IsInGroup("Terrain"))
				{
					Vector3 hitPosition = _interactionRayCast.GetCollisionPoint();

					if (Agent!.canMove)
					{
						if (IsOnNavMesh(hitPosition))
						{
							if (hasPath(hitPosition))
							{
								if (interactPressed)
								{
									Agent!.SetTarget(hitPosition);
								}
								ShowCustomCursor(canGo);
							}
							else
							{
								ShowCustomCursor(cantGo);
							}
						}
						else
						{
							ShowCustomCursor(cantGo);
						}
					}
				}
				else
				{
					HideCustomCursor();
				}
			}
		}
		else
		{
			HideCustomCursor();
		}

		if (_panningChanged)
		{
			if (Input.IsActionJustPressed("jump"))
			{
				ResetPan();
			}

			returnPanningButton?.Show();
		}
		else
		{
			_rotatingCenter = Agent!.GlobalPosition;
			Height = Mathf.Lerp(
				Height,
				FollowHeight,
				(float)delta * SmoothSpeed
			);
			returnPanningButton?.Hide();
		}

		if (_spellCursor != null && _spellCursor.Visible)
			_spellCursor.GlobalPosition = _cursorContainer!.GetGlobalMousePosition();

		CameraFollow(delta);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_panningChanged)
		{
			_groundRaycast!.GlobalPosition = new Vector3(GlobalPosition.X, GlobalPosition.Y + 1000, GlobalPosition.Z);
			UpdateCenterHeight(delta);
		}
	}


	void UpdateCenterHeight(double delta)
	{
		_groundRaycast!.TargetPosition = new Vector3(0, -2000, 0);

		_groundRaycast.ForceRaycastUpdate();

		if (_groundRaycast.IsColliding())
		{
			Vector3 hit = _groundRaycast.GetCollisionPoint();

			_rotatingCenter.Y = (float)Mathf.Lerp(
				_rotatingCenter.Y,
				hit.Y,
				1.0 - Mathf.Exp(-10.0 * delta)
			);
		}
	}

	void CameraFollow(double delta)
	{
		Vector3 offset = new Vector3(
			Mathf.Sin(_yaw) * Distance,
			0,
			Mathf.Cos(_yaw) * Distance
		);

		Vector3 desiredPosition = (_rotatingCenter + Vector3.Up * Height) + offset;

		float t = 1.0f - Mathf.Exp(-SmoothSpeed * (float)delta);

		GlobalPosition = GlobalPosition.Lerp(desiredPosition, t);

		//
		Vector3 direction = _rotatingCenter - GlobalPosition;

		if (direction.LengthSquared() < 0.0001f)
			return;

		Basis targetBasis = Basis.LookingAt(direction.Normalized(), Vector3.Up);

		Quaternion targetRotation = targetBasis.GetRotationQuaternion();

		Quaternion currentRotation = GlobalBasis.GetRotationQuaternion();

		Quaternion smoothRotation = currentRotation.Slerp(
			targetRotation,
			(float)delta * SmoothSpeed * 5
		);

		GlobalBasis = new Basis(smoothRotation);
		//LookAt(_rotatingCenter, Vector3.Up);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton rightButton &&
			rightButton.ButtonIndex == MouseButton.Right)
		{
			_rotating = rightButton.Pressed && Agent!.canMove;
		}

		if (@event is InputEventMouseMotion mouseMotionRight && _rotating)
		{
			_yaw -= mouseMotionRight.Relative.X * MouseSensitivity;
		}

		if (@event is InputEventMouseButton middleButton &&
			middleButton.ButtonIndex == MouseButton.Middle)
		{
			_panning = middleButton.Pressed && Agent!.canMove;
		}

		if (@event is InputEventMouseMotion motion && _panning)
		{
			_panningChanged = true;
			Vector3 forward = GlobalTransform.Basis.Z;
			forward.Y = 0;
			forward = forward.Normalized();

			Vector3 right = GlobalTransform.Basis.X;
			right.Y = 0;
			right = right.Normalized();

			_rotatingCenter += right * -motion.Relative.X * MouseSensitivity * MouseMovingSpeed;
			_rotatingCenter += forward * -motion.Relative.Y * MouseSensitivity * MouseMovingSpeed;

			Position += right * -motion.Relative.X * MouseSensitivity * MouseMovingSpeed;
			Position += forward * -motion.Relative.Y * MouseSensitivity * MouseMovingSpeed;
		}

		if (_panningChanged)
		{
			if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
			{
				if (mouseEvent.ButtonIndex == MouseButton.WheelDown)
				{
					Height += 1f;
				}
				else if (mouseEvent.ButtonIndex == MouseButton.WheelUp)
				{
					Height -= 1f;
				}

				Height = Mathf.Clamp(Height, MinHeight, MaxHeight);
			}
		}
	}

	public bool IsOnNavMesh(Vector3 pos)
	{
		Vector3 closest =
			NavigationServer3D.MapGetClosestPoint(map, pos);

		return closest.DistanceTo(pos) <= 0.5f;
		//return closest.X == pos.X && closest.Z == pos.Z;
	}

	public bool hasPath(Vector3 pos)
	{
		Vector3[] path = NavigationServer3D.MapGetPath(
			map,
			Agent!.GlobalPosition,
			pos,
			true
		);

		Vector3 endPoint = path[path.Length - 1];
		return endPoint.DistanceTo(pos) <= 0.5f;
	}

	public async void ResetPan()
	{
		_panning = false;

		Tween tween = CreateTween();
		tween.TweenMethod(
		Callable.From<Vector3>(v => _rotatingCenter = v),
		_rotatingCenter,
		Agent!.GlobalPosition,
		0.2f
	);

		await ToSignal(tween, Tween.SignalName.Finished);

		_panningChanged = false;
	}

	public void IgnoreAdventureCurrentClick()
	{
		_ignoringClick = true;
	}

	public void ShowCustomCursor(Texture2D? icon)
	{
		if (_spellCursor == null)
			return;
		if (icon == null)
		{
			HideCustomCursor();
			return;
		}
		if (_spellCursor.Texture != icon)
		{
			_spellCursor.Texture = icon;
			Vector2 size = icon.GetSize();
			_spellCursor.Scale = Vector2.One * 32 / Mathf.Max(1f, Mathf.Max(size.X, size.Y));
		}
		_spellCursor.GlobalPosition = _cursorContainer!.GetGlobalMousePosition();
		_spellCursor.Visible = true;
		UpdateMouseMode(false);
	}

	public void HideCustomCursor()
	{
		if (_spellCursor == null || !_spellCursor.Visible)
			return;
		_spellCursor.Visible = false;
		UpdateMouseMode(true);
	}

	private void UpdateMouseMode(bool _visible)
	{
		Input.MouseMode = _visible ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Hidden;
	}
}
