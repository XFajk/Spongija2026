using Godot;
using System.Collections.Generic;
using System.Linq;

// Scenes store the value as a number, so Teleport keeps 2 (1 was Dash, removed)
public enum MovementStyle
{
    // Moves tile by tile, playing the model's run animation
    Walk = 0,
    // Casts, vanishes and reappears on the last tile inside the unit's TeleportEffect
    Teleport = 2,
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

    // Time it takes to walk one tile (Walk only)
    [Export(PropertyHint.Range, "0.1,1")]
    public float MovementTime { get; set; } = 0.1f;

    // Teleport only: the effect played on the destination (an AbilityVisualEffect scene), freed when it finishes.
    // Without it the unit reappears at once.
    [Export]
    public PackedScene? TeleportEffect { get; set; }

    // Teleport only: how far into TeleportEffect's Duration the unit reappears, 0 at once, 1 as it ends
    [Export(PropertyHint.Range, "0,1")]
    public float TeleportAppearAt { get; set; } = 0f;

    // The TeleportEffect spawned by the last teleport, until it finishes (then null), so the cinematic camera
    // can wait for it (Arena.AnimateMove)
    public AbilityVisualEffect? ActiveTeleportEffect { get; private set; }

    // Where the unit's projectiles leave from (a hand, a bow): e.g. a Marker3D under a BoneAttachment3D, so it
    // follows the animation. Read at the attack's hit frame. If not set, abilities use their own height above the unit.
    [Export]
    public Node3D? LaunchPoint { get; set; }


    public Interactable? UnitInteractable { get; private set; } = null;

    public List<MeshInstance3D> Meshes { get; private set; } = [];

    // The double tile a 2x2 stack stands on, a child of this node; null for 1x1 stacks. Set by Arena.
    public DoubleTile? GroundTile { get; set; } = null;

    // Outline of the unit whose turn it is, so it's clear which stack acts
    [Export]
    public Color ActiveOutlineColor { get; set; } = new(1f, 0.8f, 0.2f);

    [Export(PropertyHint.Range, "0,0.1")]
    public float ActiveOutlineThickness { get; set; } = 0.04f;

    // Sounds the unit makes itself, for models that don't play them from their animations (e.g. the forest ghost,
    // which has no UnitAnimationManager). The move sound plays while the unit moves and stops when it arrives; the
    // others play when their animation starts (PlayMeleeAttack, PlayRangedAttack, PlaySpellCast). None if null.
    [ExportGroup("Sounds")]
    [Export]
    public SoundCue? MoveSound { get; set; }

    [Export]
    public SoundCue? MeleeSound { get; set; }

    [Export]
    public SoundCue? RangedSound { get; set; }

    [Export]
    public SoundCue? CastSound { get; set; }

    // The move sound playing, stopped when the move ends
    private AudioStreamPlayer? _moveSoundPlayer;

    private bool _isActive;
    private bool _hoverOutlined;
    private bool _highlighted;

    // The visual effects of the effects on the stack (ShowEffect), children of this node so they follow the unit
    private readonly Dictionary<IEffect, AbilityVisualEffect> _effectVisuals = new();

    // The model's animations; null for models without them
    private UnitAnimationManager? _animations;

    // Whether it's this unit's turn: shows the active outline. Set by Arena.CurrentStack.
    public bool IsActive
    {
        get => _isActive;
        set
        {
            _isActive = value;
            RefreshOutline();
        }
    }

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        UnitInteractable = this.FindDescendant<Interactable>();
        Meshes = this.FindDescendants<MeshInstance3D>();
        _animations = this.FindDescendant<UnitAnimationManager>();

