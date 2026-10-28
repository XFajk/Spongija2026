using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public enum MovementStyle
{
    Walk,
    Dash,
    Teleport,
}

public partial class UnitNode : Area3D, IInteractable
{
    #region Fields

    [Signal]
    public delegate void MovementCompletedEventHandler();

    private Vector2I _gridPosition;
    public Vector2I GridPosition
    {
        get => CurrentStack == null ? _gridPosition : CurrentStack.Anchor;
        set => _gridPosition = value;
    }

    public Arena? ArenaRef { get; set; } = null;

    public Stack? CurrentStack { get; set; } = null;

    // Which side the unit fights for; picks its battle interaction callbacks. Set by Arena when spawning.
    public bool IsPlayerUnit { get; set; } = true;

    [Export]
    public MovementStyle MovementStyle { get; set; } = MovementStyle.Walk;

    // Time it takes to complete one movement
    [Export(PropertyHint.Range, "0.1,1")]
    public float MovementTime { get; set; } = 0.1f;


    public Interactable? UnitInteractable { get; private set; } = null;

    public List<MeshInstance3D> Meshes { get; private set; } = [];

    // The double tile a 2x2 stack stands on, a child of this node; null for 1x1 stacks. Set by Arena.
    public DoubleTile? GroundTile { get; set; } = null;

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        UnitInteractable = this.FindDescendant<Interactable>();
        Meshes = this.FindDescendants<MeshInstance3D>();

        // Give each unit its own materials, so outlining one doesn't outline every unit sharing the scene
        foreach (var mesh in Meshes)
        {
            for (int i = 0; i < mesh.GetSurfaceOverrideMaterialCount(); i++)
            {
                if (mesh.GetActiveMaterial(i) is StandardMaterial3D material)
                    mesh.SetSurfaceOverrideMaterial(i, (StandardMaterial3D)material.Duplicate());
            }
        }

        if (UnitInteractable == null)
        {
            GD.PrintErr("UnitInteractable not found in UnitNode");
            return;
        }

