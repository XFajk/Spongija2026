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

    // The same for unit and hero casts. Use and Evaluate both use this.
    private int GetHealing() => Amount;

    public override SpellEstimate Evaluate(Stack caster, Stack target, Battle battle) =>
        SpellEstimate.ForHealing(GetHealing(), target);

    public override async Task Use(Stack user, Stack target, Arena arena)
    {

        if (VisualEffect == null || target.NodeRepresentation is not UnitNode targetNode)
        {
            target.Heal(GetHealing());
            return;
        }

        var effect = AbilityVisualEffect.Spawn(arena, VisualEffect, targetNode.GlobalPosition);
        if (effect != null)
        {
            await effect.ToSignal(effect, AbilityVisualEffect.SignalName.Finished);
            effect.QueueFree();
        }
        // Outside the if, so the heal still happens when the effect fails to spawn
        target.Heal(GetHealing());
    }
}
