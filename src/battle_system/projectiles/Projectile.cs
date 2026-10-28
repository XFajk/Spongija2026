using Godot;

// A projectile (arrow, bolt, ...) that flies along a parabola from StartPosition to EndPosition, leaving at
// ShootAngle above the horizontal (XZ) plane, and emits Landed when it arrives. Spawn it with Spawn;
// the root of the projectile's scene must be a Projectile.
[GlobalClass]
public partial class Projectile : Node3D
{
	[Signal]
	public delegate void LandedEventHandler();

	// Horizontal speed in m/s (a tile is 0.5 m). The flight takes the horizontal distance divided by this.
	[Export(PropertyHint.Range, "0.5,50")]
	public float HorizontalSpeed { get; set; } = 8f;

	// Global positions, and the angle in degrees above the XZ plane (clamped to 0..85)
	public Vector3 StartPosition { get; private set; }
	public Vector3 EndPosition { get; private set; }
	public float ShootAngle { get; private set; }

	// The direction the projectile is flying in (where an arrow's head points), normalized. Updated every frame of the flight.
	public Vector3 HeadDirection { get; private set; }

	// Instantiates the scene, mounts it under parent and starts the flight. Returns null (and frees the instance)
	// if the scene's root isn't a Projectile. Connect to Landed right after this; it can't fire before the next frame.
	public static Projectile? Spawn(Node parent, PackedScene scene, Vector3 startPosition, Vector3 endPosition, float shootAngle)
	{
		Node instance = scene.Instantiate();
		if (instance is not Projectile projectile)
		{
			GD.PushError($"Projectile.Spawn: the root of {scene.ResourcePath} is not a Projectile");
			instance.Free();
			return null;
		}

		projectile.StartPosition = startPosition;
		projectile.EndPosition = endPosition;
		// Below 0 it would shoot downward, and toward 90 the arc grows without limit (tan(90°) is infinite)
		projectile.ShootAngle = Mathf.Clamp(shootAngle, 0f, 85f);
		parent.AddChild(projectile);
		return projectile;
	}

	public override void _Ready()
	{
		GlobalPosition = StartPosition;

		Vector3 horizontal = EndPosition - StartPosition;
		horizontal.Y = 0;
		float distance = horizontal.Length();

		var tween = CreateTween();
		tween.TweenMethod(Callable.From<float>(Fly), 0f, 1f, Mathf.Max(distance / HorizontalSpeed, 0.05f));
		tween.TweenCallback(Callable.From(() =>
		{
			GlobalPosition = EndPosition;
			EmitSignal(SignalName.Landed);
		}));
	}

	// t goes 0..1 over the flight. Horizontally the projectile moves at a constant speed; vertically its height is
	//   y(t) = a t + b t²,  a = d tan(angle),  b = h - a
	// (d the horizontal distance, h the height difference). That's the parabola of a throw under constant gravity:
	// at t = 0 it rises a over d, so it leaves at the given angle, and at t = 1 it's exactly h higher, at the end point.
	// A steep angle gives a high arc, 0 a flat shot that drops.
	private void Fly(float t)
	{
		Vector3 horizontal = EndPosition - StartPosition;
		float h = horizontal.Y;
		horizontal.Y = 0;
		float d = horizontal.Length();

		// Start and end above each other: there's no direction to shoot in, so go straight
		if (d < 0.001f)
		{
			GlobalPosition = StartPosition.Lerp(EndPosition, t);
			HeadDirection = (EndPosition - StartPosition).Normalized();
			return;
		}

		float a = d * Mathf.Tan(Mathf.DegToRad(ShootAngle));
		float b = h - a;

		GlobalPosition = StartPosition + horizontal * t + Vector3.Up * (a * t + b * t * t);
		// The derivative of the position: horizontal part plus the slope dy/dt = a + 2 b t
		HeadDirection = (horizontal + Vector3.Up * (a + 2f * b * t)).Normalized();
	}
}
