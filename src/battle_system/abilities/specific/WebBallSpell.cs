using Godot;

// A fireball that also slows the target it hits, if the target survives. Hitting a stack that's already slowed
// only starts the slow's turns over (Stack.AddEffect).
[GlobalClass]
public partial class WebBallSpell : FireBallSpell
{
    // The slow's visual effect and MinSpeedTier; how much and how long come from the spell
    [Export]
    public SlowEffect? Effect { get; set; }

    // Speed tiers the slow takes away (SlowEffect.MinSpeedTier still caps it)
    [Export(PropertyHint.Range, "0,10")]
    public int SpeedTierLoss { get; set; } = 2;

    // The slowed stack's turns the slow lasts
    [Export(PropertyHint.Range, "1,10")]
    public int Turns { get; set; } = 2;

    protected override void Hit(Stack target, int damage)
    {
        base.Hit(target, damage);
        if (Effect == null || target.IsDead)
            return;

        // The stack's own copy, new or the one it already has
        var slow = (SlowEffect)target.AddEffect(Effect);
        slow.SpeedTierModifier = -SpeedTierLoss;
        slow.Duration = Turns;
        slow.RemainingDuration = Turns;
    }
}
