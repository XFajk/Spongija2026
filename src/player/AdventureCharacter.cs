using Godot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

public partial class AdventureCharacter : CharacterBody3D
{
	[Export] public float Speed = 5.0f;
	[Export] public float RotationSpeed = 5.0f;
	public Vector3 TargetPosition;
	private NavigationAgent3D agent;
	[Export] PackedScene point;
	[Export] Node3D container;
	List<Node3D> points = new List<Node3D>();
	Vector3 _nextPoint;

	public override void _Ready()
	{
		agent = GetNode<NavigationAgent3D>("NavigationAgent3D");

		agent.PathDesiredDistance = 0.5f;
		agent.TargetDesiredDistance = 0.5f;

		CallDeferred(nameof(SetupNavigation));
	}

	private async void SetupNavigation()
	{
		await ToSignal(
			GetTree(),
			SceneTree.SignalName.PhysicsFrame
		);
	}

	public void SetTarget(Vector3 pos)
	{
		TargetPosition = pos;
		agent.TargetPosition = TargetPosition;
		ResetPoints();
		GD.Print(pos);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (agent.IsNavigationFinished())
		{
			Velocity = Vector3.Zero;
			MoveAndSlide();

			if (points.Count > 0)
			{
				ResetPoints();
			}
			return;
		}

		Vector3 nextPosition = agent.GetNextPathPosition();
		Vector3 direction = GlobalPosition.DirectionTo(nextPosition);

		if (direction.LengthSquared() > 0.001f)
		{
			float targetAngle = Mathf.Atan2(
				direction.X,
				direction.Z
			);

			Rotation = new Vector3(
				Rotation.X,
				Mathf.LerpAngle(
					Rotation.Y,
					targetAngle,
					RotationSpeed * (float)delta
				),
				Rotation.Z
			);
		}

		Velocity = direction * Speed;

		MoveAndSlide();

		if (_nextPoint != nextPosition)
		{
			DisplayPathPoints();
			_nextPoint = nextPosition;
		}

		for (int i = 0; i < points.Count; i++)
		{
			if (GlobalPosition.DistanceTo(points[i].GlobalPosition) <= 0.5f)
			{
				points[i].QueueFree();
				points.RemoveAt(i);
			}
		}
	}

	void DisplayPathPoints()
	{
		Rid map = GetWorld3D().NavigationMap;

		Vector3[] path = NavigationServer3D.MapGetPath(
			map,
			GlobalPosition,
			TargetPosition,
			true
		);

		if (points.Count < path.Length)
			{
				for (int i = points.Count; i < path.Length; i++)
				{
					var p = point.Instantiate<Node3D>();
					container.AddChild(p);
					Vector3 pointsPos = path[i];
					pointsPos.Y -= 0.3f;
					p.GlobalPosition = pointsPos;
					points.Add(p);
				}
			}
	}

	void ResetPoints()
	{
		for (int i = 0; i < points.Count; i++)
		{
			points[i].QueueFree();
		}

		points = new List<Node3D>();
	}
}
