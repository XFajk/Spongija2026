using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

public partial class Arena : Node3D
{
	[Export]
    public PackedScene? SmallTile;

    [Export]
    public PackedScene? DoubleTile;

    public static uint AreaWidth = 20;

    public static uint AreaHeight = 12;

    [Export]
    public int TileWidthOffset;

    [Export]
    public int TileHeightOffset;

    // Starts the battle as soon as the arena is ready. TODO: turn off once the placement phase exists.
    [Export]
    public bool AutoBeginBattle = true;

    // Armies stored before the battle exists, so the placement phase can arrange them.
    // Set by SceneTransitions.EnterArena.
    public Army? PlayerArmy { get; private set; }
    public Army? ComputerArmy { get; private set; }

    public Battle? Battle { get; private set; }

    // The stack whose turn it is, for the UI.
    public Stack? CurrentStack { get; private set; }

    // Where CurrentStack can move this turn. Searched once at the start of the turn and reused for
    // highlighting, hover (GetAnchorsCovering), validation and the movement path.
    public Reachability? CurrentReachability { get; private set; }

    public event Action<Army?>? BattleEnded;

    // Tiles by battle grid coordinate (0..AreaWidth-1, 0..AreaHeight-1).
    private readonly SmallTile[] _smallTiles = new SmallTile[AreaWidth * AreaHeight];

    private TaskCompletionSource<TurnOutput>? _playerTurn;

    // An ability waiting to run in ExecuteTurn. Unlike TurnEntry it knows its user,
    // because retaliations are used by the target, not by the acting stack.
    private readonly record struct PendingAbility(Stack User, IAbility Ability, Stack Target, bool IsRetaliation);

    public override void _Ready()
    {
        if (SmallTile == null || DoubleTile == null)
        {
            GD.PrintErr("SmallTile or DoubleTile is not assigned.");
            return;
        }

        Node3D smallTileBoard = new Node3D()
        {
            Name = "SmallTileBoard"
        };

        AddChild(smallTileBoard);

        // Instantiate small tiles and arrange them in a grid within the smallTileBoard
        for (int x = TileWidthOffset; x < AreaWidth+TileWidthOffset; x++)
        {
            Node3D row = new Node3D()
            {
                Name = $"Row{x}"
            };
            smallTileBoard.AddChild(row);
            for (int y = TileHeightOffset; y < AreaHeight+TileHeightOffset; y++)
            {
                var tileInstance = SmallTile.Instantiate() as SmallTile;
                Debug.Assert(tileInstance != null);

                tileInstance.Position = new Vector3(x/2.0f+0.25f, 0, y/2.0f+0.25f);
                tileInstance.RotationDegrees = new Vector3(-90, 0, 0);
                tileInstance.Hide();
                row.AddChild(tileInstance);
                _smallTiles[(x-TileWidthOffset) * (int)AreaHeight + (y-TileHeightOffset)] = tileInstance;
            }
        }

        BeginBattle();
    }

    // Battle setup
    public void SetArmies(Army playerArmy, Army computerArmy)
    {
        PlayerArmy = playerArmy;
        ComputerArmy = computerArmy;
    }

    // Starts the battle with the stored armies. TODO: called by the placement phase's start button.
    public void BeginBattle()
    {
        if (PlayerArmy == null || ComputerArmy == null)
        {
            GD.PushError("BeginBattle called before SetArmies.");
            return;
        }
        StartBattle(PlayerArmy, ComputerArmy);
    }

    // Tiles

    public SmallTile GetSmallTile(Vector2I pos)
    {
        if (pos.X < 0 || pos.Y < 0 || pos.X >= AreaWidth || pos.Y >= AreaHeight)
            throw new ArgumentOutOfRangeException();
        // Search for the tile with in a flattened 2D array 
        return _smallTiles[pos.X * (int)AreaHeight + pos.Y];
    }

    public void ShowTiles(IEnumerable<Vector2I> tiles)
    {
        foreach (Vector2I tile in tiles) 
            GetSmallTile(tile).Show();
    }

    public void HideAllTiles()
    {
        foreach (var tile in _smallTiles)
            tile.Hide();
    }

    // Battle loop

    public async void StartBattle(Army playerArmy, Army computerArmy)
    {
        Battle = new Battle(playerArmy, computerArmy, (int)AreaWidth, (int)AreaHeight);
        try
        {
            await RunBattle(Battle);
        }
        catch (Exception e)
        {
            GD.PushError($"Battle failed: {e}");
        }
    }

    private async Task RunBattle(Battle battle)
    {
        while (!battle.IsOver)
        {
            // Wait a frame per turn, so turns that finish instantly (like a skipped AI turn) can't freeze the game.
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            Stack? stack = battle.GetTurnInput();
            if (stack == null)
            {
                await EndRound(battle);
                continue;
            }

            CurrentStack = stack;
            Reachability reachability = battle.GetReachability(stack);
            CurrentReachability = reachability;
            TurnOutput output = battle.IsPlayerStack(stack)
                ? await GetPlayerTurn(battle, stack, reachability)
                : GetComputerTurn(battle, stack, reachability);
            HideAllTiles();

            await ExecuteTurn(battle, stack, output, reachability);
            CurrentReachability = null;
            await EndTurn(stack);
            battle.RemoveDeadStacks();
        }

        CurrentStack = null;
        battle.EndBattle();
        BattleEnded?.Invoke(battle.Winner);
    }

