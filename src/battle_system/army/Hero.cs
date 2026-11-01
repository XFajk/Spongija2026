using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class Hero
{
    // Equipped artefacts. The stat and Spells getters return the values with every artefact applied (Modified);
    // the setters set the hero's own base values.
    public List<Artefact> Artefacts { get; } = [];

    private uint _mana;
    // Not changed by artefacts, only capped by MaxMana, so add artefacts before setting it
    public uint Mana
    {
        get { return _mana; }
        set { _mana = Math.Clamp(value, 0, MaxMana); }
    }

    private uint _strength;
    public uint Strength
    {
        get { return Modified._strength; }
        set { _strength = Math.Clamp(value, 0, 10); }
    }
    private uint _knowledge;
    public uint Knowledge
    {
        get { return Modified._knowledge; }
        set { _knowledge = Math.Clamp(value, 0, 20); }
    }
    private uint _defense;
    public uint Defense
    {
        get { return Modified._defense; }
        set { _defense = Math.Clamp(value, 0, 10); }
    }

    private uint _maxManaBonus;
    // Max mana on top of what Knowledge gives (the mana pouch)
    public uint MaxManaBonus
    {
        get { return Modified._maxManaBonus; }
        set { _maxManaBonus = value; }
    }

    public uint MaxMana
    {
        get
        {
            Hero modified = Modified;
            return modified._knowledge * 10 + modified._maxManaBonus;
        }
    }

    private List<SpellAbility> _spells = [];
    // With artefacts equipped this is a new list each call (an artefact may swap spells, NecromancyArtefact), so
    // adding to it does nothing: learn spells with LearnSpell
    public List<SpellAbility> Spells
    {
        get { return Modified._spells; }
        set { _spells = value; }
    }

    // The hero can cast once per round. Reset by Battle.StartNewRound.
    public bool HasCastThisRound;

    // The hero with every equipped artefact applied in order, or itself without artefacts. The getters read its
    // fields, so this never recurses.
    private Hero Modified
    {
        get
        {
            if (Artefacts.Count == 0)
                return this;
            Hero modified = CopyBase();
            foreach (Artefact artefact in Artefacts)
                modified = artefact.Apply(modified);
            return modified;
        }
    }

    // A new hero with this one's own values and spells (a copy of the list), without artefacts, for Artefact.Apply
    public Hero CopyBase() => new()
    {
        _mana = _mana,
        _strength = _strength,
        _knowledge = _knowledge,
        _defense = _defense,
        _maxManaBonus = _maxManaBonus,
        _spells = [.. _spells],
        HasCastThisRound = HasCastThisRound,
    };

    // Bonus applied to a stack stat by a hero stat. TBD: +10% per point for now.
    public static int ApplyBonus(int stat, uint heroStat)
    {
        return Mathf.RoundToInt(stat * (1 + heroStat * 0.1f));
    }

    public void LearnSpell(SpellAbility spell)
    {
        if (!_spells.Contains(spell))
            _spells.Add(spell);
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
