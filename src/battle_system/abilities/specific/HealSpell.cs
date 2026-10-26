using Godot;
using System.Threading.Tasks;

[GlobalClass]
public partial class HealSpell : SpellAbility
{
    // Health given back to the target's top unit (it doesn't revive dead units, as in HoMM)
    [Export(PropertyHint.Range, "0,1000")]
    public int Amount { get; set; } = 30;

    // Played on the target. The scene's root must be an AbilityVisualEffect.
    [Export]
    public PackedScene? VisualEffect { get; set; }

    public override async Task Use(Stack user, Stack target, Arena arena)
    {
        target.Heal(Amount);

        if (VisualEffect == null || target.NodeRepresentation is not UnitNode targetNode)
            return;

        var effect = AbilityVisualEffect.Spawn(arena, VisualEffect, targetNode.GlobalPosition);
        if (effect != null)
        {
            await effect.ToSignal(effect, AbilityVisualEffect.SignalName.Finished);
            effect.QueueFree();
        }
    }
}
