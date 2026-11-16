using Godot;
using System;
using System.Threading;
using System.Threading.Tasks;

public partial class FightPOI : PointOfInterest
{
	[Export] public bool AlwaysRecruit;
	[Export] public bool CanRecruit;
	[Export] public bool CanTrigger;
	[Export] public EnemyStrategy ComputerStrategy { get; set; } = EnemyStrategy.Wild;
	bool outside = false;

	[Export] public Godot.Collections.Array<StackResource>? ComputerUnits;

	[Export]
	public PackedScene? FloatingTextScene { get; set; }

	[Export]
	public AdventureCamera? Camera { get; set; }

	[Export]
	public Control? Container { get; set; }
	Area3D? trigger;

	public override void _Ready()
	{
		base._Ready();

		if (CanTrigger)
		{
			 trigger = GetNode<Area3D>("Trigger");
			_ = InitializeTrigger();
		}
	}

	async Task InitializeTrigger()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

		outside = true;
		foreach (Node3D body in trigger!.GetOverlappingBodies())
		{
			if (body is AdventureCharacter)
			{
				outside = false;
				break;
			}
		}

		trigger.BodyEntered += Trigger;
		trigger.BodyExited += Exit;
	}

	public override void Access()
	{
		if (base._destroyed)
			return;

		base.Access();

		float enemyStrenght = GetEnemyDamage(ComputerUnits);
		float playerStrenght = GetPlayerDamage();
		float enemyHealth = GetEnemyHealth(ComputerUnits);
		float playerHealth = GetPlayerHealth();

		float winningChance = (playerHealth - enemyStrenght) + (playerStrenght - enemyHealth);
		Random random = new Random();
		int _random = random.Next(2);

		EndFightScreen.TriggeringFightPOI = GetPath();  // Store the path of this FightPOI for the end fight screen
		_character?.SetupEnemyArmy(ComputerUnits!);
		_character?.SetupDifficulty(ComputerStrategy);

		if (AlwaysRecruit)
		{
			StackResource[] _units = new StackResource[ComputerUnits!.Count];
			for (int i = 0; i < ComputerUnits!.Count; i++)
			{
				_units[i] = ComputerUnits![i];
			}
			
			_character!.inventoryPanel!.ShowUnitsText(_units);
			_character!.inventory?.AddUnits(_units);
			_character!.inventory?.LearnSpellsOf(_units);
			_Destroy();
		}
		else
		{
			_character?.inventoryPanel?.GetReady(winningChance >= 600 && _random == 0 && CanRecruit, this);
		}

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

	public void _Destroy()
	{
		base.Destroy();
	}

	void Trigger(Node3D _body)
	{
		if (base._destroyed)
			return;

		GD.Print(_body.Name);

		if (_body is AdventureCharacter && _character!.poi != this && !accessed && outside && !AlwaysRecruit)
		{
			_character!.ForceStop();
			Access();
			outside = false;
		}
	}

	void Exit(Node3D _body)
	{
		if (base._destroyed)
			return;

		if (_body is AdventureCharacter)
		{
			outside = true;
		}
	}
}
