using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// Battle state and rules. No nodes: Arena asks it what's allowed and displays the answers,
// so the player and the AI go through the same checks.
public class Battle
{
    #region State

    private static readonly Vector2I[] Directions =
    [
        new(0, -1), new(-1, 0), new(1, 0), new(0, 1)
    ];

    public Army PlayerArmy { get; }
    public Army ComputerArmy { get; }

    public int Width { get; }
    public int Height { get; }

    public int Round { get; private set; }

    // Stacks that still have a turn this round, in order.
    public List<Stack> RoundQueue { get; } = new();

    private readonly Dictionary<Vector2I, Stack> _occupancy = new();

    // keepAnchors: place the stacks at the anchors they already have (set by the placement phase)
    // instead of the default starting anchors.
    public Battle(Army playerArmy, Army computerArmy, int width, int height, bool keepAnchors = false)
    {
        PlayerArmy = playerArmy;
        ComputerArmy = computerArmy;
        Width = width;
        Height = height;

        if (keepAnchors)
        {
            PlaceAtCurrentAnchors(playerArmy);
            PlaceAtCurrentAnchors(computerArmy);
        }
        else
        {
            PlaceArmy(playerArmy, leftSide: true);
            PlaceArmy(computerArmy, leftSide: false);
        }

        StartNewRound();
    }

    public IEnumerable<Stack> AllStacks => PlayerArmy.Stacks.Concat(ComputerArmy.Stacks).Where(s => !s.IsDead);

    #endregion

    #region Turn order

    // Speed tier descending, then Defense ascending (without the hero), then Quantity ascending.
    public void BuildRoundQueue()
    {
        RoundQueue.Clear();
        RoundQueue.AddRange(AllStacks
            .OrderByDescending(s => s.GetSpeedTier())
            .ThenBy(s => s.GetDefense(includeHero: false))
            .ThenBy(s => s.Quantity));
    }

    // The stack whose turn it is, or null when the round is over.
    public Stack? GetTurnInput()
    {
        RoundQueue.RemoveAll(s => s.IsDead);
        if (RoundQueue.Count == 0)
            return null;

        Stack stack = RoundQueue[0];
        RoundQueue.RemoveAt(0);
        return stack;
    }

    public bool CanWait(Stack stack) => !stack.Waited && !stack.IsDead;

    // Moves the stack's turn to the end of this round. Its turn was already taken off the queue by GetTurnInput.
    public bool Wait(Stack stack)
    {
        if (!CanWait(stack))
            return false;
        stack.Waited = true;
        RoundQueue.Add(stack);
        return true;
    }

    public void StartNewRound()
    {
        Round++;
        ResetRoundState();
        BuildRoundQueue();
    }

    public void EndBattle()
    {
        ResetRoundState();
        RoundQueue.Clear();
    }

    private void ResetRoundState()
    {
        foreach (Stack stack in PlayerArmy.Stacks.Concat(ComputerArmy.Stacks))
        {
            stack.Retaliated = false;
            stack.Waited = false;
        }
        if (PlayerArmy.Hero != null)
            PlayerArmy.Hero.HasCastThisRound = false;
        if (ComputerArmy.Hero != null)
            ComputerArmy.Hero.HasCastThisRound = false;
    }

    public bool IsOver => PlayerArmy.IsDefeated || ComputerArmy.IsDefeated;

    public Army? Winner
    {
        get
        {
            if (ComputerArmy.IsDefeated && !PlayerArmy.IsDefeated)
                return PlayerArmy;
            if (PlayerArmy.IsDefeated && !ComputerArmy.IsDefeated)
                return ComputerArmy;
            return null;
        }
    }

    #endregion

    #region Armies

    public bool IsPlayerStack(Stack stack) => stack.Army == PlayerArmy;

    public Army GetArmy(Stack stack) => IsPlayerStack(stack) ? PlayerArmy : ComputerArmy;

    public Army GetEnemyArmy(Stack stack) => IsPlayerStack(stack) ? ComputerArmy : PlayerArmy;

    public List<Stack> GetAllies(Stack stack) => GetArmy(stack).Stacks.Where(s => !s.IsDead).ToList();

    public List<Stack> GetEnemies(Stack stack) => GetEnemyArmy(stack).Stacks.Where(s => !s.IsDead).ToList();

