using Godot;
using System;
using System.Threading.Tasks;

[GlobalClass]
public partial class BasicMeleeAttackAbility : MeleeAttackAbility
{
    [Export(PropertyHint.Range, "0,1")]
    public float BasicAttackMultiplier { get; set; } = 1.0f;

    public override Task Use(Stack user, Stack target, Arena arena)
    {
        target.TakeDamage(user.GetStrength() * (int)user.Quantity);

        return Task.CompletedTask;
    }
}