using Godot;
using System.Threading.Tasks;

// Raises an allied stack's fallen: gives health back to its top unit, then brings its dead units back, up to the
// quantity it started the battle with (Stack.Revive). A stack that was wiped out is gone and can't be raised.
[GlobalClass]
public partial class NecromancySpell : SpellAbility
{
    // Health given back by a hero's cast
    [Export(PropertyHint.Range, "0,1000")]
    public int Amount { get; set; } = 60;

    // Health given back per unit in the casting stack, for a unit's own cast
    [Export(PropertyHint.Range, "0,100")]
    public int AmountPerCaster { get; set; } = 10;

    // Played on the target. The scene's root must be an AbilityVisualEffect.
    [Export]
    public PackedScene? VisualEffect { get; set; }

    public NecromancySpell()
    {
        Targets = TargetSide.Allies;
    }

    // A unit's cast scales with its quantity, a hero's doesn't. Use and Evaluate both use this.
    private int GetAmount(Stack caster, bool fromHero) => fromHero ? Amount : AmountPerCaster * (int)caster.Quantity;

    public override SpellEstimate Evaluate(Stack caster, Stack target, Battle battle) =>
        SpellEstimate.ForRevive(GetAmount(caster, fromHero: false), target);

    public override SpellEstimate EvaluateFromHero(Stack caster, Stack target, Battle battle) =>
        SpellEstimate.ForRevive(GetAmount(caster, fromHero: true), target);

    public override async Task Use(Stack user, Stack target, Arena arena)
    {
        if (user.NodeRepresentation is not UnitNode casterNode)
        {
            await RaiseTarget(user, target, GetAmount(user, fromHero: false), arena);
            return;
        }

        if (target != user)
            casterNode.FaceTowards(target);
        // The raising starts at the cast's hit frame
        UnitAnimation cast = casterNode.PlaySpellCast();
        await cast.Hit;
        await Task.WhenAll(RaiseTarget(user, target, GetAmount(user, fromHero: false), arena), cast.Finished);
        casterNode.FaceEnemySide();
    }

    // The hero casts it (Arena plays the hero's own cast), so the acting stack plays no cast animation
    public override Task UseFromHero(Stack caster, Vector3 origin, Stack target, Arena arena) =>
        RaiseTarget(caster, target, GetAmount(caster, fromHero: true), arena);

    // The effect on the target, then the revive. The camera follows the effect if it's showing the caster.
    private async Task RaiseTarget(Stack caster, Stack target, int amount, Arena arena)
    {
        if (VisualEffect != null && target.NodeRepresentation is UnitNode targetNode)
        {
            var effect = AbilityVisualEffect.Spawn(arena, VisualEffect, targetNode.GlobalPosition);
            if (effect != null)
            {
                arena.FollowWithCamera(caster, effect);
                await effect.ToSignal(effect, AbilityVisualEffect.SignalName.Finished);
                effect.QueueFree();
            }
        }
        target.Revive(amount);
    }
}