    #endregion

    #region Tiles and movement

    public bool IsInBounds(Vector2I tile) => tile.X >= 0 && tile.Y >= 0 && tile.X < Width && tile.Y < Height;

    public bool IsOccupied(Vector2I tile) => _occupancy.ContainsKey(tile);

    public Stack? GetStackAt(Vector2I tile) => _occupancy.GetValueOrDefault(tile);

    public static bool AreAdjacent(Vector2I a, Vector2I b)
    {
        Vector2I d = (a - b).Abs();
        return Math.Max(d.X, d.Y) == 1;
    }

    // Whether two square footprints touch (including diagonally) without overlapping.
    public static bool AreAdjacent(Vector2I anchorA, int sizeA, Vector2I anchorB, int sizeB)
    {
        // Gap between the footprints on each axis; 0 means they overlap on that axis.
        int gapX = Math.Max(0, Math.Max(anchorB.X - (anchorA.X + sizeA - 1), anchorA.X - (anchorB.X + sizeB - 1)));
        int gapY = Math.Max(0, Math.Max(anchorB.Y - (anchorA.Y + sizeA - 1), anchorA.Y - (anchorB.Y + sizeB - 1)));
        return Math.Max(gapX, gapY) == 1;
    }

    public static bool AreAdjacent(Stack a, Stack b) => AreAdjacent(a.Anchor, a.Size, b.Anchor, b.Size);

    // Whether the stack's whole footprint fits with its anchor on the tile: in bounds and not on another stack.
    // The stack's own tiles count as free, since it moves out of them.
    public bool Fits(Stack stack, Vector2I anchor)
    {
        foreach (Vector2I tile in stack.GetTilesAt(anchor))
        {
            if (!IsInBounds(tile))
                return false;
            Stack? other = GetStackAt(tile);
            if (other != null && other != stack)
                return false;
        }
        return true;
    }

    // Breadth-first search over anchors: each step moves the whole stack one tile, and is only allowed
    // if the footprint fits at the new anchor, so large stacks can't squeeze through narrow gaps.
    // Up to speed tier steps. Remembers where each anchor was first reached from, for paths.
    public Reachability GetReachability(Stack stack)
    {
        var cameFrom = new Dictionary<Vector2I, Vector2I>();
        var visited = new HashSet<Vector2I> { stack.Anchor };
        var frontier = new List<Vector2I> { stack.Anchor };

        for (int step = 0; step < stack.GetSpeedTier() && frontier.Count > 0; step++)
        {
            var next = new List<Vector2I>();
            foreach (Vector2I anchor in frontier)
            {
                foreach (Vector2I direction in Directions)
                {
                    Vector2I neighbor = anchor + direction;
                    if (!Fits(stack, neighbor) || !visited.Add(neighbor))
                        continue;
                    cameFrom[neighbor] = anchor;
                    next.Add(neighbor);
                }
            }
            frontier = next;
        }
        return new Reachability(stack, stack.Anchor, cameFrom);
    }

    // Steps the stack needs from each anchor to an anchor it can melee the target from (next to it), walking around
    // other stacks like GetReachability but with no step limit. Anchors that can't get there at all are missing,
    // e.g. when the target is walled in. Moves are reversible, so this is a breadth-first search outward from the
    // anchors next to the target. Used by the AI for distances and for walking toward a target over several turns.
    public Dictionary<Vector2I, int> GetStepsToAttack(Stack stack, Stack target)
    {
        var steps = new Dictionary<Vector2I, int>();
        var frontier = new List<Vector2I>();
        for (int x = target.Anchor.X - stack.Size; x <= target.Anchor.X + target.Size; x++)
            for (int y = target.Anchor.Y - stack.Size; y <= target.Anchor.Y + target.Size; y++)
            {
                var anchor = new Vector2I(x, y);
                if (AreAdjacent(anchor, stack.Size, target.Anchor, target.Size) && Fits(stack, anchor))
                {
                    steps[anchor] = 0;
                    frontier.Add(anchor);
                }
            }

        for (int step = 1; frontier.Count > 0; step++)
        {
            var next = new List<Vector2I>();
            foreach (Vector2I anchor in frontier)
            {
                foreach (Vector2I direction in Directions)
                {
                    Vector2I neighbor = anchor + direction;
                    if (steps.ContainsKey(neighbor) || !Fits(stack, neighbor))
                        continue;
                    steps[neighbor] = step;
                    next.Add(neighbor);
                }
            }
            frontier = next;
        }
        return steps;
    }

