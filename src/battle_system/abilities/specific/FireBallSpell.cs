using Godot;
using System.Threading.Tasks;

[GlobalClass]
public partial class FireBallSpell : SpellAbility
{
    [Export(PropertyHint.Range, "0,1000")]
    public int Damage { get; set; } = 30;

    // The fireball flown from the caster to the target. The scene's root must be a Projectile.
    [Export]
    public PackedScene? Projectile { get; set; }

    // Degrees above the ground the fireball is shot at (Projectile clamps it to 0..85)
    [Export(PropertyHint.Range, "0,85")]
    public float ShootAngle { get; set; } = 20f;

    // Height above the units' origins (on the ground) the fireball leaves from and hits at, in meters
    [Export(PropertyHint.Range, "0,3")]
    public float ShootHeight { get; set; } = 0.5f;

    public override async Task Use(Stack user, Stack target, Arena arena)
    {
        target.TakeDamage(Damage);

        if (user.NodeRepresentation is not UnitNode userNode)
            return;

        userNode.FaceTowards(target);
        await FlyFireBall(userNode.GlobalPosition + Vector3.Up * ShootHeight, target, arena);
        userNode.FaceEnemySide();
    }

    // From where the hero stands; the marker is the launch point itself, so no ShootHeight is added to it
    public override async Task UseFromHero(Stack caster, Vector3 origin, Stack target, Arena arena)
    {
        target.TakeDamage(Damage);
        await FlyFireBall(origin, target, arena);
    }

    private async Task FlyFireBall(Vector3 start, Stack target, Arena arena)
    {
        if (Projectile == null || target.NodeRepresentation is not UnitNode targetNode)
            return;

        Vector3 end = targetNode.GlobalPosition + Vector3.Up * ShootHeight;
        var projectile = global::Projectile.Spawn(arena, Projectile, start, end, ShootAngle);
        if (projectile != null)
        {
            await projectile.ToSignal(projectile, global::Projectile.SignalName.Landed);
            projectile.QueueFree();
        }
    }
}
