using Godot;

// A sound and how to play it, so one stream can sound different per use (e.g. the forest ghost's single sound,
// pitched down for moving and up for attacking). Played with OneShotSound.
[GlobalClass]
public partial class SoundCue : Resource
{
    [Export]
    public AudioStream? Stream { get; set; }

    // Below 1 lower and slower (and longer), above 1 higher and faster
    [Export(PropertyHint.Range, "0.01,4,0.01")]
    public float PitchScale { get; set; } = 1f;

    [Export(PropertyHint.Range, "-40,20,0.1,suffix:dB")]
    public float VolumeDb { get; set; }

    [Export]
    public StringName Bus { get; set; } = "SFX";
}
