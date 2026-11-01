using Godot;

// Fire-and-forget sounds for abilities and units (a projectile leaving, landing, a unit attacking). Each gets its
// own AudioStreamPlayer under the given parent, which frees itself once the sound has finished, so a sound outlives
// the projectile or effect that started it. Not positional, like the units' own sounds.
public static class OneShotSound
{
    // Does nothing if stream is null, so abilities can leave their sounds unset
    public static void Play(Node parent, AudioStream? stream, StringName bus) => Spawn(parent, stream, bus, 1f, 0f);

    // With the cue's pitch and volume. Returns the player, so a sound that should end early (a unit's move sound)
    // can be stopped with Stop; null if there's no cue or stream.
    public static AudioStreamPlayer? Play(Node parent, SoundCue? cue) =>
        cue == null ? null : Spawn(parent, cue.Stream, cue.Bus, cue.PitchScale, cue.VolumeDb);

    // Stops a sound started by Play and frees its player (Stop doesn't emit Finished, so it wouldn't free itself)
    public static void Stop(AudioStreamPlayer? player)
    {
        if (player == null || !GodotObject.IsInstanceValid(player))
            return;
        player.Stop();
        player.QueueFree();
    }

    private static AudioStreamPlayer? Spawn(Node parent, AudioStream? stream, StringName bus, float pitch, float volumeDb)
    {
        if (stream == null)
            return null;

        var player = new AudioStreamPlayer { Stream = stream, Bus = bus, PitchScale = pitch, VolumeDb = volumeDb };
        player.Finished += player.QueueFree;
        parent.AddChild(player);
        player.Play();
        return player;
    }
}
