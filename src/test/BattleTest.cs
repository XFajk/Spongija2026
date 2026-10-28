using Godot;
using Godot.Collections;
using System;

// Test entry point: run scenes/battle_test.tscn (F6) to jump straight into a battle with these armies.
public partial class BattleTest : Node
{
    [Export]
    public Array<SpellAbility> HeroSpells { get; set; } = [];

    [Export]
    public Array<StackResource> PlayerUnits { get; set; } = [];

    [Export]
    public Array<StackResource> ComputerUnits { get; set; } = [];

    public override void _Ready()
    {

        var playerArmy = new Army { Hero = new Hero { Strength = 2, Defense = 2, Knowledge = 5, Mana = 30} };
        playerArmy.Hero.Spells.AddRange(HeroSpells);
        foreach (var stack in PlayerUnits)
        {
            if (stack.Unit != null)
            {
                playerArmy.AddStack(new Stack(stack.Unit, stack.Quantity));
            }
        }

        var computerArmy = new Army();
        foreach (var stack in ComputerUnits)
        {
            if (stack.Unit != null)
            {
                computerArmy.AddStack(new Stack(stack.Unit, stack.Quantity));
            }
        }

        SceneTransitions.EnterArena(GetTree(), playerArmy, computerArmy);
    }
}
