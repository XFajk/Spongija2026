using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

public partial class Arena : Node3D
{
    public const string SmallTileScene = "uid://c16eba65w4ptq";
    public const string DoubleTileScene = "uid://fahq2oemfim8";
    public const string ArenaUserInterfaceScene = "uid://cd3eg1hleix2";

    [Export]
    public Node3D? SetupBoardCameraPosition { get; set; } = null;

    [Export]
    public Node3D? BoardOverviewCameraPosition { get; set; } = null;

    [Export]
    public ArenaCamera? PlayerCamera { get; set; } = null;

    public const uint AreaWidth = 20;

    public const uint AreaHeight = 12;

    // Side of one tile in meters.
    public const float TileSize = 0.5f;

    // Columns on the player's side where stacks can be arranged during the placement phase.
    public const int PlacementColumns = 2;

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
        var smallTile = GD.Load<PackedScene>(SmallTileScene);

        Node3D smallTileBoard = new Node3D()
        {
            Name = "SmallTileBoard"
        };

        AddChild(smallTileBoard);

        // Instantiate small tiles and arrange them in a grid within the smallTileBoard
        for (int x = 0; x < AreaWidth; x++)
        {
            Node3D row = new Node3D()
            {
                Name = $"Row{x}"
            };
            smallTileBoard.AddChild(row);
            for (int y = 0; y < AreaHeight; y++)
            {
                var tileInstance = smallTile.Instantiate() as SmallTile;
                Debug.Assert(tileInstance != null);
                tileInstance.GridPosition = new Vector2I(x, y); 
                tileInstance.Position = GridToWorld(new Vector2I(x, y));
                tileInstance.RotationDegrees = new Vector3(-90, 0, 0);
                tileInstance.Enabled = false;
                row.AddChild(tileInstance);
                _smallTiles[x * (int)AreaHeight + y] = tileInstance;
            }
        }

        if (SetupBoardCameraPosition == null)
        {
            GD.PrintErr("SetupBoardCameraPosition is not assigned.");
            return;
        }

        if (BoardOverviewCameraPosition == null)
        {
            GD.PrintErr("BoardOverviewCameraPosition is not assigned.");
            return;
        }

        if (PlayerCamera == null)
        {
            GD.PrintErr("PlayerCamera is not assigned.");
            return;
        }

