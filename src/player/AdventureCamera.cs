using Godot;
using System;
using System.Diagnostics;

public partial class AdventureCamera : PlayerCamera
{
	Rid map;
	[Export] public AdventureCharacter? agent;

	[Export] public float Distance = 6.0f;
	[Export] public float Height = 3.0f;
	[Export] public float MouseSensitivity = 0.005f;
	[Export] public float SmoothSpeed = 10.0f;

	private float _yaw = 0.0f;
	private bool _rotating = false;
	private Vector2 _panningDirection;
	private bool _panning;
	private bool _panningChanged;
	Vector3 rotatingCenter;
	RayCast3D? _groundRaycast;

	public override void _Ready()
	{
		base._Ready();

		map = GetWorld3D().NavigationMap;
		rotatingCenter = agent!.GlobalPosition;
		_groundRaycast = GetNode<RayCast3D>("/root/Map/SubViewport/RayCast3D");
		_groundRaycast.CollisionMask = (1u << 2) | (1u << 3);
		_groundRaycast.CollideWithAreas = true;
		_groundRaycast.CollideWithBodies = true;
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		Debug.Assert(_interactionRayCast != null);
		if (_interactionRayCast.IsColliding())
		{
			if (_interactionRayCast.GetCollider() is CollisionObject3D obj)
			{
				if ((obj.CollisionLayer & (1u << 2)) != 0)
				{
					Vector3 hitPosition = _interactionRayCast.GetCollisionPoint();

					/*Vector3 navPoint =
					NavigationServer3D.MapGetClosestPoint(map, hitPosition);*/

					if (agent!.canMove)
					{
						if (IsOnNavMesh(hitPosition))
						{
							if (hasPath(hitPosition))
							{
								if (Input.IsActionJustPressed("interact"))
								{
									agent.SetTarget(hitPosition);
								}
							}
						}
					}
				}
			}
		}

		if (_panningChanged)
		{
			if (Input.IsActionJustPressed("jump"))
			{
				ResetPan();
			}
		}
		else
		{
			rotatingCenter = agent!.GlobalPosition;
		}

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

			rotatingCenter.Y = (float)Mathf.Lerp(
				rotatingCenter.Y,
				hit.Y,
				1.0 - Mathf.Exp(-10.0 * delta)
			);
		}
	}

	void CameraFollow(double delta)
	{
		if (!_panning)
		{
			Vector3 offset = new Vector3(
				Mathf.Sin(_yaw) * Distance,
				0,
				Mathf.Cos(_yaw) * Distance
			);

			Vector3 desiredPosition = (rotatingCenter + Vector3.Up * Height) + offset;

			float t = 1.0f - Mathf.Exp(-SmoothSpeed * (float)delta);

			GlobalPosition = GlobalPosition.Lerp(desiredPosition, t);

			LookAt(rotatingCenter, Vector3.Up);
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton rightButton &&
			rightButton.ButtonIndex == MouseButton.Right)
		{
			_rotating = rightButton.Pressed;
		}

		if (@event is InputEventMouseMotion mouseMotionRight && _rotating)
		{
			_yaw -= mouseMotionRight.Relative.X * MouseSensitivity;
		}

		if (@event is InputEventMouseButton middleButton &&
			middleButton.ButtonIndex == MouseButton.Middle)
		{
			_panning = middleButton.Pressed;
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

			rotatingCenter += right * -motion.Relative.X * MouseSensitivity * 5;
			rotatingCenter += forward * -motion.Relative.Y * MouseSensitivity * 5;

			Position += right * -motion.Relative.X * MouseSensitivity * 5;
			Position += forward * -motion.Relative.Y * MouseSensitivity * 5;
		}
	}

	public bool IsOnNavMesh(Vector3 pos)
	{
		Vector3 closest =
			NavigationServer3D.MapGetClosestPoint(map, pos);

		//return closest.DistanceTo(pos) <= 0.5f;
		return closest.X == pos.X && closest.Z == pos.Z;
	}

	public bool hasPath(Vector3 pos)
	{
		Vector3[] path = NavigationServer3D.MapGetPath(
			map,
			agent!.GlobalPosition,
			pos,
			true
		);

		Vector3 endPoint = path[path.Length - 1];
		return endPoint.DistanceTo(pos) <= 1f;
	}

	public async void ResetPan()
	{
		_panning = false;

		Tween tween = CreateTween();
		tween.TweenMethod(
		Callable.From<Vector3>(v => rotatingCenter = v),
		rotatingCenter,
		agent!.GlobalPosition,
		0.2f
	);

		await ToSignal(tween, Tween.SignalName.Finished);

		_panningChanged = false;
	}
}
