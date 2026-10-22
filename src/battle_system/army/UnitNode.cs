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

    [Export]
    public MovementStyle MovementStyle { get; set; } = MovementStyle.Walk;

    // Time it takes to complete the movement
    [Export(PropertyHint.Range, "0,100")]
    public float MovementTime { get; set; } = 5.0f;


    public Interactable? UnitInteractable { get; private set; } = null;

    public List<MeshInstance3D> Meshes { get; private set; } = [];

    // The double tile a 2x2 stack stands on, a child of this node; null for 1x1 stacks. Set by Arena.
    public DoubleTile? GroundTile { get; set; } = null;

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

        UnitInteractable.Focused += OnUnitFocused;
        UnitInteractable.Unfocused += OnUnitFocusLost;
        UnitInteractable.InteractionStarted += OnUnitStartInteraction;
        UnitInteractable.Interacting += OnUnitInteracting;
    }

    private void OnUnitFocused(Node user)
    {
        // Handle unit being focused, e.g., highlight meshes
        OutlineModel(true);
    }

    private void OnUnitFocusLost(Node user)
    {
        // Handle unit losing focus, e.g., remove highlight from meshes
        OutlineModel(false);
    }

    private void OnUnitStartInteraction(Node user)
    {
        SetCollisionLayerValue(4, false);
        UnitInteractable!.InteractionLock = true;
        GhostModel(true);
    }

    private void OnUnitInteracting(Node user)
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
        if (GroundTile != null)
        {
            ArenaRef!.ShowTiles(CurrentStack!.GetOccupiedTiles());
            ArenaRef.HideTiles(CurrentStack.GetTilesAt(anchor));
            GroundTile.GridPosition = anchor;
        }

        // The anchor follows the model, so the other stacks' checks see where this one is now
        CurrentStack!.Anchor = anchor;
        // GridToWorld is Arena-local, and the unit is a child of the Arena
        Position = Arena.GridToWorld(CurrentStack, anchor);
    }


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
                material.StencilColor = Colors.White;
                material.StencilOutlineThickness = 0.005f;
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

    public void Setup(Stack stack, Arena arena)
    {
        CurrentStack = stack;
        ArenaRef = arena;
    }

    public async Task MoveAlong(List<Vector2I> gridPath)
    {
        var worldPath = gridPath.Select(Arena.GridToWorld).ToList();
        var tween = CreateTween();
        var delay = MovementTime / gridPath.Count;

        switch (MovementStyle)
        {
            case MovementStyle.Walk:
                tween.SetParallel(false);
                for (int i = 0; i < gridPath.Count; i++)
                {
                    Vector2I point = gridPath[i];
                    Vector2I? nextPoint = i + 1 < gridPath.Count ? gridPath[i + 1] : null;

                    tween.TweenProperty(this, "GlobalPosition", Arena.GridToWorld(point), delay);
                    if (nextPoint is Vector2I next)
                    {
                        tween.TweenCallback(Callable.From(() => LookAt(Arena.GridToWorld(next), Vector3.Up)));
                    }
                }
                tween.TweenCallback(Callable.From(() => EmitSignal(SignalName.MovementCompleted)));
                break;
            case MovementStyle.Dash:
                // TODO
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
                            LookAt(Arena.GridToWorld(next), Vector3.Up);
                        }
                        GlobalPosition = Arena.GridToWorld(point);
                    })).SetDelay(delay);
                }
                tween.TweenCallback(Callable.From(() => EmitSignal(SignalName.MovementCompleted)));
                break;
        }
    }

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
}