using Godot;

// A visual effect an ability plays at a point (a heal glow, an explosion, ...) that emits Finished after Duration.
// Spawn it with Spawn; the root of the effect's scene must be an AbilityVisualEffect. It doesn't free itself.
// The effect is moved to the position, so build the scene around its origin.
[GlobalClass]
public partial class AbilityVisualEffect : Node3D
{
	[Signal]
	public delegate void FinishedEventHandler();

	// Seconds from spawning until Finished
	[Export(PropertyHint.Range, "0,10")]
	public float Duration { get; set; } = 1f;

	// Global position the effect plays at
	public Vector3 EffectPosition { get; private set; }

	// Instantiates the scene, mounts it under parent and starts the effect. Returns null (and frees the instance)
	// if the scene's root isn't an AbilityVisualEffect. Connect to Finished right after this; it can't fire before
	// the next frame.
	public static AbilityVisualEffect? Spawn(Node parent, PackedScene scene, Vector3 position)
	{
		Node instance = scene.Instantiate();
		if (instance is not AbilityVisualEffect effect)
		{
			GD.PushError($"AbilityVisualEffect.Spawn: the root of {scene.ResourcePath} is not an AbilityVisualEffect");
			instance.Free();
			return null;
		}

		effect.EffectPosition = position;
		parent.AddChild(effect);
		return effect;
	}

	public override void _Ready()
	{
		GlobalPosition = EffectPosition;

		var tween = CreateTween();
		tween.TweenInterval(Mathf.Max(Duration, 0.01f));
		tween.TweenCallback(Callable.From(() => EmitSignal(SignalName.Finished)));
	}
}
