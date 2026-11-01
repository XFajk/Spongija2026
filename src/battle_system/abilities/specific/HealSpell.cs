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
        if (user.NodeRepresentation is not UnitNode casterNode)
        {
            await HealTarget(user, target, arena);
            return;
        }

        if (target != user)
            casterNode.FaceTowards(target);
        // The heal starts at the cast's hit frame
        UnitAnimation cast = casterNode.PlaySpellCast();
        await cast.Hit;
        await Task.WhenAll(HealTarget(user, target, arena), cast.Finished);
        casterNode.FaceEnemySide();
    }

    // The hero casts it, so the acting stack plays no cast animation
    public override Task UseFromHero(Stack caster, Vector3 origin, Stack target, Arena arena) => HealTarget(caster, target, arena);

    // The effect on the target, then the heal. The camera follows the effect if it's showing the caster.
    private async Task HealTarget(Stack caster, Stack target, Arena arena)
    {
        if (VisualEffect == null || target.NodeRepresentation is not UnitNode targetNode)
        {
            target.Heal(GetHealing());
            return;
        }

        var effect = AbilityVisualEffect.Spawn(arena, VisualEffect, targetNode.GlobalPosition);
        if (effect != null)
        {
            arena.FollowWithCamera(caster, effect);
            await effect.ToSignal(effect, AbilityVisualEffect.SignalName.Finished);
            effect.QueueFree();
        }
        // Outside the if, so the heal still happens when the effect fails to spawn
        target.Heal(GetHealing());
    }
}