        // Give each unit its own materials, so outlining one doesn't outline every unit sharing the scene.
        // Imported models (.glb) keep their materials on the Mesh resource, not in the node's surface overrides,
        // so each surface's material (the override if set, else the mesh's own) is copied into the overrides.
        // The outline and ghost effects only touch the overrides.
        foreach (var mesh in Meshes)
        {
            if (mesh.Mesh == null)
                continue;
            for (int i = 0; i < mesh.Mesh.GetSurfaceCount(); i++)
            {
                Material? material = mesh.GetSurfaceOverrideMaterial(i) ?? mesh.Mesh.SurfaceGetMaterial(i);
                if (material is StandardMaterial3D standard)
                    mesh.SetSurfaceOverrideMaterial(i, (StandardMaterial3D)standard.Duplicate());
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

    // The hover outline, in the side's color. While it's off, the active outline shows if the unit is active.
    public void OutlineModel(bool enable)
    {
        _hoverOutlined = enable;
        RefreshOutline();
    }

    // The hover outline, set by UI that points at the unit (the turn queue's icons). Its own flag, so the camera's
    // hover and unhover don't clear it.
    public bool IsHighlighted
    {
        get => _highlighted;
        set
        {
            if (_highlighted == value)
                return;
            _highlighted = value;
            RefreshOutline();
        }
    }

    // The hover outline wins over the active one, so hovering the active unit still shows it can be targeted
    private void RefreshOutline()
    {
        bool hovered = _hoverOutlined || _highlighted;
        bool enable = hovered || _isActive;
        Color color = hovered ? (IsPlayerUnit ? Colors.White : Colors.Red) : ActiveOutlineColor;
        float thickness = hovered ? 0.025f : ActiveOutlineThickness;

        foreach (var mesh in Meshes)
        {
            for (int i = 0; i < mesh.GetSurfaceOverrideMaterialCount(); i++)
            {
                if (mesh.GetSurfaceOverrideMaterial(i) is not StandardMaterial3D material) continue;

                // WARNING: GODOT stencil API is only Experimental may break on GODOT version's after 4.7.2
                material.StencilMode = enable ? BaseMaterial3D.StencilModeEnum.Outline : BaseMaterial3D.StencilModeEnum.Disabled;
                if (!enable) continue;
                material.StencilReference = 1;
                material.StencilColor = color;
                material.StencilOutlineThickness = thickness;
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

    // Plays the effect's visual effect on the unit (at its origin, the footprint center on the ground) until
    // HideEffect. Called by Stack.AddEffect; the visual should be Endless, as nothing frees it when it finishes.
    public void ShowEffect(IEffect effect)
    {
        if (effect.VisualEffect == null || _effectVisuals.ContainsKey(effect))
            return;

        var visual = AbilityVisualEffect.Spawn(this, effect.VisualEffect, GlobalPosition);
        if (visual != null)
            _effectVisuals[effect] = visual;
    }

    // Called by Stack.RemoveExpiredEffects. The visuals also go with the unit when it's freed.
    public void HideEffect(IEffect effect)
    {
        if (_effectVisuals.Remove(effect, out AbilityVisualEffect? visual) && IsInstanceValid(visual))
            visual.QueueFree();
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

    // Where a projectile leaves the unit: LaunchPoint if set, else height meters above the unit's origin (global).
    // The model's own UnitAnimationManager.LaunchPoint counts too.
    public Vector3 GetLaunchPosition(float height) =>
        LaunchPoint != null && IsInstanceValid(LaunchPoint) ? LaunchPoint.GlobalPosition
        : _animations?.GetLaunchPosition() ?? GlobalPosition + Vector3.Up * height;

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

    // Emits MovementCompleted when done, never before returning, so callers can await the signal after calling.
    public void MoveAlong(List<Vector2I> gridPath)
    {
        if (gridPath.Count == 0)
        {
            CallDeferred(GodotObject.MethodName.EmitSignal, SignalName.MovementCompleted);
            return;
        }

        switch (MovementStyle)
        {
            case MovementStyle.Walk:
                WalkAlong(gridPath);
                break;
            case MovementStyle.Teleport:
                TeleportTo(gridPath[^1]);
                break;
        }
    }

    private void WalkAlong(List<Vector2I> gridPath)
    {
        var tween = CreateTween();
        StartMoveSound();
        tween.Finished += StopMoveSound;

        tween.SetParallel(false);
        tween.TweenCallback(Callable.From(() => SetRunning(true)));
        foreach (Vector2I point in gridPath)
        {
            tween.TweenCallback(Callable.From(() => FaceTowards(point)));
            // Engine property name (snake_case), not the C# one
            tween.TweenProperty(this, "position", GridToLocal(point), MovementTime);
        }
        tween.TweenCallback(Callable.From(() => SetRunning(false)));
        tween.TweenCallback(Callable.From(FaceEnemySide));
        tween.TweenCallback(Callable.From(() => EmitSignal(SignalName.MovementCompleted)));
    }

    // Faces the destination and plays the spell cast, then vanishes; TeleportEffect plays on the destination and
    // the unit reappears there TeleportAppearAt of the way through it. The path between is skipped.
    private async void TeleportTo(Vector2I destination)
    {
        FaceTowards(destination);

        // Completes at once for models without an animation manager
        await PlaySpellCast().Finished;
        if (!IsInstanceValid(this) || !IsInsideTree())
            return;

        StartMoveSound();
        Visible = false;

        Vector3 localTarget = GridToLocal(destination);
        Vector3 globalTarget = ArenaRef != null ? ArenaRef.ToGlobal(localTarget) : localTarget;

        AbilityVisualEffect? effect = TeleportEffect != null
            ? AbilityVisualEffect.Spawn(SoundParent, TeleportEffect, globalTarget)
            : null;
        ActiveTeleportEffect = effect;
        if (effect != null)
        {
            effect.Finished += () =>
            {
                if (IsInstanceValid(this) && ActiveTeleportEffect == effect)
                    ActiveTeleportEffect = null;
                FreeTeleportEffect(effect);
            };

            float appearDelay = effect.Duration * Mathf.Clamp(TeleportAppearAt, 0f, 1f);
            if (appearDelay > 0f)
            {
                await ToSignal(GetTree().CreateTimer(appearDelay, processAlways: false), SceneTreeTimer.SignalName.Timeout);
                if (!IsInstanceValid(this) || !IsInsideTree())
                    return;
            }
        }

        Position = localTarget;
        FaceEnemySide();
        Visible = true;
        StopMoveSound();
        EmitSignal(SignalName.MovementCompleted);
    }

    // Stops the effect's particles emitting (the root, whose script hides its GPUParticles3D type from C#, and any
    // child emitters) and frees it once the longest-lived of their last particles have faded
    private static void FreeTeleportEffect(AbilityVisualEffect effect)
    {
        if (!IsInstanceValid(effect))
            return;

        if (!effect.IsInsideTree())
        {
            effect.QueueFree();
            return;
        }

        var emitters = new List<Node>();
        if (effect.IsClass("GPUParticles3D"))
            emitters.Add(effect);
        emitters.AddRange(effect.FindChildren("*", "GPUParticles3D", true, false));

        float lifetime = 0f;
        foreach (Node emitter in emitters)
        {
            emitter.Set(GpuParticles3D.PropertyName.Emitting, false);
            lifetime = Mathf.Max(lifetime, (float)emitter.Get(GpuParticles3D.PropertyName.Lifetime).AsDouble());
        }

        effect.GetTree().CreateTimer(lifetime, processAlways: false).Timeout += () =>
        {
            if (IsInstanceValid(effect))
                effect.QueueFree();
        };
    }

    #endregion

    #region Animations

    // The model's one-shot animations (UnitAnimationManager plays them); UnitAnimation.None without a manager

    // Each also plays the unit's own sound for it, if it has one (#region Sounds)

    // Plays the model's melee attack; Hit is when the weapon connects
    public UnitAnimation PlayMeleeAttack()
    {
        PlaySound(MeleeSound);
        return _animations?.PlayMeleeAttack() ?? UnitAnimation.None;
    }

    // Plays the model's ranged attack; Hit is when the shot leaves
    public UnitAnimation PlayRangedAttack()
    {
        PlaySound(RangedSound);
        return _animations?.PlayRangedAttack() ?? UnitAnimation.None;
    }

    // Plays the model's spell cast; Hit is when the spell is released
    public UnitAnimation PlaySpellCast()
    {
        PlaySound(CastSound);
        return _animations?.PlaySpellCast() ?? UnitAnimation.None;
    }

    private void SetRunning(bool running)
    {
        if (_animations != null)
            _animations.IsRunning = running;
    }

    #endregion

    #region Sounds

    // Under the Arena rather than the unit, so a sound plays out even if the unit dies right after
    private Node SoundParent => (Node?)ArenaRef ?? GetParent() ?? this;

    private void PlaySound(SoundCue? cue) => OneShotSound.Play(SoundParent, cue);

    private void StartMoveSound()
    {
        StopMoveSound();
        _moveSoundPlayer = OneShotSound.Play(SoundParent, MoveSound);
    }

    private void StopMoveSound()
    {
        OneShotSound.Stop(_moveSoundPlayer);
        _moveSoundPlayer = null;
    }

    public override void _ExitTree()
    {
        StopMoveSound();
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