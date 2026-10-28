using System;

// What one cast of a spell would do to one target, without doing it (SpellAbility.Evaluate). Facts only: the AI's
// strategies decide what they're worth. The "Potential" values are the raw amounts; the others are what really lands,
// so Potential - real is the overkill or overheal. E.g. 300 damage on a stack of 2 with 65 health each: Damage 130,
// PotentialDamage 300, UnitsKilled 2.
public readonly record struct SpellEstimate(
    int Damage = 0,
    int PotentialDamage = 0,
    int UnitsKilled = 0,
    int Healing = 0,
    int PotentialHealing = 0,
    int Revives = 0,
    int PotentialRevives = 0)
{
    // Damage through the target's units, top unit first, capped by its total health (as Stack.TakeDamage)
    public static SpellEstimate ForDamage(int potential, Stack target)
    {
        potential = Math.Max(0, potential);
        return new SpellEstimate(
            Damage: Math.Min(potential, target.GetTotalHealth()),
            PotentialDamage: potential,
            UnitsKilled: (int)target.GetUnitsKilledBy(potential));
    }

    // Healing on the target's top unit, capped by what it's missing (as Stack.Heal)
    public static SpellEstimate ForHealing(int potential, Stack target)
    {
        potential = Math.Max(0, potential);
        int missing = target.IsDead ? 0 : target.Unit.MaxHealth - target.TopHealth;
        return new SpellEstimate(Healing: Math.Min(potential, missing), PotentialHealing: potential);
    }

    // Whether the damage would wipe out the whole stack
    public bool KillsStack(Stack target) => Damage > 0 && Damage >= target.GetTotalHealth();

    public int Overkill => PotentialDamage - Damage;

    public int Overheal => PotentialHealing - Healing;
}
