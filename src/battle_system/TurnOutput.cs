using Godot;
using System.Collections.Generic;
using System.Linq;

// Entries in the same wave run at the same time; waves run in order.
public readonly record struct TurnEntry(IAbility Ability, Stack Target, int Wave = 0);

// What a stack does on its turn. Produced by player input or the AI, executed by Arena.
public class TurnOutput
{
    // Runs before everything else.
    public Vector2I? MoveTo;

    // Cast by the hero during this stack's turn, before the entries.
    public (SpellAbility Spell, Stack Target)? HeroSpell;

    public List<TurnEntry> Entries { get; } = new();

    public bool IsEmpty => MoveTo == null && HeroSpell == null && Entries.Count == 0;

    // The wave is how many earlier entries already target the same stack,
    // so abilities hitting one stack play one after another.
    public void Add(IAbility ability, Stack target)
    {
        Add(ability, target, Entries.Count(e => e.Target == target));
    }

    public void Add(IAbility ability, Stack target, int wave)
    {
        Entries.Add(new TurnEntry(ability, target, wave));
    }
}
