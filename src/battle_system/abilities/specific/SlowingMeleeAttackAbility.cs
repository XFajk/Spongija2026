using Godot;

// A melee attack that also slows the target it hits, if the target survives (a spider's bite wrapping it in web).
// Hitting a stack that's already slowed only starts the slow's turns over (Stack.AddEffect). Retaliations slow too.
[GlobalClass]
public partial class SlowingMeleeAttackAbility : BasicMeleeAttackAbility
{
    // The slow's visual effect and MinSpeedTier; how much and how long come from the attack
    [Export]
    public SlowEffect? Effect { get; set; }

    // Speed tiers the slow takes away (SlowEffect.MinSpeedTier still caps it)
    [Export(PropertyHint.Range, "0,10")]
    public int SpeedTierLoss { get; set; } = 1;

    // The slowed stack's turns the slow lasts
    [Export(PropertyHint.Range, "1,10")]
    public int Turns { get; set; } = 1;

    protected override void Hit(Stack user, Stack target)
    {
        base.Hit(user, target);
        if (Effect != null)
            SlowEffect.Inflict(target, Effect, SpeedTierLoss, Turns);
    }
}
