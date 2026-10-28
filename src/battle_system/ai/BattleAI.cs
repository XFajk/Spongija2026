using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// Decides the computer's turns. Plain C#, like Battle: it only asks Battle and builds a TurnOutput, as the player's
// clicks do, so it follows the same rules. One instance lives for the whole battle, so strategies can remember
// things between turns (each stack's target).
public abstract class BattleAI
{
    protected Battle Battle { get; }

    protected Random Random { get; } = new();

    // Each of the computer's stacks' current target: set by the strategy, and by OnStackAttacked to whoever
    // last attacked that stack.
    protected Dictionary<Stack, Stack> Targets { get; } = new();

    protected BattleAI(Battle battle)
    {
        Battle = battle;
    }

    public static BattleAI Create(EnemyStrategy strategy, Battle battle) => strategy switch
    {
        EnemyStrategy.GreedyWeak => new GreedyAI(battle, strongTargeting: false),
        EnemyStrategy.GreedyStrong => new GreedyAI(battle, strongTargeting: true),
        _ => new WildAI(battle),
    };

    // The stack's turn: what the stack itself does, plus a hero spell when the strategy says it's time.
    // The reachability is the one searched at the start of its turn.
    public TurnOutput DecideTurn(Stack stack, Reachability reachability)
    {
        TurnOutput output = DecideStackTurn(stack, reachability);
        List<SpellAbility> heroSpells = GetHeroSpells(stack);
        if (heroSpells.Count > 0 && ShouldCastHeroSpell(stack)
            && ChooseCast(stack, GetCastOptions(stack, heroSpells, fromHero: true)) is CastOption cast)
            output.HeroSpell = (cast.Spell, cast.Target);
        return output;
    }

    // What the stack does with its own turn: cast, attack, move
    protected abstract TurnOutput DecideStackTurn(Stack stack, Reachability reachability);

    // Whether the army's hero casts during this stack's turn. Only asked while it can cast something this round.
    protected abstract bool ShouldCastHeroSpell(Stack stack);

    // Which of the casts to make, or null for none of them. Used for the stack's own spells and the hero's.
    protected abstract CastOption? ChooseCast(Stack caster, List<CastOption> options);

    // Called by Arena after an attack or spell lands, not for retaliations. A computer stack hit by an enemy
    // takes the attacker as its target.
    public virtual void OnStackAttacked(Stack attacker, Stack target)
    {
        if (target.IsDead || attacker.IsDead || Battle.IsPlayerStack(target) || !Battle.IsPlayerStack(attacker))
            return;
        Targets[target] = attacker;
    }

    // The stack's living target, or null (none yet, or it died)
    protected Stack? GetTarget(Stack stack) =>
        Targets.TryGetValue(stack, out Stack? target) && !target.IsDead ? target : null;

    #region Attacking

    // Enemies the stack can hit this turn: in melee (moving first if needed) or by shooting
    public List<Stack> GetAttackableTargets(Stack stack, Reachability reachability) =>
        Battle.GetMeleeTargets(stack, reachability).Concat(Battle.GetRangedTargets(stack, reachability)).ToList();

    // Shoots the target if it can, else melees it from the attack anchor with the shortest walk.
    // Null if it can do neither this turn.
    protected TurnOutput? Attack(Stack stack, Stack target, Reachability reachability)
    {
        if (stack.Unit.LongRangeAttack is LongRangeAttackAbility ranged && Battle.CanShoot(stack, target, reachability))
        {
            var shot = new TurnOutput();
            shot.Add(ranged, target);
            return shot;
        }

        if (stack.Unit.MeleeAttack is not MeleeAttackAbility melee)
            return null;
        List<Vector2I> anchors = Battle.GetMeleeAttackAnchors(stack, target, reachability);
        if (anchors.Count == 0)
            return null;

        Vector2I anchor = anchors.MinBy(a => a == stack.Anchor ? 0 : reachability.GetPath(a).Count);
        var output = new TurnOutput();
        if (anchor != stack.Anchor)
            output.MoveTo = anchor;
        output.Add(melee, target);
        return output;
    }

    #endregion

    #region Casting

    // One possible cast: a spell on a target, and what it would do there
    protected readonly record struct CastOption(SpellAbility Spell, Stack Target, SpellEstimate Estimate);

    // Every target each spell can be cast on, with what it would do there
    protected List<CastOption> GetCastOptions(Stack caster, IEnumerable<SpellAbility> spells, bool fromHero) =>
        spells.SelectMany(spell => Battle.GetSpellTargets(caster, spell).Select(target => new CastOption(spell, target,
            fromHero ? spell.EvaluateFromHero(caster, target, Battle) : spell.Evaluate(caster, target, Battle))))
        .ToList();

