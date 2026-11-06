using System.Collections.Generic;
using System.Linq;

// Beasts: every stack for itself. It attacks its target if it can reach it this turn, otherwise the nearest enemy
// it can reach. With nothing in reach it walks toward its target. The first target is a random enemy; a dead target
// is replaced by the nearest enemy, and an attacked stack takes its attacker (BattleAI.OnStackAttacked).
// Casters cast first while they have the mana (ChooseCast), all at random; the hero casts on a random turn each round.
public class WildAI : BattleAI
{
    public WildAI(Battle battle) : base(battle) { }

    protected override TurnOutput DecideStackTurn(Stack stack, Reachability reachability)
    {
        Stack? target = GetTarget(stack) ?? PickTarget(stack);
        if (target == null)
            return new TurnOutput();

        if (ChooseCast(stack, GetOwnCastOptions(stack)) is CastOption cast)
            return CastOutput(cast);

        if (!stack.HasMelee && !stack.HasLongRange)
            return IdleTurn(stack, reachability);

        if (Attack(stack, target, reachability) is TurnOutput attack)
            return attack;

        List<Stack> attackable = GetAttackableTargets(stack, reachability);
        if (attackable.Count > 0)
        {
            Stack nearest = attackable.OrderBy(e => StepsTo(stack, e) ?? int.MaxValue).ThenBy(e => BoardDistance(stack, e)).First();
            return Attack(stack, nearest, reachability) ?? new TurnOutput();
        }

        return MoveToward(stack, target, reachability);
    }

    // On a random one of the army's turns this round: each turn casts with chance 1 / turns left (this one included),
    // so the hero always casts by the army's last turn
    protected override bool ShouldCastHeroSpell(Stack stack) => Random.Next(RemainingArmyTurns(stack) + 1) == 0;

    // A random revive if any would bring someone back; otherwise a random heal that doesn't overheal its ally;
    // otherwise a random damage spell on the caster's target
    protected override CastOption? ChooseCast(Stack caster, List<CastOption> options)
    {
        List<CastOption> revives = options.Where(o => o.Estimate.Revives > 0 && IsAlly(caster, o.Target)).ToList();
        if (revives.Count > 0)
            return RandomItem(revives);

        List<CastOption> heals = options
            .Where(o => o.Estimate.Healing > 0 && o.Estimate.Overheal == 0 && IsAlly(caster, o.Target)).ToList();
        if (heals.Count > 0)
            return RandomItem(heals);

        Stack? target = GetTarget(caster);
        List<CastOption> attacks = options.Where(o => o.Target == target && o.Estimate.Damage > 0).ToList();
        return attacks.Count > 0 ? RandomItem(attacks) : null;
    }

    // A random enemy the first time, the nearest one after its target died
    private Stack? PickTarget(Stack stack)
    {
        List<Stack> enemies = Battle.GetEnemies(stack);
        if (enemies.Count == 0)
            return null;

        Stack target = Targets.ContainsKey(stack) ? GetNearestEnemy(stack)! : RandomItem(enemies);
        Targets[stack] = target;
        return target;
    }
}
