using Godot;
using System;
using System.Threading.Tasks;

[GlobalClass]
public partial class BasicMeleeAttackAbility : MeleeAttackAbility
{
    [Export(PropertyHint.Range, "0,1")]
    public float BasicAttackMultiplier { get; set; } = 1.0f;

    // How far the unit springs toward the target at the peak of the attack, in meters (a tile is 0.5 m).
    [Export(PropertyHint.Range, "0,1")]
    public float LungeDistance { get; set; } = 0.25f;

    // Seconds from the start of the lunge until the unit has settled back at its position.
    [Export(PropertyHint.Range, "0.1,3")]
    public float LungeDuration { get; set; } = 0.7f;

    public override Task Use(Stack user, Stack target, Arena arena)
    {
        target.TakeDamage(user.GetStrength() * (int)user.Quantity);

        var tcs = new TaskCompletionSource();

        var maybeUserNode = user.NodeRepresentation;
        var maybeTargetNode = target.NodeRepresentation;

        if (maybeUserNode is not UnitNode userNode || maybeTargetNode is not UnitNode targetNode)
            return Task.CompletedTask;

        // Local positions: both units are children of the Arena, so this works wherever the Arena is
        Vector3 start = userNode.Position;
        Vector3 direction = targetNode.Position - start;
        direction.Y = 0;
        direction = direction.Normalized();

        var tween = userNode.CreateTween();
        tween.TweenCallback(Callable.From(() => userNode.FaceTowards(target)));
        // One springy motion out toward the target and back, instead of two tweens that stop in between
        tween.TweenMethod(
            Callable.From<float>(t => userNode.Position = start + direction * LungeDistance * Lunge(t)),
            0f, 1f, LungeDuration);
        tween.TweenCallback(Callable.From(() =>
        {
            userNode.Position = start;
            userNode.FaceEnemySide();
            tcs.SetResult();
        }));

        return tcs.Task;
    }

    // A damped spring over t = 0..1: 0 at the start, a full swing out (peak 1 at t ~ 0.12), a small recoil behind
    // the start, a smaller swing out, and exactly 0 again at t = 1. The 2 scales the peak (~0.5) up to 1.
    private static float Lunge(float t) => 2f * Mathf.Exp(-5f * t) * Mathf.Sin(3f * Mathf.Pi * t);
}