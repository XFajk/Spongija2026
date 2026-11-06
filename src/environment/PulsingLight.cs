using Godot;

// Makes an omni light slowly grow and shrink around the range and energy it has in the scene, following smooth noise
// so it looks alive rather than mechanical (the fireflies' glow). Every instance gets its own noise seed, so lights
// don't pulse in step.
[GlobalClass]
public partial class PulsingLight : OmniLight3D
{
	// Share of the scene's range the light grows or shrinks by at most (0.1 = 10%)
	[Export(PropertyHint.Range, "0,0.5")]
	public float RangeVariation { get; set; } = 0.1f;

	// Share of the scene's energy the light brightens or dims by at most, in step with the range
	[Export(PropertyHint.Range, "0,0.5")]
	public float EnergyVariation { get; set; } = 0.15f;

	// How fast it changes; around 1 is a slow breathing, higher is a flicker
	[Export(PropertyHint.Range, "0.05,10")]
	public float Speed { get; set; } = 0.8f;

	private float _baseRange;
	private float _baseEnergy;
	private float _time;
	private readonly FastNoiseLite _noise = new()
	{
		NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex,
		Frequency = 1f,
	};

	public override void _Ready()
	{
		_baseRange = OmniRange;
		_baseEnergy = LightEnergy;
		_noise.Seed = (int)GD.Randi();
	}

	public override void _Process(double delta)
	{
		_time += (float)delta * Speed;
		// -1..1
		float pulse = _noise.GetNoise1D(_time);
		OmniRange = _baseRange * (1 + pulse * RangeVariation);
		LightEnergy = _baseEnergy * (1 + pulse * EnergyVariation);
	}
}
