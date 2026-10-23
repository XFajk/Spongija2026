using Godot;
using System;

public partial class AdventureCamera : PlayerCamera
{
	Rid map;
	[Export] public AdventureCharacter agent; 

	[Export] public float Distance = 6.0f;
	[Export] public float Height = 3.0f;
	[Export] public float MouseSensitivity = 0.005f;
	[Export] public float SmoothSpeed = 10.0f;

	private float _yaw = 0.0f;
	private bool _rotating = false;

	public override void _Ready()
	{
		base._Ready();

		map = GetWorld3D().NavigationMap;
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		if (_interactionRayCast.IsColliding())
		{
			if (Input.IsActionJustPressed("interact"))
			{
				if (_interactionRayCast.GetCollider() is CollisionObject3D obj)
				{
					if ((obj.CollisionLayer & (1u << 2)) != 0)
					{
						Vector3 hitPosition = _interactionRayCast.GetCollisionPoint();
				
						Vector3 navPoint =
						NavigationServer3D.MapGetClosestPoint(map, hitPosition);

						if (IsOnNavMesh(hitPosition))
						{
							if (hasPath(hitPosition))
							{
								agent.SetTarget(hitPosition);
							}
						}
					}
				}
			}
		}

		Vector3 center = agent.GlobalPosition + Vector3.Up * Height;

		Vector3 offset = new Vector3(
			Mathf.Sin(_yaw) * Distance,
			0,
			Mathf.Cos(_yaw) * Distance
		);

		Vector3 desiredPosition = center + offset;

		float t = 1.0f - Mathf.Exp(-SmoothSpeed * (float)delta);

		GlobalPosition = GlobalPosition.Lerp(desiredPosition, t);

		LookAt(agent.GlobalPosition, Vector3.Up);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButton &&
			mouseButton.ButtonIndex == MouseButton.Left)
		{
			_rotating = mouseButton.Pressed;
		}

		if (@event is InputEventMouseMotion mouseMotion && _rotating)
		{
			_yaw -= mouseMotion.Relative.X * MouseSensitivity;
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
			agent.GlobalPosition,
			pos,
			true
		);

		Vector3 endPoint = path[path.Length - 1];
		return endPoint.DistanceTo(pos) <= 1f;
	}
}