    // The stack's own spells it has the mana for
    protected List<CastOption> GetOwnCastOptions(Stack stack) =>
        GetCastOptions(stack, stack.GetCastableSpells(), fromHero: false);

    // The hero's spells it can cast now: unlocked, enough mana, not cast yet this round
    protected static List<SpellAbility> GetHeroSpells(Stack stack) =>
        stack.Hero is Hero hero ? hero.GetKnownSpells().Where(hero.CanCast).ToList() : new();

    // The stack's own spell as its whole turn: the caster stays where it is
    protected static TurnOutput CastOutput(CastOption cast)
    {
        var output = new TurnOutput();
        output.Add(cast.Spell, cast.Target);
        return output;
    }

    // Turns the stack's army still has this round after this one
    protected int RemainingArmyTurns(Stack stack) =>
        Battle.RoundQueue.Count(s => !s.IsDead && s != stack && Battle.IsPlayerStack(s) == Battle.IsPlayerStack(stack));

    protected bool IsAlly(Stack stack, Stack other) => Battle.IsPlayerStack(stack) == Battle.IsPlayerStack(other);

    // Top unit's lost health, what a heal can restore
    protected static int MissingHealth(Stack stack) => stack.Unit.MaxHealth - stack.TopHealth;

    #endregion

    #region Distances and movement

    // Steps the stack needs to get next to the target, around other stacks; 0 if it's already next to it.
    // Null if it can't get there at all.
    protected int? StepsTo(Stack stack, Stack target) =>
        Battle.GetStepsToAttack(stack, target).TryGetValue(stack.Anchor, out int steps) ? steps : null;

    // Whole turns of walking before the stack can attack the target: 0 if it can this turn.
    // Null if it can't get to it at all (a ranged stack can always shoot, so it's never null for them).
    protected int? TurnsTo(Stack stack, Stack target, Reachability reachability)
    {
        if (Battle.CanShoot(stack, target, reachability) || Battle.CanMelee(stack, target, reachability))
            return 0;
        int? steps = StepsTo(stack, target);
        return steps is int s ? Mathf.CeilToInt(s / (float)Math.Max(1, stack.GetSpeedTier())) : null;
    }

    // The enemy that's the fewest steps away, ties broken by distance on the board; unreachable ones count last
    protected Stack? GetNearestEnemy(Stack stack) => Battle.GetEnemies(stack)
        .OrderBy(e => StepsTo(stack, e) ?? int.MaxValue)
        .ThenBy(e => BoardDistance(stack, e))
        .FirstOrDefault();

    // Moves as far toward the target as it can this turn: to the reachable anchor with the fewest steps left to it.
    // If the target is walled in, it gets as close as it can on the board instead. Empty output (stays) if no
    // reachable anchor is closer than where it stands.
    protected TurnOutput MoveToward(Stack stack, Stack target, Reachability reachability)
    {
        Dictionary<Vector2I, int> steps = Battle.GetStepsToAttack(stack, target);
        IEnumerable<Vector2I> candidates = reachability.Anchors.Prepend(stack.Anchor);

        Vector2I best = steps.Count > 0
            ? candidates.MinBy(a => steps.TryGetValue(a, out int s) ? s : int.MaxValue)
            : candidates.MinBy(a => BoardDistance(a, stack.Size, target));

        var output = new TurnOutput();
        if (best != stack.Anchor)
            output.MoveTo = best;
        return output;
    }

    // For a stack with no attack: stays put while it still has spells it can cast (casters don't move to keep
    // casting), and wanders to a random reachable anchor once it has none
    protected TurnOutput IdleTurn(Stack stack, Reachability reachability)
    {
        var output = new TurnOutput();
        List<Vector2I> anchors = reachability.Anchors.ToList();
        if (stack.GetCastableSpells().Count == 0 && anchors.Count > 0)
            output.MoveTo = RandomItem(anchors);
        return output;
    }

    protected T RandomItem<T>(IReadOnlyList<T> items) => items[Random.Next(items.Count)];

    // Distance between footprint centers in tiles, ignoring what's in the way
    protected static float BoardDistance(Stack a, Stack b) => a.GetCenter().DistanceTo(b.GetCenter());

    private static float BoardDistance(Vector2I anchor, int size, Stack target) =>
        ((Vector2)anchor + new Vector2(size, size) / 2f).DistanceTo(target.GetCenter());

    #endregion
}
