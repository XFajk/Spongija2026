using Godot;
using System.Threading.Tasks;

[GlobalClass]
public partial class BasicLongRangeAttackAbility : LongRangeAttackAbility
{
    public const string ProjectileScene = "uid://c3crf8etwpgx0";

    [Export(PropertyHint.Range, "0,1")]
    public float BasicAttackMultiplier { get; set; } = 1.0f;

    // Degrees above the ground the projectile is shot at (Projectile clamps it to 0..85)
    [Export(PropertyHint.Range, "0,85")]
    public float ShootAngle { get; set; } = 30f;

    // Height above the units' origins (on the ground) the projectile leaves from and hits at, in meters
    [Export(PropertyHint.Range, "0,3")]
    public float ShootHeight { get; set; } = 0.5f;

    public override async Task Use(Stack user, Stack target, Arena arena)
    {

        if (user.NodeRepresentation is not UnitNode userNode || target.NodeRepresentation is not UnitNode targetNode)
        {
            target.TakeDamage(Battle.CalculateDamage(user, target, BasicAttackMultiplier));
            return;
        }

        userNode.FaceTowards(target);

        // Units stand at their footprint's center, so this also aims at the middle of a 2x2 target
        Vector3 start = userNode.GlobalPosition + Vector3.Up * ShootHeight;
        Vector3 end = targetNode.GlobalPosition + Vector3.Up * ShootHeight;

        var projectile = Projectile.Spawn(arena, GD.Load<PackedScene>(ProjectileScene), start, end, ShootAngle);
        if (projectile != null)
        {
            await projectile.ToSignal(projectile, Projectile.SignalName.Landed);
            projectile.QueueFree();
        }
        // Outside the if, so the hit still lands when the projectile fails to spawn
        target.TakeDamage(Battle.CalculateDamage(user, target, BasicAttackMultiplier));

        userNode.FaceEnemySide();
    }
}
