using Godot;
using System.Threading.Tasks;

public interface IEffect
{
    int StrengthModifier { get; }
    int DefenseModifier { get; }
    int SpeedTierModifier { get; }

    // Number of applications before the effect expires.
    int Duration { get; }
    int RemainingDuration { get; set; }

    // Played on the stack's unit for as long as the effect is on it (the scene's root must be an AbilityVisualEffect,
    // set to Endless). Null for none.
    PackedScene? VisualEffect { get; }

    // The effect this one was copied from (Stack.AddEffect), so the same effect isn't on a stack twice. Runtime state.
    IEffect? Source { get; set; }

    bool IsExpired => RemainingDuration <= 0;

    // The speed tier with this effect, from the speed tier with the effects before it (Stack.GetSpeedTier).
    // By default adds SpeedTierModifier.
    int ModifySpeedTier(int speedTier);

    Task Apply(Stack target, Arena arena);
}
