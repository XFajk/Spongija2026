using Godot;
using System.Threading.Tasks;

[GlobalClass]
public abstract partial class MeleeAttackAbility : Resource, IAbility
{
    public abstract Task Use(Stack user, Stack target, Arena arena);
}
