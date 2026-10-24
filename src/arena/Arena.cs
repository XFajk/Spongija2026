using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

public partial class Arena : Node3D
{
    #region Fields

    [Signal]
    public delegate void SetupPlacementPhaseCompletedEventHandler();

    public const string SmallTileScene = "uid://c16eba65w4ptq";
    public const string DoubleTileScene = "uid://fahq2oemfim8";
    public const string ArenaUserInterfaceScene = "uid://cd3eg1hleix2";

    [Export]
    public Node3D? SetupBoardCameraPosition { get; set; } = null;

    [Export]
    public Node3D? BoardOverviewCameraPosition { get; set; } = null;

    [Export]
    public ArenaCamera? PlayerCamera { get; set; } = null;

    // Seconds the camera takes to move from the setup view to the battle view.
    [Export]
    public float CameraTransitionDuration { get; set; } = 1.5f;

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

    public bool IsPlayerTurn => CurrentStack?.Army == PlayerArmy && _playerTurn != null;

    public bool IsInSetupPhase => Battle == null;

    // Enemies the current stack can melee this player turn. The tiles under them are red, and hovering one
    // (its unit or its tile) picks the position to attack it from.
    private readonly List<Stack> _meleeTargets = [];

    // The attack position picked by hovering a melee target this turn: red and raised.
    private (Stack Target, Vector2I Anchor)? _pickedMeleeAttack;

    // On a 2x2 stack's turn: a second double tile showing where the stack would move (or attack from).
    // It follows the hovered tile like the setup drag, moving as little as needed from where it is.
    // Arena's own, not a unit's; it stands in for the small tiles under it like a stack's double tile.
    public DoubleTile? MovePreview { get; private set; }

    // The preview's anchor; null while it's on the stack's own position, where it's hidden and the stack's
    // own double tile acts as the preview (two quads in one spot would flicker).
    private Vector2I? _movePreviewAnchor;

    // Lifts the preview just above the stack's own double tile where they overlap.
    private const float MovePreviewLift = 0.005f;

    // An ability waiting to run in ExecuteTurn. Unlike TurnEntry it knows its user,
    // because retaliations are used by the target, not by the acting stack.
    private readonly record struct PendingAbility(Stack User, IAbility Ability, Stack Target, bool IsRetaliation);

    #endregion

