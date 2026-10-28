using Godot;
using System;
using System.Threading.Tasks;

public partial class ArenaCamera : PlayerCamera
{
    public Arena? arena;

    public override void _Ready()
    {
        base._Ready();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
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
