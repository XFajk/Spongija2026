using System.Threading.Tasks;

public interface IEffect
{
    int StrengthModifier { get; }
    int DefenseModifier { get; }
    int SpeedTierModifier { get; }

    // Number of applications before the effect expires.
    int Duration { get; }
    int RemainingDuration { get; set; }

    bool IsExpired => RemainingDuration <= 0;

    Task Apply(Stack target, Arena arena);
}
