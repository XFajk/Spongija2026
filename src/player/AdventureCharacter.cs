using Godot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

public partial class AdventureCharacter : CharacterBody3D
{
	[Export] public float stopDistance;
	[Export] public float Speed = 5.0f;
	[Export] public float RotationSpeed = 5.0f;
	public Vector3 TargetPosition;
	private NavigationAgent3D agent;
	[Export] PackedScene point;
	[Export] Node3D container;
	[Export] float pointsMinDistance;
	List<Node3D> points = new List<Node3D>();
	PointOfInterest poi;

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
		poi = null;
		agent.TargetDesiredDistance = stopDistance;
		TargetPosition = pos;
		agent.TargetPosition = TargetPosition;
		ResetPoints();
		GD.Print(pos);
		DisplayPathPoints();
	}

	public void SetPOITarget(Vector3 pos, float distance, PointOfInterest _point)
	{
		poi = _point;
		agent.TargetDesiredDistance = distance;
		TargetPosition = pos;
		agent.TargetPosition = TargetPosition;
		ResetPoints();
		GD.Print(pos);
		DisplayPathPoints();
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

			if (poi != null)
			{
				poi.Access();
				poi = null;
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

		float distanceToNext = 0f;

		for (int i = 0; i < path.Length - 1; i++)
		{
			Vector3 start = path[i];
			Vector3 end = path[i + 1];

			float length = start.DistanceTo(end);

			if (length < 0.001f)
				continue;

			while (distanceToNext <= length)
			{
				float t = distanceToNext / length;
				Vector3 position = start.Lerp(end, t);

				Node3D _point = point.Instantiate<Node3D>();
				_point.Hide();
				container.AddChild(_point);
				_point.GlobalPosition = position;

				if (_point.GlobalPosition.DistanceTo(TargetPosition) < agent.TargetDesiredDistance && agent.TargetDesiredDistance != stopDistance)
				{
					_point.QueueFree();
				}
				else
				{
					_point.Show();
					points.Add(_point);
				}

				distanceToNext += pointsMinDistance;
			}

			distanceToNext -= length;
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

	void ReduceClosePoints()
	{
		for (int i = 1; i < points.Count - 1; i++)
		{
			if (points[i].GlobalPosition.DistanceTo(points[i - 1].GlobalPosition) < pointsMinDistance || points[i].GlobalPosition.DistanceTo(points[i + 1].GlobalPosition) < pointsMinDistance)
			{
				points[i].QueueFree();
				points.RemoveAt(i);
				i--;
			}
		}
	}
}
