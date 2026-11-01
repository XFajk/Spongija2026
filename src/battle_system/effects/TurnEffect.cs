using Godot;
using System.Threading.Tasks;

// Applied at the end of the affected stack's turn.
[GlobalClass]
public abstract partial class TurnEffect : Resource, IEffect
{
    [ExportGroup("Modifiers")]
    [Export]
    public int StrengthModifier { get; set; }

    [Export]
    public int DefenseModifier { get; set; }

    [Export]
    public int SpeedTierModifier { get; set; }

    [ExportGroup("")]
    [Export]
    public int Duration { get; set; } = 1;

    [Export]
    public PackedScene? VisualEffect { get; set; }

    // Runtime state, set by Stack.AddEffect. Effects are duplicated per stack.
    public int RemainingDuration { get; set; }

    public IEffect? Source { get; set; }

    public virtual int ModifySpeedTier(int speedTier) => speedTier + SpeedTierModifier;

    public abstract Task Apply(Stack target, Arena arena);
}
