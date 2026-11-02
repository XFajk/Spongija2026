using Godot;
using System;
using Godot.Collections;

public partial class BattleStarter : Node
{
	public EnemyStrategy ComputerStrategy;
	Array<StackResource>? PlayerUnits;
	public Array<StackResource>? ComputerUnits;

	// hero: the player's hero, from Inventory.CreateHero (its stats, artefacts, spells and mana)
	public void EnterBattle(Hero hero, StackResource[] units)
	{
		GetUnits(units);

		var playerArmy = new Army { Hero = hero };
		foreach (var stack in PlayerUnits!)
		{
			if (stack.Unit != null)
			{
				playerArmy.AddStack(new Stack(stack.Unit, stack.Quantity));
			}
		}

		// No hero: the computer's armies fight without one
		var computerArmy = new Army { Strategy = ComputerStrategy };
		foreach (var stack in ComputerUnits!)
		{
			if (stack.Unit != null)
			{
				computerArmy.AddStack(new Stack(stack.Unit, stack.Quantity));
			}
		}

		GetNode<SaveManager>("/root/Map/SaveSystem").SaveGame(CurrentSave.CurrentSaveIndex);

		_ = SceneTransitions.EnterArena(GetTree(), playerArmy, computerArmy);
	}

	void GetUnits(StackResource[] units)
	{
		PlayerUnits = new Array<StackResource>();
		for (int i = 0; i < units.Length; i++)
		{
			if (units[i] != null)
			{
				PlayerUnits.Add(units[i]);
			}
		}
	}

	public void SetupEnemyArmy(Godot.Collections.Array<StackResource> units)
	{
		ComputerUnits = units;
	}

	public void SetupDifficulty(EnemyStrategy difficulty)
	{
		ComputerStrategy = difficulty;
	}
}
