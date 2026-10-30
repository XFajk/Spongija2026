using Godot;
using System;
using Godot.Collections;

public partial class BattleStarter : Node
{
	public EnemyStrategy ComputerStrategy;
	Array<SpellAbility>? HeroSpells;
	Array<StackResource>? PlayerUnits;
	Array<StackResource>? ComputerUnits;
	Array<SpellAbility>? ComputerHeroSpells;

	public void EnterBattle(SpellAbility[] spells, StackResource[] units)
	{
		GetUnits(units);
		GetSpells(spells);

		var playerArmy = new Army { Hero = new Hero { Strength = 2, Defense = 2, Knowledge = 5, Mana = 30} };
		playerArmy.Hero.Spells.AddRange(HeroSpells!);
		foreach (var stack in PlayerUnits!)
		{
			if (stack.Unit != null)
			{
				playerArmy.AddStack(new Stack(stack.Unit, stack.Quantity));
			}
		}

		var computerArmy = new Army { Strategy = ComputerStrategy };
		if (ComputerHeroSpells?.Count > 0)
		{
			computerArmy.Hero = new Hero { Strength = 2, Defense = 2, Knowledge = 5, Mana = 30 };
			computerArmy.Hero.Spells.AddRange(ComputerHeroSpells);
		}
		foreach (var stack in ComputerUnits!)
		{
			if (stack.Unit != null)
			{
				computerArmy.AddStack(new Stack(stack.Unit, stack.Quantity));
			}
		}

		SceneTransitions.EnterArena(GetTree(), playerArmy, computerArmy);
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

	void GetSpells(SpellAbility[] spells)
	{
		HeroSpells = new Array<SpellAbility>();
		for (int i = 0; i < spells.Length; i++)
		{
			HeroSpells.Add(spells[i]);
		}
	}

	public void SetupEnemyArmy(Godot.Collections.Array<StackResource> units, Godot.Collections.Array<SpellAbility> spells)
	{
		ComputerUnits = units;
		ComputerHeroSpells = spells;
	}

	public void SetupDifficulty(EnemyStrategy difficulty)
	{
		ComputerStrategy = difficulty;
	}
}
