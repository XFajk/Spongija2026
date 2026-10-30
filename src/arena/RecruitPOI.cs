using Godot;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

public partial class RecruitPOI : PointOfInterest
{
	[Export] StackResource[] Units = [];

	[Export] Unit? pilgrimUnit;
	[Export] Unit? warriorUnit;
	[Export] public float metersForPilgrim;
	[Export] public float metersForWarrior;

	public bool looted;

	public float maxDistance;
	public float currentMaxDistance;
	float distance;
	float bonusDistance;
	bool firstTimeLooted;

	[Export]
	public PackedScene? FloatingTextScene { get; set; }

	[Export]
	public AdventureCamera? Camera { get; set; }

	[Export]
	public Control? Container { get; set; }

	public override void _Process(double delta)
	{
		distance = GetDistance();

		if (firstTimeLooted)
		{
			if (distance > currentMaxDistance)
			{
				currentMaxDistance = distance;
			}

			if (currentMaxDistance > maxDistance)
			{
				if (looted)
				{
					looted = false;
				}

				bonusDistance = currentMaxDistance - maxDistance;
			}
			else
			{
				bonusDistance = 0;
			}
		}
		else
		{
			bonusDistance = 15;
		}
	}

	float GetDistance()
	{
		Rid map = Camera!.Agent!.GetNode<NavigationAgent3D>("NavigationAgent3D").GetNavigationMap();

		Vector3[] path = NavigationServer3D.MapGetPath(
			map,
			Camera.Agent.GlobalPosition,
			GlobalPosition,
			true
		);

		float distance = 0;

		for (int i = 0; i < path.Length - 1; i++)
		{
			distance += path[i].DistanceTo(path[i + 1]);
		}

		return distance;
	}


	public override async void Access()
	{
		if (!looted)
		{
			if (!firstTimeLooted)
			{
				firstTimeLooted = true;
			}

			base.Access();

			_character!.canMove = false;

			GetUnits();

			if (Units.Length != 0)
			{
				await ShowFloatingText(GlobalPosition);

				_character?.GetNode<Inventory>("Inventory").AddUnits(Units!);
			}

			maxDistance = currentMaxDistance;
			looted = true;
		}
		
		_character!.poi = null;
		UnFocus();
	}

	public async Task ShowFloatingText(Vector3 worldPosition)
	{
		for (int i = 0; i < Units?.Length; i++)
		{
			var popup = FloatingTextScene?.Instantiate<FloatingText>();

			Container?.AddChild(popup);

			// Prevod 3D pozície na 2D pozíciu obrazovky
			Vector2 screenPosition = Camera!.UnprojectPosition(worldPosition);

			popup!.Text = "+ " + Units[i]?.Quantity.ToString() + " " + Units[i]?.Unit?.Name;
			popup!.Position = screenPosition;

			if (i == Units?.Length - 1)
			{
				await ToSignal(
					GetTree().CreateTimer(0.7f),
					SceneTreeTimer.SignalName.Timeout
				);
				_character!.canMove = true;
				await ToSignal(
					GetTree().CreateTimer(0.3f),
					SceneTreeTimer.SignalName.Timeout
				);
			}
			else
			{
				await ToSignal(
					GetTree().CreateTimer(0.5f),
					SceneTreeTimer.SignalName.Timeout
				);
			}
		}
	}

	void GetUnits()
	{
		if (bonusDistance >= metersForWarrior)
		{
			StackResource _pilgrimUnit = new StackResource();
			_pilgrimUnit.Unit = pilgrimUnit;
			_pilgrimUnit.Quantity = (uint)MathF.Floor(bonusDistance / metersForPilgrim);

			StackResource _warriorUnit = new StackResource();
			_warriorUnit.Unit = warriorUnit;
			_warriorUnit.Quantity = (uint)MathF.Floor(bonusDistance / metersForWarrior);

			Units = [_pilgrimUnit, _warriorUnit];
		}
		else if (bonusDistance >= metersForPilgrim)
		{
			StackResource _pilgrimUnit = new StackResource();
			_pilgrimUnit.Unit = pilgrimUnit;
			_pilgrimUnit.Quantity = (uint)MathF.Floor(bonusDistance / metersForPilgrim);

			Units = [_pilgrimUnit];
		}
		else
		{
			Units = [];
		}
	}
}