        // Focus works the same in both phases; the interaction callbacks start as the setup (dragging) ones
        UnitInteractable.Focused += OnUnitFocused;
        UnitInteractable.Unfocused += OnUnitFocusLost;
        UnitInteractable.InteractionStarted += OnSetupInteractionStarted;
        UnitInteractable.Interacting += OnSetupInteracting;
    }

    // Swaps the setup (dragging) interaction callbacks for the battle ones of the unit's side.
    // Call once, when the battle starts.
    public void SwitchToBattleInteractions()
    {
        if (UnitInteractable == null)
            return;

        UnitInteractable.InteractionStarted -= OnSetupInteractionStarted;
        UnitInteractable.Interacting -= OnSetupInteracting;

        if (IsPlayerUnit)
        {
            UnitInteractable.InteractionStarted += OnPlayerUnitInteractionStarted;
            UnitInteractable.Interacting += OnPlayerUnitInteracting;
            UnitInteractable.InteractionFinished += OnPlayerUnitInteractionFinished;
        }
        else
        {
            UnitInteractable.InteractionStarted += OnComputerUnitInteractionStarted;
            UnitInteractable.Interacting += OnComputerUnitInteracting;
            UnitInteractable.InteractionFinished += OnComputerUnitInteractionFinished;
        }
    }

    #endregion

    #region Focus (both phases)

    private void OnUnitFocused(Node user)
    {
        // Handle unit being focused, e.g., highlight meshes
        if (ArenaRef?.IsInSetupPhase ?? false)
        {
            OutlineModel(true);
        }
        else
        {
            if ((ArenaRef?.CanCurrentStackMelee(CurrentStack!) ?? false) || (ArenaRef?.CurrentStack?.HasLongRange ?? false))
            {
                OutlineModel(true);
            }
        }

    }

    private void OnUnitFocusLost(Node user)
    {
        OutlineModel(false);
    }

    #endregion

    #region Setup phase (dragging)

    private void OnSetupInteractionStarted(Node user)
    {
        SetCollisionLayerValue(4, false);
        UnitInteractable!.InteractionLock = true;
        GhostModel(true);
    }

    private void OnSetupInteracting(Node user)
    {
        if (user is not ArenaCamera player)
            return;

        // Checked before the tile checks, so releasing over an invalid tile or nothing still ends the drag;
        // the stack stays on the last valid tile it was dragged over
        if (!Input.IsActionPressed("interact"))
        {
            SetCollisionLayerValue(4, true);
            UnitInteractable!.InteractionLock = false;
            GhostModel(false);
            return;
        }

        // A 2x2 stack's double tile stays on while dragging and hides the small tiles under it, so hovering
        // inside the footprint focuses the double tile and nothing moves; only small tiles outside it move the stack
        if (player.CurrentFocusedNode is not SmallTile tile)
            return;

        // The closest anchor that covers the hovered tile, checked against the placement columns and the other stacks
        if (ArenaRef?.FindSetupAnchorCovering(CurrentStack!, tile.GridPosition) is not Vector2I anchor)
            return;

        MoveDuringSetup(anchor);
    }

    // Moves the stack, its model and its double tile to the anchor, swapping which small tiles are hidden.
    private void MoveDuringSetup(Vector2I anchor)
    {
        List<Vector2I> oldTiles = CurrentStack!.GetOccupiedTiles();

        // The anchor follows the model, so the other stacks' checks see where this one is now.
        // Moved before showing the tiles: Arena decides by the current anchors which tiles the double tile
        // stands in for, so the old tiles left behind get their small tiles back and the new ones hand over to it
        CurrentStack.Anchor = anchor;

        // The double tile's GridPosition follows CurrentStack.Anchor by itself
        if (GroundTile != null)
            ArenaRef!.ShowTiles(oldTiles.Concat(CurrentStack.GetOccupiedTiles()));

        // GridToWorld is Arena-local, and the unit is a child of the Arena
        Position = Arena.GridToWorld(CurrentStack, anchor);
    }

    #endregion

    #region Battle phase, player's units

    // TODO: filled in as the battle input is built

    private void OnPlayerUnitInteractionStarted(Node user)
    {
        if (CurrentStack == null || ArenaRef == null || !ArenaRef.IsPlayerTurn)
            return;

        // An own unit can only be clicked as the target of a spell for allies
        ArenaRef.TrySubmitSpell(CurrentStack);
    }

    private void OnPlayerUnitInteracting(Node user)
    {
    }

    private void OnPlayerUnitInteractionFinished(Node user)
    {
    }

    #endregion

    #region Battle phase, computer's units

    // Enemies to the player. TODO: filled in as the battle input is built

    private void OnComputerUnitInteractionStarted(Node user)
    {
        if (CurrentStack == null || ArenaRef == null)
            return;

        if (!ArenaRef.IsPlayerTurn)
            return;

        // While a spell's target is being picked, a click casts it (the melee and ranged input is off meanwhile)
        if (ArenaRef.TrySubmitSpell(CurrentStack))
            return;

        // Melee comes first: an enemy the current stack can reach in melee can't be shot (Battle.GetRangedTargets).
        // The attack position is the one Arena picked from where the mouse is over the unit.
        if (ArenaRef.TrySubmitMeleeAttack(CurrentStack))
            return;

        ArenaRef.TrySubmitRangedAttack(CurrentStack);
    }

    private void OnComputerUnitInteracting(Node user)
    {
    }

    private void OnComputerUnitInteractionFinished(Node user)
    {
    }

    #endregion

    #region Visual effects

    public void OutlineModel(bool enable)
    {
        foreach (var mesh in Meshes)
        {
            for (int i = 0; i < mesh.GetSurfaceOverrideMaterialCount(); i++)
            {
                if (mesh.GetSurfaceOverrideMaterial(i) is not StandardMaterial3D material) continue;

                // WARNING: GODOT stencil API is only Experimental may break on GODOT version's after 4.7.2
                material.StencilMode = enable ? BaseMaterial3D.StencilModeEnum.Outline : BaseMaterial3D.StencilModeEnum.Disabled;
                if (!enable) continue;
                material.StencilReference = 1;
                material.StencilColor = IsPlayerUnit ? Colors.White : Colors.Red;
                material.StencilOutlineThickness = 0.025f;
            }
        }
    }

    public void GhostModel(bool enable)
    {
        foreach (var mesh in Meshes)
        {
            for (int i = 0; i < mesh.GetSurfaceOverrideMaterialCount(); i++)
            {
                if (mesh.GetSurfaceOverrideMaterial(i) is not StandardMaterial3D material) continue;
                material.AlbedoColor = enable ? new Color(material.AlbedoColor.R, material.AlbedoColor.G, material.AlbedoColor.B, 0.5f) : new Color(material.AlbedoColor.R, material.AlbedoColor.G, material.AlbedoColor.B, 1f);
                material.Transparency = enable ? BaseMaterial3D.TransparencyEnum.Alpha : BaseMaterial3D.TransparencyEnum.Disabled;
            }
        }
    }

    #endregion

    #region Stack and movement

    public void Setup(Stack stack, Arena arena)
    {
        CurrentStack = stack;
        ArenaRef = arena;
    }

    // Where the model stands with its anchor on the grid point: the footprint center, so 2x2 stacks line up
    // with their 4 tiles. Arena-local, like Position, since the unit is a child of the Arena.
    private Vector3 GridToLocal(Vector2I anchor) =>
        CurrentStack != null ? Arena.GridToWorld(CurrentStack, anchor) : Arena.GridToWorld(anchor);

    // Turns the unit to face where it would stand with its anchor on the grid point (for its own moves).
    public void FaceTowards(Vector2I anchor) => FaceTowardsLocal(GridToLocal(anchor));

    // Turns the unit to face another stack: the center of its footprint, so a 2x2 target is faced in its middle.
    public void FaceTowards(Stack target) => FaceTowardsLocal(Arena.GridToWorld(target, target.Anchor));

    // Turns the unit to face an Arena-local point. The double tile is a child, so it's turned back square to the board.
    private void FaceTowardsLocal(Vector3 localTarget)
    {
        Vector3 target = ArenaRef != null ? ArenaRef.ToGlobal(localTarget) : localTarget;
        if (target.IsEqualApprox(GlobalPosition))
            return;
        LookAt(target, Vector3.Up);
        KeepGroundTileSquare();
    }

    // Turns the unit back toward the enemy side, as it was spawned (Arena.SpawnUnitNode):
    // right for the player's units, left for the computer's.
    public void FaceEnemySide()
    {
        RotationDegrees = new Vector3(0, IsPlayerUnit ? -90 : 90, 0);
        KeepGroundTileSquare();
    }

    // The double tile is a child, so turning the unit turns it too; this lays it back square to the board.
    private void KeepGroundTileSquare()
    {
        if (GroundTile != null && ArenaRef != null)
            GroundTile.GlobalBasis = ArenaRef.GlobalBasis * Basis.FromEuler(new Vector3(-Mathf.Pi / 2, 0, 0));
    }

    public void MoveAlong(List<Vector2I> gridPath)
    {
        var tween = CreateTween();
        var delay = MovementTime;

        switch (MovementStyle)
        {
            // TODO: Dash has no animation yet, so it walks; otherwise MovementCompleted would never fire
            case MovementStyle.Dash:
            case MovementStyle.Walk:
                tween.SetParallel(false);
                foreach (Vector2I point in gridPath)
                {
                    tween.TweenCallback(Callable.From(() => FaceTowards(point)));
                    // Engine property name (snake_case), not the C# one
                    tween.TweenProperty(this, "position", GridToLocal(point), delay);
                }
                tween.TweenCallback(Callable.From(FaceEnemySide));
                tween.TweenCallback(Callable.From(() => EmitSignal(SignalName.MovementCompleted)));
                break;
            case MovementStyle.Teleport:
                tween.SetParallel(false);
                for (int i = 0; i < gridPath.Count; i++)
                {
                    Vector2I point = gridPath[i];
                    Vector2I? nextPoint = i + 1 < gridPath.Count ? gridPath[i + 1] : null;

                    tween.TweenCallback(Callable.From(() =>
                    {
                        if (nextPoint is Vector2I next)
                        {
                            FaceTowards(next);
                        }
                        Position = GridToLocal(point);
                    })).SetDelay(delay);
                }
                tween.TweenCallback(Callable.From(FaceEnemySide));
                tween.TweenCallback(Callable.From(() => EmitSignal(SignalName.MovementCompleted)));
                break;
        }
    }

    #endregion

    #region IInteractable

    public void Interact(Node user)
    {
        UnitInteractable?.Interact(user);
    }

    public void Focus(Node user)
    {
        UnitInteractable?.Focus(user);
    }

    public void FocusLost(Node user)
    {
        UnitInteractable?.FocusLost(user);
    }

    #endregion
}