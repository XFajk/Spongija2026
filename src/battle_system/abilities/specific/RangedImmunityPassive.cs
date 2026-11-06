using Godot;
using System.Threading.Tasks;

// Long range attacks can't target the unit (Battle.GetRangedTargets leaves it out); arrows pass through ghosts.
// Melee and spells still hit it. Always on: it's checked by Stack.IsImmuneToRanged, never used as a turn or round
// passive.
[GlobalClass]
public partial class RangedImmunityPassive : PassiveAbility
{
    public override Task Use(Stack user, Stack target, Arena arena) => Task.CompletedTask;
}