    private Task<TurnOutput> GetPlayerTurn(Battle battle, Stack stack, Reachability reachability)
    {
        ShowTiles(reachability.GetTiles());
        // TODO: sword cursor over battle.GetMeleeTargets(stack, reachability), arrow cursor over
        // battle.GetRangedTargets(stack, reachability), spellbook button enabled by battle.CanCastSpell(stack).
        // Size 2 hover: reachability.GetAnchorsCovering(hoveredTile) gives the valid 2x2 squares under the mouse.
        // Clicks build a TurnOutput (MoveTo is an anchor) and call SubmitPlayerTurn.
        _playerTurn = new TaskCompletionSource<TurnOutput>();
        return _playerTurn.Task;
    }

    // Called by the input handling when the player has decided their turn.
    public void SubmitPlayerTurn(TurnOutput output)
    {
        TaskCompletionSource<TurnOutput>? turn = _playerTurn;
        _playerTurn = null;
        turn?.TrySetResult(output);
    }

    private TurnOutput GetComputerTurn(Battle battle, Stack stack, Reachability reachability)
    {
        // TODO: AI. Skips the turn for now.
        return new TurnOutput();
    }

    private async Task ExecuteTurn(Battle battle, Stack user, TurnOutput output, Reachability reachability)
    {
        if (output.MoveTo is Vector2I moveTo)
        {
            if (reachability.CanReach(moveTo) && battle.TryPlaceStack(user, moveTo))
                await AnimateMove(user, reachability.GetPath(moveTo));
            else
                GD.PushWarning($"{user.Unit.Name} can't move to {moveTo}.");
        }

        if (output.HeroSpell is var (spell, spellTarget))
        {
            Hero? hero = user.Hero;
            if (hero != null && battle.SpendHeroMana(hero, spell))
                await spell.Use(user, spellTarget, this);
        }

        var waves = new SortedDictionary<int, List<PendingAbility>>();
        foreach (TurnEntry entry in output.Entries)
            AddToWave(waves, entry.Wave, new PendingAbility(user, entry.Ability, entry.Target, false));

        // Retaliations add entries to later waves while this runs, so look up the next wave each time.
        int wave = 0;
        while (waves.Keys.Any(w => w >= wave))
        {
            wave = waves.Keys.First(w => w >= wave);
            List<Task> tasks = waves[wave].Select(p => ExecuteAbility(battle, p, wave, waves)).ToList();
            await Task.WhenAll(tasks);
            wave++;
        }
    }

    // Walks the stack's model through the anchors in order. The board state has already changed.
    private Task AnimateMove(Stack stack, List<Vector2I> path)
    {
        // TODO: move the stack's model tile by tile (use Stack.GetTilesAt / GetCenter for each anchor in path).
        return Task.CompletedTask;
    }

    private async Task ExecuteAbility(Battle battle, PendingAbility pending, int wave, SortedDictionary<int, List<PendingAbility>> waves)
    {
        if (pending.User.IsDead || pending.Target.IsDead)
            return;

        if (pending.Ability is SpellAbility spell && !battle.SpendStackMana(pending.User, spell))
            return;

        await pending.Ability.Use(pending.User, pending.Target, this);

        if (pending.Ability is MeleeAttackAbility && !pending.IsRetaliation && battle.CanRetaliate(pending.Target))
        {
            pending.Target.Retaliated = true;
            AddToWave(waves, wave + 1, new PendingAbility(pending.Target, pending.Target.Unit.MeleeAttack!, pending.User, true));
        }
    }

    private static void AddToWave(SortedDictionary<int, List<PendingAbility>> waves, int wave, PendingAbility pending)
    {
        if (!waves.TryGetValue(wave, out List<PendingAbility>? list))
        {
            list = new List<PendingAbility>();
            waves[wave] = list;
        }
        list.Add(pending);
    }

    private async Task EndTurn(Stack stack)
    {
        if (stack.IsDead)
            return;

        foreach (TurnPassiveAbility passive in stack.GetTurnPassives())
            await passive.Use(stack, stack, this);
        foreach (TurnEffect effect in stack.GetTurnEffects())
            await ApplyEffect(effect, stack);

        stack.RemoveExpiredEffects();
    }

    private async Task EndRound(Battle battle)
    {
        foreach (Stack stack in battle.AllStacks.ToList())
        {
            foreach (RoundPassiveAbility passive in stack.GetRoundPassives())
                await passive.Use(stack, stack, this);
            foreach (RoundEffect effect in stack.GetRoundEffects())
                await ApplyEffect(effect, stack);
            stack.RemoveExpiredEffects();
        }

        battle.RemoveDeadStacks();
        if (!battle.IsOver)
            battle.StartNewRound();
    }

    private async Task ApplyEffect(IEffect effect, Stack target)
    {
        if (target.IsDead)
            return;
        await effect.Apply(target, this);
        effect.RemainingDuration--;
    }
}
