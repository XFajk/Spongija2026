using Godot;
using System.Threading.Tasks;

[GlobalClass]
public abstract partial class MeleeAttackAbility : Resource, IAbility
{
    public abstract Task Use(Stack user, Stack target, Arena arena);

    // The damage Use would deal to the target, without dealing it (for previews and the AI). Use must deal exactly this.
    public abstract int GetDamage(Stack user, Stack target);
}
