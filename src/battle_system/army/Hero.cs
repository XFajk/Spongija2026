using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class Hero
{
    private uint _mana;
    public uint Mana
    {
        get { return _mana; }
        set { _mana = Math.Clamp(value, 0, MaxMana); }
    }

    private uint _strength;
    public uint Strength
    {
        get { return _strength; }
        set { _strength = Math.Clamp(value, 0, 10); }
    }
    private uint _knowledge;
    public uint Knowledge
    {
        get { return _knowledge; }
        set { _knowledge = Math.Clamp(value, 0, 20); }
    }
    private uint _defense;
    public uint Defense
    {
        get { return _defense; }
        set { _defense = Math.Clamp(value, 0, 10); }
    }

    public uint MaxMana => Knowledge * 10;

    public List<SpellAbility> Spells = [];

    // The hero can cast once per round. Reset by Battle.StartNewRound.
    public bool HasCastThisRound;

    // Bonus applied to a stack stat by a hero stat. TBD: +10% per point for now.
    public static int ApplyBonus(int stat, uint heroStat)
    {
        return Mathf.RoundToInt(stat * (1 + heroStat * 0.1f));
    }

    public void LearnSpell(SpellAbility spell)
    {
        if (!Spells.Contains(spell))
            Spells.Add(spell);
    }

    // Spells unlocked by the current Knowledge. Computed on every call, so artifact changes apply right away.
    public List<SpellAbility> GetKnownSpells() => Spells.Where(s => Knowledge >= s.MinKnowledge).ToList();

    // Unlocked spells the hero has enough mana for, ignoring the once-per-round limit.
    public List<SpellAbility> GetUsableSpells() => GetKnownSpells().Where(s => Mana >= s.ManaCost).ToList();

    public bool CanCast(SpellAbility spell)
    {
        return !HasCastThisRound && Spells.Contains(spell) && Knowledge >= spell.MinKnowledge && Mana >= spell.ManaCost;
    }
}