    // Every tile covered by some reachable anchor, for highlighting.
    public HashSet<Vector2I> GetReachableTiles(Stack stack) => GetReachability(stack).GetTiles();

    public bool CanMoveTo(Stack stack, Vector2I anchor) => GetReachability(stack).CanReach(anchor);

    #endregion

    #region Melee

    // These take an optional Reachability so a turn can search once and reuse it.

    // Anchors the stack can attack the target from: its current anchor or a reachable one next to the target.
    public List<Vector2I> GetMeleeAttackAnchors(Stack stack, Stack target, Reachability? reachability = null)
    {
        if (!stack.HasMelee || target.IsDead)
            return new();

        reachability ??= GetReachability(stack);
        return reachability.Anchors
            .Prepend(stack.Anchor)
            .Where(a => AreAdjacent(a, stack.Size, target.Anchor, target.Size))
            .ToList();
    }

    public List<Stack> GetMeleeTargets(Stack stack, Reachability? reachability = null)
    {
        if (!stack.HasMelee)
            return new();

        reachability ??= GetReachability(stack);
        return GetEnemies(stack).Where(e => GetMeleeAttackAnchors(stack, e, reachability).Count > 0).ToList();
    }

    public bool CanMelee(Stack stack, Reachability? reachability = null) => GetMeleeTargets(stack, reachability).Count > 0;

    public bool CanMelee(Stack stack, Stack target, Reachability? reachability = null) =>
        GetMeleeAttackAnchors(stack, target, reachability).Count > 0;

    public bool CanRetaliate(Stack stack) => stack.Unit.CanRetaliate && stack.HasMelee && !stack.Retaliated && !stack.IsDead;

    #endregion

    #region Long range

    // Enemies the stack can shoot: all of them, except ones it can reach in melee if it also has melee.
    public List<Stack> GetRangedTargets(Stack stack, Reachability? reachability = null)
    {
        if (!stack.HasLongRange)
            return new();

        List<Stack> meleeTargets = GetMeleeTargets(stack, reachability);
        return GetEnemies(stack).Where(e => !meleeTargets.Contains(e)).ToList();
    }

    public bool CanShoot(Stack stack, Reachability? reachability = null) => GetRangedTargets(stack, reachability).Count > 0;

    public bool CanShoot(Stack stack, Stack target, Reachability? reachability = null) =>
        GetRangedTargets(stack, reachability).Contains(target);

    #endregion

    #region Spells

    public bool CanHeroCast(Stack stack)
    {
        Hero? hero = stack.Hero;
        return hero != null && !hero.HasCastThisRound && hero.GetUsableSpells().Count > 0;
    }

    // Whether the spellbook should be active on this stack's turn.
    public bool CanCastSpell(Stack stack) => stack.GetCastableSpells().Count > 0 || CanHeroCast(stack);

    // Living stacks the spell can be cast on during this stack's turn (a hero spell is cast from the stack's side).
    public List<Stack> GetSpellTargets(Stack stack, SpellAbility spell) => spell.Targets switch
    {
        SpellAbility.TargetSide.Enemies => GetEnemies(stack),
        SpellAbility.TargetSide.Allies => GetAllies(stack),
        _ => GetAllies(stack).Concat(GetEnemies(stack)).ToList(),
    };

    public bool SpendStackMana(Stack stack, SpellAbility spell)
    {
        if (stack.Mana < spell.ManaCost)
            return false;
        stack.Mana -= spell.ManaCost;
        return true;
    }

    public bool SpendHeroMana(Hero hero, SpellAbility spell)
    {
        if (!hero.CanCast(spell))
            return false;
        hero.Mana -= spell.ManaCost;
        hero.HasCastThisRound = true;
        return true;
    }

    #endregion

    #region Damage

