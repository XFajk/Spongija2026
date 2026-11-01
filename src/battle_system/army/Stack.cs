using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class Stack
{
    public Unit Unit { get; }

    // Set by Army.AddStack.
    public Army? Army { get; set; }

    public uint Quantity;

    private int _topHealth;
    // Health of the top unit of the stack.
    public int TopHealth
    {
        get { return _topHealth; }
        set { _topHealth = Math.Clamp(value, 1, Math.Max(1, Unit.MaxHealth)); }
    }

    private uint _mana;
    public uint Mana
    {
        get { return _mana; }
        set { _mana = Math.Clamp(value, 0, Unit.MaxMana); }
    }

    public List<IEffect> Effects { get; } = new();

    // Top-left tile of the stack's footprint. A size 2 stack also covers the tiles right of and below it.
    // Set through Battle so the board's occupancy stays in sync.
    public Vector2I Anchor;

    // True once the stack has retaliated this round. Reset at round end and battle end.
    public bool Retaliated;

    // True once the stack has waited this round (it can wait only once). Reset at round end and battle end.
    public bool Waited;

    public UnitNode? NodeRepresentation { get; set; }

    // The quantity the stack was made with, at the start of the battle: the most Revive brings it back to.
    public uint StartingQuantity { get; }

    public Stack(Unit unit, uint quantity)
    {
        Unit = unit;
        Quantity = quantity;
        StartingQuantity = quantity;
        TopHealth = unit.MaxHealth;
        Mana = unit.StartingMana;
    }

    public Hero? Hero => Army?.Hero;

    public bool IsDead => Quantity == 0;

    public bool HasMelee => Unit.MeleeAttack != null;

    public bool HasLongRange => Unit.LongRangeAttack != null;

    public int Size => Math.Max(1, Unit.Size);

    // Tiles the stack would cover with its anchor on the given tile.
    public List<Vector2I> GetTilesAt(Vector2I anchor)
    {
        var tiles = new List<Vector2I>(Size * Size);
        for (int x = 0; x < Size; x++)
            for (int y = 0; y < Size; y++)
                tiles.Add(anchor + new Vector2I(x, y));
        return tiles;
    }

    public List<Vector2I> GetOccupiedTiles() => GetTilesAt(Anchor);

    // Center of the footprint in tile units, e.g. for placing the model. (0.5, 0.5) is the middle of tile (0, 0).
    public Vector2 GetCenter() => (Vector2)Anchor + new Vector2(Size, Size) / 2f;

    // Stats: Unit base value + effect modifiers, optionally multiplied by the hero bonus.

    public int GetStrength(bool includeHero = true)
    {
        int strength = Unit.Strength + Effects.Sum(e => e.StrengthModifier);
        if (includeHero && Hero != null)
            strength = Hero.ApplyBonus(strength, Hero.Strength);
        return Math.Max(0, strength);
    }

    public int GetDefense(bool includeHero = true)
    {
        int defense = Unit.Defense + Effects.Sum(e => e.DefenseModifier);
        if (includeHero && Hero != null)
            defense = Hero.ApplyBonus(defense, Hero.Defense);
        return Math.Max(0, defense);
    }

    public int GetSpeedTier()
    {
        // Effect by effect, so an effect can cap what it does (SlowEffect)
        return Math.Max(0, Effects.Aggregate(Unit.SpeedTier, (speedTier, e) => e.ModifySpeedTier(speedTier)));
    }

    public int GetTotalHealth()
    {
        if (IsDead)
            return 0;
        return (int)(Quantity - 1) * Unit.MaxHealth + TopHealth;
    }

    // How many units TakeDamage(damage) would kill, without changing anything
    public uint GetUnitsKilledBy(int damage)
    {
        if (damage <= 0 || IsDead)
            return 0;
        int remaining = GetTotalHealth() - damage;
        if (remaining <= 0)
            return Quantity;
        int maxHealth = Math.Max(1, Unit.MaxHealth);
        return Quantity - (uint)((remaining + maxHealth - 1) / maxHealth);
    }

    // Removes health from the top unit downwards. Returns how many units died.
    public uint TakeDamage(int damage)
    {
        if (damage <= 0 || IsDead)
            return 0;

        uint before = Quantity;
        int remaining = GetTotalHealth() - damage;
        if (remaining <= 0)
        {
            Quantity = 0;
            return before;
        }

        int maxHealth = Math.Max(1, Unit.MaxHealth);
        Quantity = (uint)((remaining + maxHealth - 1) / maxHealth);
        TopHealth = remaining - (int)(Quantity - 1) * maxHealth;
        return before - Quantity;
    }

    // Gives health back to the top unit, up to its max; dead units stay dead. Returns how much was healed.
    public int Heal(int amount)
    {
        if (amount <= 0 || IsDead)
            return 0;

        int before = TopHealth;
        TopHealth += amount;
        return TopHealth - before;
    }

    // Health Revive can still give back: the top unit's missing health plus the units lost this battle
    public int GetRevivableHealth() =>
        IsDead ? 0 : Math.Max(0, (int)StartingQuantity * Unit.MaxHealth - GetTotalHealth());

    // How many units Revive(amount) would bring back, without changing anything
    public uint GetUnitsRevivedBy(int amount)
    {
        if (amount <= 0 || IsDead)
            return 0;
        int total = GetTotalHealth() + Math.Min(amount, GetRevivableHealth());
        int maxHealth = Math.Max(1, Unit.MaxHealth);
        return (uint)((total + maxHealth - 1) / maxHealth) - Quantity;
    }

    // Necromancy: heals the top unit, then brings dead units back, up to StartingQuantity (a dead stack is gone
    // from the battle, so it can't be revived). Returns how many units came back.
    public uint Revive(int amount)
    {
        if (amount <= 0 || IsDead)
            return 0;

        uint before = Quantity;
        int total = GetTotalHealth() + Math.Min(amount, GetRevivableHealth());
        int maxHealth = Math.Max(1, Unit.MaxHealth);
        Quantity = (uint)((total + maxHealth - 1) / maxHealth);
        TopHealth = total - (int)(Quantity - 1) * maxHealth;
        return Quantity - before;
    }

    // Whether long range attacks can't target it (a passive, e.g. the forest ghosts')
    public bool IsImmuneToRanged => Unit.Passives.Any(p => p is RangedImmunityPassive);

    // Effects are Resources shared through .tres files, so every stack gets its own copy, and its visual effect plays
    // on the unit. The same effect isn't added twice: if it's already on the stack, its duration starts over instead.
    public IEffect AddEffect(IEffect effect)
    {
        IEffect? existing = Effects.FirstOrDefault(e => e.Source == effect);
        if (existing != null)
        {
            existing.RemainingDuration = existing.Duration;
            return existing;
        }

        IEffect copy = effect is Resource resource ? (IEffect)resource.Duplicate() : effect;
        copy.Source = effect;
        copy.RemainingDuration = copy.Duration;
        Effects.Add(copy);
        NodeRepresentation?.ShowEffect(copy);
        return copy;
    }

    public void RemoveExpiredEffects()
    {
        foreach (IEffect effect in Effects.Where(e => e.IsExpired))
            NodeRepresentation?.HideEffect(effect);
        Effects.RemoveAll(e => e.IsExpired);
    }

    public List<TurnEffect> GetTurnEffects() => Effects.OfType<TurnEffect>().ToList();

    public List<RoundEffect> GetRoundEffects() => Effects.OfType<RoundEffect>().ToList();

    public List<TurnPassiveAbility> GetTurnPassives() => Unit.Passives.OfType<TurnPassiveAbility>().ToList();

    public List<RoundPassiveAbility> GetRoundPassives() => Unit.Passives.OfType<RoundPassiveAbility>().ToList();

    // The unit's own spells it has enough mana for. Hero spells are in Hero.GetUsableSpells.
    public List<SpellAbility> GetCastableSpells() => Unit.Spells.Where(s => Mana >= s.ManaCost).ToList();
}