    #region Lifecycle

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
                tileInstance.ArenaRef = this;
                tileInstance.GridPosition = new Vector2I(x, y); 
                tileInstance.Position = GridToWorld(new Vector2I(x, y));
                tileInstance.RotationDegrees = new Vector3(-90, 0, 0);
                tileInstance.Enabled = false;
                row.AddChild(tileInstance);
                _smallTiles[x * (int)AreaHeight + y] = tileInstance;
            }
        }

        MovePreview = GD.Load<PackedScene>(DoubleTileScene).Instantiate<DoubleTile>();
        MovePreview.Name = "MovePreview";
        MovePreview.ArenaRef = this;
        MovePreview.RotationDegrees = new Vector3(-90, 0, 0);
        MovePreview.Enabled = false;
        AddChild(MovePreview);

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

    #endregion

    #region Placement phase and transition

    public void SetArmies(Army playerArmy, Army computerArmy)
    {
        PlayerArmy = playerArmy;
        ComputerArmy = computerArmy;
    }

    // Setup the Scene to let the player place their units before the battle begins.
    public async void SetupPlacementPhase()
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
            SpawnUnitNode(stack, anchor, playerSide: true);
        }

        await ToSignal(this, SignalName.SetupPlacementPhaseCompleted);

        await TransitionToBattle();

        BeginBattle();
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
        var candidates = new List<Vector2I>();
        for (int dx = 0; dx < stack.Size; dx++)
            for (int dy = 0; dy < stack.Size; dy++)
                candidates.Add(tile - new Vector2I(dx, dy));
        return ClosestAnchor(candidates.Where(a => CanPlaceDuringSetup(stack, a)), stack.Anchor);
    }

    // The candidate with the smallest step distance (Manhattan) from the given anchor; null if there are none.
    private static Vector2I? ClosestAnchor(IEnumerable<Vector2I> candidates, Vector2I from)
    {
        Vector2I? best = null;
        int bestDistance = int.MaxValue;
        foreach (Vector2I anchor in candidates)
        {
            Vector2I offset = (anchor - from).Abs();
            int distance = offset.X + offset.Y;
            if (distance < bestDistance)
            {
                best = anchor;
                bestDistance = distance;
            }
        }
        return best;
    }

    private void ShowPlayerFirst2Columns()
    {
        var columns = new List<Vector2I>();
        for (int x = 0; x < PlacementColumns; x++)
            for (int y = 0; y < AreaHeight; y++)
                columns.Add(new Vector2I(x, y));
        ShowTiles(columns);
    }

    // Spawns the stack's model at the anchor, facing the other side, and links it as Stack.NodeRepresentation.
    private UnitNode? SpawnUnitNode(Stack stack, Vector2I anchor, bool playerSide)
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
            unit.RotationDegrees = new Vector3(0, playerSide ? -90 : 90, 0);
            stack.NodeRepresentation = unit;
            unit.IsPlayerUnit = playerSide;

            // A 2x2 stack stands on one double tile instead of the 4 small tiles under it.
            // It's a child of the unit, so it stays under it when the unit moves.
            if (stack.Size >= 2)
            {
                // Takes over whether the tiles it covers were shown, e.g. shown in the placement columns
                bool shown = GetSmallTile(anchor).Enabled;
                var doubleTile = GD.Load<PackedScene>(DoubleTileScene).Instantiate<DoubleTile>();
                doubleTile.ArenaRef = this;
                doubleTile.GridPosition = anchor;
                doubleTile.RotationDegrees = new Vector3(-90, 0, 0);
                unit.AddChild(doubleTile);
                unit.GroundTile = doubleTile;
                SetTilesShown(stack.GetOccupiedTiles(), shown);
            }
            return unit;
        }

        GD.PrintErr("Failed to instantiate stack's unit model.");
        if (stackInstance != null)
            stackInstance.QueueFree();
        return null;
    }

    // From the placement phase to the battle: spawns the computer's units, switches every unit to its battle
    // interactions, hides the placement tiles and moves the camera to the battle view.
    public async Task TransitionToBattle()
    {
        var computerAnchors = Battle.GetStartingAnchors(ComputerArmy!.Stacks, false, (int)AreaWidth, (int)AreaHeight);
        foreach (var (stack, anchor) in ComputerArmy.Stacks.Zip(computerAnchors, (s, a) => (s, a)))
            SpawnUnitNode(stack, anchor, playerSide: false);

        foreach (Stack stack in PlayerArmy!.Stacks.Concat(ComputerArmy.Stacks))
            stack.NodeRepresentation?.SwitchToBattleInteractions();

        // Tiles are shown per turn from now on. Double tiles stay under their units.
        HideAllTiles();

        await PlayerCamera!.MoveTo(BoardOverviewCameraPosition!, CameraTransitionDuration);
    }

    // Starts the battle with the stored armies. Called after the placement phase.
    public void BeginBattle()
    {
        if (PlayerArmy == null || ComputerArmy == null)
        {
            GD.PushError("BeginBattle called before SetArmies.");
            return;
        }
        StartBattle(PlayerArmy, ComputerArmy);
    }

    #endregion

    #region Tiles

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

    public void ShowTiles(IEnumerable<Vector2I> tiles) => SetTilesShown(tiles, true);

    public void HideTiles(IEnumerable<Vector2I> tiles) => SetTilesShown(tiles, false);

    public void HideAllTiles()
    {
        foreach (var tile in _smallTiles)
        {
            tile.Enabled = false;
            tile.IsMeleeAttackTile = false;
            tile.Raised = false;
        }
        foreach (DoubleTile doubleTile in GetDoubleTilesByTile().Values)
        {
            doubleTile.Enabled = false;
            doubleTile.IsMeleeAttackTile = false;
            doubleTile.Raised = false;
        }
    }

    // Makes the tiles red and/or raised. Like showing, a tile under a 2x2 stack marks its double tile.
    // Hiding a tile clears both.
    public void SetTilesHighlight(IEnumerable<Vector2I> tiles, bool red, bool raised)
    {
        Dictionary<Vector2I, DoubleTile> doubleTiles = GetDoubleTilesByTile();
        foreach (Vector2I tile in tiles)
        {
            if (doubleTiles.TryGetValue(tile, out DoubleTile? doubleTile))
            {
                doubleTile.IsMeleeAttackTile = red;
                doubleTile.Raised = raised;
            }
            else
            {
                SmallTile smallTile = GetSmallTile(tile);
                smallTile.IsMeleeAttackTile = red;
                smallTile.Raised = raised;
            }
        }
    }

    // A tile under a 2x2 stack is stood in for by the stack's double tile: its small tile stays disabled,
    // so it never shows through, and the double tile is shown or hidden in its place.
    private void SetTilesShown(IEnumerable<Vector2I> tiles, bool shown)
    {
        Dictionary<Vector2I, DoubleTile> doubleTiles = GetDoubleTilesByTile();
        foreach (Vector2I tile in tiles)
        {
            SmallTile smallTile = GetSmallTile(tile);
            if (doubleTiles.TryGetValue(tile, out DoubleTile? doubleTile))
            {
                smallTile.Enabled = false;
                doubleTile.Enabled = shown;
                if (!shown)
                {
                    doubleTile.IsMeleeAttackTile = false;
                    doubleTile.Raised = false;
                }
            }
            else
            {
                smallTile.Enabled = shown;
            }
            if (!shown)
            {
                smallTile.IsMeleeAttackTile = false;
                smallTile.Raised = false;
            }
        }
    }

    // The double tile covering each tile, for every spawned 2x2 stack, by the stack's current anchor,
    // and the move preview while it's shown. The preview comes last, so it wins where it overlaps the
    // current stack's own double tile.
    private Dictionary<Vector2I, DoubleTile> GetDoubleTilesByTile()
    {
        var doubleTiles = new Dictionary<Vector2I, DoubleTile>();
        IEnumerable<Stack> stacks = (PlayerArmy?.Stacks ?? []).Concat(ComputerArmy?.Stacks ?? []);
        foreach (Stack stack in stacks)
            if (stack.NodeRepresentation?.GroundTile is DoubleTile doubleTile)
                foreach (Vector2I tile in stack.GetOccupiedTiles())
                    doubleTiles[tile] = doubleTile;
        if (MovePreview != null && CurrentStack != null && _movePreviewAnchor is Vector2I previewAnchor)
            foreach (Vector2I tile in CurrentStack.GetTilesAt(previewAnchor))
                doubleTiles[tile] = MovePreview;
        return doubleTiles;
    }

    #endregion

    #region Battle loop

    public async void StartBattle(Army playerArmy, Army computerArmy)
    {
        // After the placement phase the stacks already have their anchors, so the Battle keeps them
        Battle = new Battle(playerArmy, computerArmy, (int)AreaWidth, (int)AreaHeight, keepAnchors: !AutoBeginBattle);
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
            ClearTurnInput();

            await ExecuteTurn(battle, stack, output, reachability);
            CurrentReachability = null;
            await EndTurn(stack);
            RemoveDeadStacks(battle);
        }

        CurrentStack = null;
        battle.EndBattle();
        BattleEnded?.Invoke(battle.Winner);
    }

    #endregion

    #region Turn input

    private Task<TurnOutput> GetPlayerTurn(Battle battle, Stack stack, Reachability reachability)
    {
        ShowTiles(reachability.GetTiles());

        // The tiles under every enemy the stack can melee turn red; hovering one picks where to attack it from
        _meleeTargets.AddRange(battle.GetMeleeTargets(stack, reachability));
        foreach (Stack target in _meleeTargets)
        {
            ShowTiles(target.GetOccupiedTiles());
            SetTilesHighlight(target.GetOccupiedTiles(), red: true, raised: false);
        }

        // TODO: sword cursor over battle.GetMeleeTargets(stack, reachability), arrow cursor over
        // battle.GetRangedTargets(stack, reachability), spellbook button enabled by battle.CanCastSpell(stack).
        // Size 2 hover: reachability.GetAnchorsCovering(hoveredTile) gives the valid 2x2 squares under the mouse.
        // Clicks build a TurnOutput (MoveTo is an anchor) and call SubmitPlayerTurn.
        _playerTurn = new TaskCompletionSource<TurnOutput>();
        return _playerTurn.Task;
    }

    // Hides the tiles and forgets the melee targets, the picked attack and the move preview.
    private void ClearTurnInput()
    {
        // While the preview anchor is still set, so HideAllTiles also resets the preview
        HideAllTiles();
        _meleeTargets.Clear();
        _pickedMeleeAttack = null;
        _movePreviewAnchor = null;
        if (MovePreview != null)
            MovePreview.Enabled = false;
    }

    // Whether the stack whose turn it is can melee the target this turn: from where it stands, or after moving
    // to a reachable anchor next to it. False outside a turn. Doesn't check sides.
    public bool CanCurrentStackMelee(Stack target) =>
        Battle != null && CurrentStack != null && CurrentReachability != null
        && Battle.CanMelee(CurrentStack, target, CurrentReachability);

    public override void _Process(double delta)
    {
        if (IsPlayerTurn)
            UpdateTurnHover();
    }

    // Every frame on the player's turn: picks the attack position for the hovered melee target (red and raised),
    // and for a 2x2 stack moves the move preview to it, or else to the hovered tile.
    private void UpdateTurnHover()
    {
        Stack stack = CurrentStack!;
        Stack? target = GetFocusedMeleeTarget();
        Vector2I? attackAnchor = null;
        if (target != null && PlayerCamera?.GetBoardHoverPoint() is Vector3 hoverPoint)
            attackAnchor = PickMeleeAttackAnchor(target, ToLocal(hoverPoint));
        (Stack Target, Vector2I Anchor)? picked = attackAnchor is Vector2I a ? (target!, a) : null;

        Vector2I previewAnchor = _movePreviewAnchor ?? stack.Anchor;
        Vector2I newPreviewAnchor = stack.Size >= 2 ? attackAnchor ?? GetHoveredMovePreviewAnchor(previewAnchor) : previewAnchor;

        if (picked == _pickedMeleeAttack && newPreviewAnchor == previewAnchor)
            return;

        // Unmarked before the preview moves, since a 2x2 pick is shown on the preview
        if (_pickedMeleeAttack is { } previous)
            SetTilesHighlight(stack.GetTilesAt(previous.Anchor), red: false, raised: false);
        if (newPreviewAnchor != previewAnchor)
            MoveMovePreview(newPreviewAnchor);
        _pickedMeleeAttack = picked;
        if (picked is { } current)
            SetTilesHighlight(stack.GetTilesAt(current.Anchor), red: true, raised: true);
    }

    #region Move preview (2x2 stacks)

    // Where the move preview goes for what's hovered: a reachable small tile pulls it as little as needed to
    // cover it (like the setup drag), the stack's own double tile resets it, anything else keeps it in place.
    private Vector2I GetHoveredMovePreviewAnchor(Vector2I previewAnchor)
    {
        Stack stack = CurrentStack!;
        return PlayerCamera?.CurrentFocusedNode switch
        {
            SmallTile tile when CurrentReachability != null =>
                ClosestAnchor(CurrentReachability.GetAnchorsCovering(tile.GridPosition), previewAnchor) ?? previewAnchor,
            DoubleTile tile when tile == stack.NodeRepresentation?.GroundTile => stack.Anchor,
            _ => previewAnchor,
        };
    }

    // Moves the move preview to the anchor, or hides it on the stack's own position. The small tiles it leaves
    // get shown again and the new ones hand over to it, as with a stack's double tile.
    private void MoveMovePreview(Vector2I anchor)
    {
        Stack stack = CurrentStack!;
        List<Vector2I> oldTiles = _movePreviewAnchor is Vector2I old ? stack.GetTilesAt(old) : [];
        if (_movePreviewAnchor != null)
        {
            MovePreview!.IsMeleeAttackTile = false;
            MovePreview.Raised = false;
        }

        _movePreviewAnchor = anchor == stack.Anchor ? null : anchor;
        if (_movePreviewAnchor is Vector2I newAnchor)
        {
            MovePreview!.GridPosition = newAnchor;
            MovePreview.Position = GridToWorld(stack, newAnchor) + Vector3.Up * MovePreviewLift;
        }
        else
        {
            MovePreview!.Enabled = false;
        }

        // Shown by the current anchors: tiles under the preview enable it, the others their small tile
        // or the stack's own double tile
        ShowTiles(oldTiles.Concat(stack.GetTilesAt(anchor)));
    }

    // Moves the current stack: to the clicked tile for a 1x1 stack, to the move preview for a 2x2 stack
    // (the clicked tile may be any of the 4 it covers). False if it isn't the player's turn or it can't go there.
    public bool TrySubmitMoveTo(Vector2I tile)
    {
        if (!IsPlayerTurn || CurrentStack == null || CurrentReachability == null)
            return false;

        Vector2I anchor = CurrentStack.Size >= 2
            ? _movePreviewAnchor ?? ClosestAnchor(CurrentReachability.GetAnchorsCovering(tile), CurrentStack.Anchor) ?? CurrentStack.Anchor
            : tile;
        if (anchor == CurrentStack.Anchor || !CurrentReachability.CanReach(anchor))
            return false;

        SubmitPlayerTurn(new TurnOutput { MoveTo = anchor });
        return true;
    }

    #endregion

    #region Melee

    // The melee target under the mouse: its unit, or the tile it stands on. Null if the hovered thing isn't one.
    private Stack? GetFocusedMeleeTarget()
    {
        Stack? stack = PlayerCamera?.CurrentFocusedNode switch
        {
            UnitNode unit => unit.CurrentStack,
            SmallTile tile => Battle?.GetStackAt(tile.GridPosition),
            DoubleTile tile => Battle?.GetStackAt(tile.GridPosition),
            _ => null,
        };
        return stack != null && _meleeTargets.Contains(stack) ? stack : null;
    }

    // Of the positions the current stack can melee the target from, the one on the side of the target that the
    // hover point (Arena-local, on the board plane) is on: the smallest angle around the target's footprint center.
    public Vector2I? PickMeleeAttackAnchor(Stack target, Vector3 hoverPoint)
    {
        if (Battle == null || CurrentStack == null || CurrentReachability == null)
            return null;
        List<Vector2I> anchors = Battle.GetMeleeAttackAnchors(CurrentStack, target, CurrentReachability);
        if (anchors.Count == 0)
            return null;

        Vector3 center = GridToWorld(target, target.Anchor);
        Vector3 toMouse = hoverPoint - center;
        toMouse.Y = 0;
        // Right on the center there's no direction; keep the previous pick
        if (toMouse.LengthSquared() < 1e-6f)
            return _pickedMeleeAttack is { } p && p.Target == target ? p.Anchor : anchors[0];
        toMouse = toMouse.Normalized();

        return anchors.MaxBy(anchor =>
        {
            Vector3 toAnchor = GridToWorld(CurrentStack, anchor) - center;
            toAnchor.Y = 0;
            return toAnchor.Normalized().Dot(toMouse);
        });
    }

    // Melee attack on the target from the picked position, moving there first if needed.
    // False, and nothing submitted, if it isn't the player's turn or the target can't be meleed.
    public bool TrySubmitMeleeAttack(Stack target)
    {
        if (!IsPlayerTurn || Battle == null || CurrentStack?.Unit.MeleeAttack is not MeleeAttackAbility melee
            || !_meleeTargets.Contains(target))
            return false;

        Vector2I anchor = _pickedMeleeAttack is { } picked && picked.Target == target
            ? picked.Anchor
            : Battle.GetMeleeAttackAnchors(CurrentStack, target, CurrentReachability)[0];

        var output = new TurnOutput();
        if (anchor != CurrentStack.Anchor)
            output.MoveTo = anchor;
        output.Add(melee, target);
        SubmitPlayerTurn(output);
        return true;
    }

    #endregion

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

    #endregion

    #region Turn execution

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
    private async Task AnimateMove(Stack stack, List<Vector2I> path)
    {
        HideTiles(stack.GetOccupiedTiles());

        stack.NodeRepresentation!.MoveAlong(path);

        await ToSignal(stack.NodeRepresentation!, UnitNode.SignalName.MovementCompleted);
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

        RemoveDeadStacks(battle);
        if (!battle.IsOver)
            battle.StartNewRound();
    }

    // Removes dead stacks from the battle (armies, round queue, board) and their units from the scene.
    // A 2x2 stack's double tile is a child of its unit, so it goes too.
    private static void RemoveDeadStacks(Battle battle)
    {
        foreach (Stack stack in battle.RemoveDeadStacks())
        {
            // TODO: play a death animation and await it before freeing
            stack.NodeRepresentation?.QueueFree();
            stack.NodeRepresentation = null;
        }
    }

    private async Task ApplyEffect(IEffect effect, Stack target)
    {
        if (target.IsDead)
            return;
        await effect.Apply(target, this);
        effect.RemainingDuration--;
    }

    #endregion
}