    // Damage of an attack: the attacker's Strength per unit, scaled by the ability's multiplier, minus a tenth of
    // the target's Defense per unit. Never negative. Both stats include effects and the hero bonus.
    // Spells don't use it: they ignore Defense.
    public static int CalculateDamage(Stack attacker, Stack target, float multiplier = 1f)
    {
        float attack = multiplier * attacker.GetStrength() * attacker.Quantity;
        float defense = target.GetDefense() * target.Quantity / 10f;
        return Math.Max(0, (int)(attack - defense));
    }

    #endregion

    #region Mutations

    // Puts the stack's whole footprint on the anchor. Only checks that it fits, not that it's reachable.
    public bool TryPlaceStack(Stack stack, Vector2I anchor)
    {
        if (!Fits(stack, anchor))
            return false;
        RemoveFromBoard(stack);
        stack.Anchor = anchor;
        foreach (Vector2I tile in stack.GetOccupiedTiles())
            _occupancy[tile] = stack;
        return true;
    }

    // Removes dead stacks from the armies, the round queue and the board. Returns them.
    public List<Stack> RemoveDeadStacks()
    {
        List<Stack> dead = PlayerArmy.RemoveDeadStacks();
        dead.AddRange(ComputerArmy.RemoveDeadStacks());
        foreach (Stack stack in dead)
        {
            RoundQueue.Remove(stack);
            RemoveFromBoard(stack);
        }
        return dead;
    }

    private void RemoveFromBoard(Stack stack)
    {
        foreach (Vector2I tile in stack.GetOccupiedTiles())
            if (GetStackAt(tile) == stack)
                _occupancy.Remove(tile);
    }

    #endregion

    #region Placement

    private void PlaceAtCurrentAnchors(Army army)
    {
        foreach (Stack stack in army.Stacks)
            if (!TryPlaceStack(stack, stack.Anchor))
                GD.PushError($"{stack.Unit.Name} doesn't fit at its anchor {stack.Anchor}.");
    }

    // Spreads the army's stacks evenly down its edge of the board. A stack that doesn't fit at its
    // spot goes to the nearest free row, then further inward.
    // Used when the battle starts without the placement phase (Arena.AutoBeginBattle).
    private void PlaceArmy(Army army, bool leftSide)
    {
        List<Vector2I> anchors = GetStartingAnchors(army.Stacks, leftSide, Width, Height);
        for (int i = 0; i < army.Stacks.Count; i++)
            if (!TryPlaceStack(army.Stacks[i], anchors[i]))
                GD.PushError($"No room to place {army.Stacks[i].Unit.Name}.");
    }

    // Default starting anchors for one side, without touching any board. anchors[i] belongs to stacks[i].
    // Stacks are spread evenly along the edge column; a stack that doesn't fit at its preferred row tries
    // the other rows of that column (0, +1, -1, +2, -2...), then moves one column inward.
    public static List<Vector2I> GetStartingAnchors(IReadOnlyList<Stack> stacks, bool leftSide, int width, int height)
    {
        var anchors = new List<Vector2I>(stacks.Count);
        var taken = new HashSet<Vector2I>();
        int count = stacks.Count;
        for (int i = 0; i < count; i++)
        {
            Stack stack = stacks[i];
            int edge = leftSide ? 0 : width - stack.Size;
            int row = Math.Clamp((2 * i + 1) * height / (2 * count) - stack.Size / 2, 0, height - stack.Size);

            bool placed = false;
            for (int inward = 0; !placed && inward < width; inward++)
            {
                int column = leftSide ? edge + inward : edge - inward;
                for (int offset = 0; !placed && offset < 2 * height; offset++)
                {
                    int y = row + (offset % 2 == 0 ? offset / 2 : -(offset / 2 + 1));
                    var anchor = new Vector2I(column, y);
                    List<Vector2I> tiles = stack.GetTilesAt(anchor);
                    if (tiles.All(t => t.X >= 0 && t.Y >= 0 && t.X < width && t.Y < height && !taken.Contains(t)))
                    {
                        anchors.Add(anchor);
                        taken.UnionWith(tiles);
                        placed = true;
                    }
                }
            }
            // Can't happen within the 5-stack limit on a normal board, and a gap would shift every later index.
            if (!placed)
                throw new InvalidOperationException($"No room to place {stack.Unit.Name}.");
        }
        return anchors;
    }

    #endregion
}
