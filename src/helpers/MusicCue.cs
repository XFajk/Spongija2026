using Godot;

// Put one in a scene to choose its music: when the scene loads, Music plays Track (or fades the music out if it's
// empty). A scene with the same track as the one before keeps it playing without a break.
[GlobalClass]
public partial class MusicCue : Node
{
	[Export]
	public AudioStream? Track { get; set; }

	// The track's volume in this scene, on top of the Music bus's; 0 is the track as it is, -6 about half as loud.
	// A scene with the same track but another volume eases to it over FadeDuration.
	[Export(PropertyHint.Range, "-40,6,0.1,suffix:dB")]
	public float VolumeDb { get; set; }

	// Seconds the old track takes to fade out and this one to fade in
	[Export(PropertyHint.Range, "0,10")]
	public float FadeDuration { get; set; } = 2f;

	public override void _Ready()
	{
		Music.Play(GetTree(), Track, FadeDuration, VolumeDb);
	}
}
