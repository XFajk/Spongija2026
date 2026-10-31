using Godot;
using System;
using System.Threading.Tasks;

public partial class FightPOI : PointOfInterest
{
	[Export] public EnemyStrategy ComputerStrategy { get; set; } = EnemyStrategy.Wild;
	[Export] public float detectionDistance;

	[Export] public Godot.Collections.Array<StackResource>? ComputerUnits;
	[Export] public Godot.Collections.Array<SpellAbility>? ComputerSpells;

	[Export]
	public PackedScene? FloatingTextScene { get; set; }

	[Export]
	public AdventureCamera? Camera { get; set; }

	[Export]
	public Control? Container { get; set; }

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

		float enemyStrenght = GetEnemyDamage(ComputerUnits);
		float playerStrenght = GetPlayerDamage();
		float enemyHealth = GetEnemyHealth(ComputerUnits);
		float playerHealth = GetPlayerHealth();

		float winningChance = (playerHealth - enemyStrenght) + (playerStrenght - enemyHealth);
		Random random = new Random();
		int _random = random.Next(2);

		GD.Print(winningChance);

		_character?.SetupEnemyArmy(ComputerUnits!, ComputerSpells!);
		_character?.SetupDifficulty(ComputerStrategy);
		_character?.inventoryPanel?.GetReady(winningChance >= 600 && _random == 0, this);

		_character!.poi = null;
		UnFocus();
	}

	float GetEnemyDamage(Godot.Collections.Array<StackResource>? units)
	{
		float totalDamage = 0;

		for (int i = 0; i < units!.Count; i++)
		{
			totalDamage += units[i].Unit!.Strength * units[i].Quantity;
		}

		return totalDamage;
	}

	float GetEnemyHealth(Godot.Collections.Array<StackResource>? units)
	{
		float totalHealth = 0;

		for (int i = 0; i < units!.Count; i++)
		{
			totalHealth += units[i].Unit!.MaxHealth * units[i].Quantity;
		}

		return totalHealth;
	}

	float GetPlayerDamage()
	{
		float totalDamage = 0;
		Inventory _inventory = _character!.GetNode<Inventory>("Inventory");

		for (int i = 0; i < _inventory.Units!.Length; i++)
		{
			if ( _inventory.Units[i] != null)
			{
				totalDamage += _inventory.Units[i].Unit!.Strength * _inventory.Units[i].Quantity;
			}
		}

		return totalDamage;
	}

	float GetPlayerHealth()
	{
		float totalHealth = 0;
		Inventory _inventory = _character!.GetNode<Inventory>("Inventory");

		for (int i = 0; i < _inventory.Units!.Length; i++)
		{
			if ( _inventory.Units[i] != null)
			{
				totalHealth += _inventory.Units[i].Unit!.MaxHealth * _inventory.Units[i].Quantity;
			}
		}

		return totalHealth;
	}
}
