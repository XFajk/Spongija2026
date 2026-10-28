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

    // Runtime state, set by Stack.AddEffect. Effects are duplicated per stack.
    public int RemainingDuration { get; set; }

    public abstract Task Apply(Stack target, Arena arena);
}