        if (!AutoBeginBattle)
        {
            SetupPlacementPhase();
        }

    }


    // Battle setup
    public void SetArmies(Army playerArmy, Army computerArmy)
    {
        PlayerArmy = playerArmy;
        ComputerArmy = computerArmy;
    }

    // Setup the Scene to let the player place their units before the battle begins.
    public void SetupPlacementPhase()
    {
        if (PlayerCamera == null || SetupBoardCameraPosition == null)
        {
            GD.PrintErr("Cannot setup placement phase: PlayerCamera or SetupBoardCameraPosition is not assigned.");
            return;
        }
        var arenaUI = GD.Load<PackedScene>(ArenaUserInterfaceScene).Instantiate() as ArenaUserInterface;
        if (arenaUI == null)
        {
            GD.PrintErr("Failed to instantiate ArenaUserInterface.");
            return;
        }
        arenaUI.arena = this;
        arenaUI.arenaCamera = PlayerCamera;
        AddChild(arenaUI);

        PlayerCamera.arena = this;
        PlayerCamera.GlobalPosition = SetupBoardCameraPosition.GlobalPosition;
        PlayerCamera.Rotation = SetupBoardCameraPosition.Rotation;
        ShowPlayerFirst2Columns();

        var stackGridInstantiationAnchors = Battle.GetStartingAnchors(PlayerArmy!.Stacks, true, (int)AreaWidth, (int)AreaHeight);
        foreach (var (stack, anchor) in PlayerArmy!.Stacks.Zip(stackGridInstantiationAnchors, (s, a) => (s, a)))
        {
            TryPlaceStack(stack, anchor);
        }
    }

    // Whether the stack can go with its anchor on the tile during the placement phase: its whole footprint
    // inside the placement columns and not on another player stack. Battle doesn't exist yet, so this can't use Battle.Fits.
    public bool CanPlaceDuringSetup(Stack stack, Vector2I anchor)
    {
        foreach (Vector2I tile in stack.GetTilesAt(anchor))
        {
            if (tile.X < 0 || tile.X >= PlacementColumns || tile.Y < 0 || tile.Y >= AreaHeight)
                return false;
            if (PlayerArmy!.Stacks.Any(other => other != stack && other.GetOccupiedTiles().Contains(tile)))
                return false;
        }
        return true;
    }

    // For dragging during the placement phase: of the anchors whose footprint covers the tile and that
    // CanPlaceDuringSetup, the one closest to the stack's current anchor, so the stack moves as little as it
    // needs to cover the tile. For a 1x1 stack that's the tile itself. Null if none fits.
    public Vector2I? FindSetupAnchorCovering(Stack stack, Vector2I tile)
    {
        Vector2I? best = null;
        int bestDistance = int.MaxValue;
        for (int dx = 0; dx < stack.Size; dx++)
        {
            for (int dy = 0; dy < stack.Size; dy++)
            {
                Vector2I anchor = tile - new Vector2I(dx, dy);
                if (!CanPlaceDuringSetup(stack, anchor))
                    continue;
                Vector2I offset = (anchor - stack.Anchor).Abs();
                int distance = offset.X + offset.Y;
                if (distance < bestDistance)
                {
                    best = anchor;
                    bestDistance = distance;
                }
            }
        }
        return best;
    }

    private void ShowPlayerFirst2Columns()
    {
        for (int x = 0; x < PlacementColumns; x++)
        {
            for (int y = 0; y < AreaHeight; y++)
            {
                GetSmallTile(new Vector2I(x, y)).Enabled = true;
            }
        }
    }

    private void TryPlaceStack(Stack stack, Vector2I anchor)
    {
        Node? stackInstance = stack?.Unit?.Model?.Instantiate();
        Unit? type = stack?.Unit;

        if (type == null)
            throw new ArgumentNullException(nameof(stack), "Stack's unit type is null.");

        if (stackInstance is UnitNode unit)
        {
            AddChild(unit);
            // GridToWorld is Arena-local, so set the local position.
            unit.CurrentStack = stack;
            unit.ArenaRef = this;
            stack!.Anchor = anchor;
            unit.Position = GridToWorld(stack!, anchor);
            unit.RotationDegrees = new Vector3(0, -90, 0);

            // A 2x2 stack stands on one double tile instead of the 4 small tiles under it.
            // It's a child of the unit, so it stays under it when the unit moves.
            if (stack.Size >= 2)
            {
                HideTiles(stack.GetOccupiedTiles());
                var doubleTile = GD.Load<PackedScene>(DoubleTileScene).Instantiate<DoubleTile>();
                doubleTile.GridPosition = anchor;
                doubleTile.RotationDegrees = new Vector3(-90, 0, 0);
                unit.AddChild(doubleTile);
                unit.GroundTile = doubleTile;
            }
        }
        else
        {
            GD.PrintErr("Failed to instantiate stack's unit model.");
            if (stackInstance != null)
                stackInstance.QueueFree();
        }
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

    // Position relative to the Arena of a point on the grid, in tile units: (0, 0) is the outer corner
    // of tile (0, 0) and (0.5, 0.5) its center. The grid is centered on the Arena's origin.
    // For a global position use arena.ToGlobal(GridToWorld(...)).
    public static Vector3 GridToWorld(Vector2 gridPoint)
    {
        return new Vector3((gridPoint.X - AreaWidth / 2f) * TileSize, 0, (gridPoint.Y - AreaHeight / 2f) * TileSize);
    }

    // Center of a tile.
    public static Vector3 GridToWorld(Vector2I tile) => GridToWorld((Vector2)tile + new Vector2(0.5f, 0.5f));

    // Center of a stack's footprint with its anchor on the given tile; works for 1x1 and 2x2 stacks.
    public static Vector3 GridToWorld(Stack stack, Vector2I anchor) =>
        GridToWorld((Vector2)anchor + new Vector2(stack.Size, stack.Size) / 2f);

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
            GetSmallTile(tile).Enabled = true;
    }

    public void HideTiles(IEnumerable<Vector2I> tiles)
    {
        foreach (Vector2I tile in tiles)
            GetSmallTile(tile).Enabled = false;
    }

    public void HideAllTiles()
    {
        foreach (var tile in _smallTiles)
            tile.Enabled = false;
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
