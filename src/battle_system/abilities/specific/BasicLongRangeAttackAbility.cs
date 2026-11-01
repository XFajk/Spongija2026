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

    // Height above the units' origins (on the ground) the projectile hits at, in meters, and leaves from if the
    // shooter has no UnitNode.LaunchPoint
    [Export(PropertyHint.Range, "0,3")]
    public float ShootHeight { get; set; } = 0.5f;

    // Played when the projectile leaves and when it lands (OneShotSound); none if null
    [ExportGroup("Sounds")]
    [Export]
    public AudioStream? LaunchSound { get; set; }

    [Export]
    public AudioStream? HitSound { get; set; }

    [Export]
    public StringName SoundBus { get; set; } = "SFX";

    public override int GetDamage(Stack user, Stack target) => Battle.CalculateDamage(user, target, BasicAttackMultiplier);

    public override async Task Use(Stack user, Stack target, Arena arena)
    {

        if (user.NodeRepresentation is not UnitNode userNode || target.NodeRepresentation is not UnitNode targetNode)
        {
            target.TakeDamage(GetDamage(user, target));
            return;
        }

        userNode.FaceTowards(target);
        // The projectile leaves at the shot's hit frame (right away if the model has no shooting animation)
        UnitAnimation shot = userNode.PlayRangedAttack();
        await shot.Hit;

        // Units stand at their footprint's center, so this also aims at the middle of a 2x2 target
        Vector3 start = userNode.GetLaunchPosition(ShootHeight);
        Vector3 end = targetNode.GlobalPosition + Vector3.Up * ShootHeight;

        var projectile = Projectile.Spawn(arena, GD.Load<PackedScene>(ProjectileScene), start, end, ShootAngle);
        if (projectile != null)
        {
            OneShotSound.Play(arena, LaunchSound, SoundBus);
            arena.FollowWithCamera(user, projectile);
            await projectile.ToSignal(projectile, Projectile.SignalName.Landed);
            projectile.QueueFree();
            OneShotSound.Play(arena, HitSound, SoundBus);
        }
        // Outside the if, so the hit still lands when the projectile fails to spawn
        target.TakeDamage(GetDamage(user, target));

        await shot.Finished;
        userNode.FaceEnemySide();
    }
}
