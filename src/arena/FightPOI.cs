using Godot;
using System;

public partial class FightPOI : PointOfInterest
{
	[Export] public EnemyStrategy ComputerStrategy { get; set; } = EnemyStrategy.Wild;
	[Export] public float detectionDistance;

	[Export] public Godot.Collections.Array<StackResource>? ComputerUnits;
	[Export] public Godot.Collections.Array<SpellAbility>? ComputerSpells;

	public override void _Process(double delta)
	{
		if (GlobalPosition.DistanceTo(_character!.GlobalPosition) <= detectionDistance && _character.poi != this && !accessed)
		{
			_character.ForceStop();
			Access();
		}
	}


	public override void Access()
	{
		base.Access();

		_character?.SetupEnemyArmy(ComputerUnits!, ComputerSpells!);
		_character?.SetupDifficulty(ComputerStrategy);
		_character?.inventoryPanel?.GetReady();

		_character!.poi = null;
		UnFocus();
	}
}
