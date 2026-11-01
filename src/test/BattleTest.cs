using Godot;
using Godot.Collections;
using System;

// Test entry point: run scenes/battle_test.tscn (F6) to jump straight into a battle with these armies.
public partial class BattleTest : Node
{
    [Export]
    public Array<SpellAbility> HeroSpells { get; set; } = [];

    // Equipped by the player's hero, in order
    [Export]
    public Array<Artefact> HeroArtefacts { get; set; } = [];

    [Export]
    public Array<StackResource> PlayerUnits { get; set; } = [];

    [Export]
    public Array<StackResource> ComputerUnits { get; set; } = [];

    [Export]
    public EnemyStrategy ComputerStrategy { get; set; } = EnemyStrategy.Wild;


    public override void _Ready()
    {

        var hero = new Hero { Strength = 2, Defense = 2, Knowledge = 5 };
        // Before the mana, which is capped by the artefacts' max mana
        hero.Artefacts.AddRange(HeroArtefacts);
        foreach (SpellAbility spell in HeroSpells)
            hero.LearnSpell(spell);
        hero.Mana = 30;
        var playerArmy = new Army { Hero = hero };
        foreach (var stack in PlayerUnits)
        {
            if (stack.Unit != null)
            {
                playerArmy.AddStack(new Stack(stack.Unit, stack.Quantity));
            }
        }

        var computerArmy = new Army { Strategy = ComputerStrategy };
        foreach (var stack in ComputerUnits)
        {
            if (stack.Unit != null)
            {
                computerArmy.AddStack(new Stack(stack.Unit, stack.Quantity));
            }
        }

        _ = SceneTransitions.EnterArena(GetTree(), playerArmy, computerArmy);
    }
}
