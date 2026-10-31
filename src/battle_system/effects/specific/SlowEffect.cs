using Godot;
using System;
using System.Threading.Tasks;

// Lowers the speed tier by SpeedTierModifier (a negative number), but not below MinSpeedTier:
// with -2 and 3, speed 6 becomes 4, 4 becomes 3, and a stack at 3 or slower keeps its speed.
// Lasts Duration of the slowed stack's own turns.
[GlobalClass]
public partial class SlowEffect : TurnEffect
{
    [Export(PropertyHint.Range, "0,20")]
    public int MinSpeedTier { get; set; } = 3;

    public SlowEffect()
    {
        SpeedTierModifier = -2;
    }

    public override int ModifySpeedTier(int speedTier) =>
        speedTier <= MinSpeedTier ? speedTier : Math.Max(MinSpeedTier, speedTier + SpeedTierModifier);

    // Only modifies the speed tier, nothing happens at the end of the turn
    public override Task Apply(Stack target, Arena arena) => Task.CompletedTask;
}
