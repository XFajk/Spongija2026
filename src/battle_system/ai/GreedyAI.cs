using System;
using System.Collections.Generic;
using System.Linq;

// Ranks the enemies and goes after the best one. Weak targeting ranks by how much of the enemy the stack can kill
// in one hit (the weakest for this stack first); strong targeting by how hard the enemy would hit this stack.
// A stack that can shoot attacks the best target it can hit. A melee-only stack weighs rank against distance: it
// picks the enemy with the lowest rank + turns of walking, so a slightly weaker enemy far away loses to a close one.
// Damage lands before the next stack decides, so stacks pile onto a hurt enemy until it's dead without planning.
// A stack that's attacked goes after its attacker instead (BattleAI.OnStackAttacked) until the attacker dies.
// Casters cast first while they have the mana, picking the cast that wastes the least (ChooseCast); the hero casts
// on the army's last turn of each round.
public class GreedyAI : BattleAI
{
    // A caster heals an ally only once its top unit is missing at least this share of its health
    public const float HealThreshold = 0.25f;

    private readonly bool _strongTargeting;

    public GreedyAI(Battle battle, bool strongTargeting) : base(battle)
    {
        _strongTargeting = strongTargeting;
    }

    protected override TurnOutput DecideStackTurn(Stack stack, Reachability reachability)
    {
        if (ChooseCast(stack, GetOwnCastOptions(stack)) is CastOption cast)
            return CastOutput(cast);

        if (!stack.HasMelee && !stack.HasLongRange)
            return IdleTurn(stack, reachability);

        if (GetTarget(stack) is Stack attacker)
            return Attack(stack, attacker, reachability) ?? MoveToward(stack, attacker, reachability);

        List<Stack> ranking = RankEnemies(stack);
        if (ranking.Count == 0)
            return new TurnOutput();

        if (stack.HasLongRange)
        {
            List<Stack> attackable = GetAttackableTargets(stack, reachability);
            Stack? best = ranking.FirstOrDefault(attackable.Contains);
            if (best != null && Attack(stack, best, reachability) is TurnOutput shot)
                return shot;
        }

        // The enemy with the lowest rank + turns away; unreachable ones are skipped. If every one is walled in, it
        // still heads for the top one (MoveToward gets as close as it can on the board) instead of freezing.
        Stack target = ranking[0];
        int bestCost = int.MaxValue;
        for (int rank = 0; rank < ranking.Count; rank++)
        {
            if (TurnsTo(stack, ranking[rank], reachability) is not int turns || rank + turns >= bestCost)
                continue;
            target = ranking[rank];
            bestCost = rank + turns;
        }

        return Attack(stack, target, reachability) ?? MoveToward(stack, target, reachability);
    }

    // Saves the hero's spell for the army's last turn this round, when the board is as settled as it gets
    protected override bool ShouldCastHeroSpell(Stack stack) => RemainingArmyTurns(stack) == 0;

    // Revives the slowest ally that can be revived; otherwise heals the top ally in need (RankAllies) with the
    // least overheal; otherwise hits the top enemy (or its attacker) with the cheapest kill: the stack-killing cast
    // with the least overkill, or the most damage when nothing kills it
    protected override CastOption? ChooseCast(Stack caster, List<CastOption> options)
    {
        List<CastOption> revives = options.Where(o => o.Estimate.Revives > 0 && IsAlly(caster, o.Target)).ToList();
        if (revives.Count > 0)
        {
            Stack slowest = revives.Select(o => o.Target).MinBy(s => s.GetSpeedTier())!;
            return revives.Where(o => o.Target == slowest)
                .OrderBy(o => o.Estimate.PotentialRevives - o.Estimate.Revives)
                .ThenByDescending(o => o.Estimate.Revives)
                .First();
        }

        foreach (Stack ally in RankAllies(caster))
        {
            if (MissingHealth(ally) < HealThreshold * ally.Unit.MaxHealth)
                continue;
            List<CastOption> heals = options.Where(o => o.Target == ally && o.Estimate.Healing > 0).ToList();
            if (heals.Count > 0)
                return heals.OrderBy(o => o.Estimate.Overheal).ThenByDescending(o => o.Estimate.Healing).First();
        }

        Stack? target = GetTarget(caster) ?? RankEnemies(caster).FirstOrDefault();
        List<CastOption> attacks = options.Where(o => o.Target == target && o.Estimate.Damage > 0).ToList();
        if (attacks.Count == 0)
            return null;
        List<CastOption> kills = attacks.Where(o => o.Estimate.KillsStack(o.Target)).ToList();
        return kills.Count > 0
            ? kills.MinBy(o => o.Estimate.Overkill)
            : attacks.MaxBy(o => o.Estimate.Damage);
    }

    // Best target first
    private List<Stack> RankEnemies(Stack stack)
    {
        List<Stack> enemies = Battle.GetEnemies(stack);
        return _strongTargeting
            ? enemies.OrderByDescending(e => Threat(e, stack)).ThenByDescending(e => KillFraction(stack, e)).ToList()
            : enemies.OrderByDescending(e => KillFraction(stack, e)).ThenByDescending(e => Threat(e, stack)).ToList();
    }

    // Allies to heal first: weak targeting the most hurt top unit, strong targeting the ally that hits the enemy hardest
    private List<Stack> RankAllies(Stack stack)
    {
        List<Stack> allies = Battle.GetAllies(stack);
        return _strongTargeting
            ? allies.OrderByDescending(a => Battle.GetEnemies(a).Select(e => Threat(a, e)).DefaultIfEmpty(0).Max()).ToList()
            : allies.OrderByDescending(MissingHealth).ToList();
    }

    // Share of the target's total health one attack by the stack takes, 0..1
    private static float KillFraction(Stack stack, Stack target) =>
        Math.Min(1f, GetAttackDamage(stack, target) / (float)Math.Max(1, target.GetTotalHealth()));

    // Damage the attacker's attack would do to the stack
    private static int Threat(Stack attacker, Stack stack) => GetAttackDamage(attacker, stack);
}
