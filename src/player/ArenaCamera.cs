using Godot;
using System;
using System.Threading.Tasks;

public partial class ArenaCamera : PlayerCamera
{
    public Arena? arena;

    // The close shots during turns (Arena's cinematic camera): the camera looks at the focus point from
    // ShotDistance away, pitched ShotPitch down, keeping the heading it had when the shots started
    [ExportGroup("Cinematic shots")]
    [Export(PropertyHint.Range, "0.5,10")]
    public float ShotDistance { get; set; } = 3f;

    [Export(PropertyHint.Range, "5,85")]
    public float ShotPitch { get; set; } = 30f;

    // How fast the camera catches up with the shot; higher is snappier
    [Export(PropertyHint.Range, "1,20")]
    public float ShotFollowSpeed { get; set; } = 5f;

    // Where to look at, read every frame so the shot follows moving things; null when not shooting
    private Func<Vector3>? _shotFocus;
    private float _shotYaw;

    // Starts following the focus point, or stops with null (the camera then stays where it is)
    public void SetShotFocus(Func<Vector3>? focus)
    {
        if (_shotFocus == null && focus != null)
            _shotYaw = GlobalRotation.Y;
        _shotFocus = focus;
    }

    public override void _Ready()
    {
        base._Ready();

        // Terrain3D picks its LODs and foliage from where its camera is, and this camera renders in a SubViewport,
        // where Terrain3D can't find it by itself. Every terrain, since the arena has one per scenery (forest, hill).
        foreach (Node terrain in GetTree().GetNodesInGroup("Terrain"))
            terrain.Call("set_camera", this);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (_shotFocus == null)
            return;
        // The camera looks along its -Z, so it stands on its +Z side of the focus point
        var basis = Basis.FromEuler(new Vector3(-Mathf.DegToRad(ShotPitch), _shotYaw, 0));
        var shot = new Transform3D(basis, _shotFocus() + basis.Z * ShotDistance);
        float weight = 1f - Mathf.Exp(-ShotFollowSpeed * (float)delta);
        GlobalTransform = GlobalTransform.InterpolateWith(shot, weight);
    }

    // Where the mouse ray crosses the board's plane (through the Arena's origin), in global coordinates.
    // Flat on the board even when the mouse is over a tall model, so it can pick a side of the unit.
    // Null if there's no arena or the ray doesn't point at the plane.
    public Vector3? GetBoardHoverPoint()
    {
        if (arena == null)
            return null;
        var mousePosition = GetViewport().GetMousePosition();
        var boardPlane = new Plane(arena.GlobalBasis.Y.Normalized(), arena.GlobalPosition);
        return boardPlane.IntersectsRay(ProjectRayOrigin(mousePosition), ProjectRayNormal(mousePosition));
    }

    // Smoothly moves and turns the camera to the target's global transform. Completes when it arrives.
    public async Task MoveTo(Node3D target, float duration)
    {
        var tween = CreateTween()
            .SetParallel()
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
        tween.TweenProperty(this, "global_position", target.GlobalPosition, duration);
        // Quaternions are slerped, so the rotation takes the shortest way without skewing the basis
        tween.TweenProperty(this, "quaternion", target.GlobalBasis.GetRotationQuaternion(), duration);
        await ToSignal(tween, Tween.SignalName.Finished);
    }
}
