using Godot;

// The background music. Its players live on the tree's root, not in a scene, so a track keeps playing across scene
// changes: a scene asks for its track (MusicCue) and the same track just goes on, while a different one crossfades in.
// Tracks loop by their import settings (loop on, which Ogg Vorbis plays gaplessly). They play on the Music bus and keep
// playing while the tree is paused.
public static class Music
{
    public const string Bus = "Music";

    // Two players, so the old track fades out while the new one fades in
    private static AudioStreamPlayer? _current;
    private static AudioStreamPlayer? _previous;

    // Plays the track at volumeDb, fading it in over fadeDuration seconds and the one playing now out. If it's already
    // playing it goes on, only easing to the new volume. Null fades the music out.
    public static void Play(SceneTree tree, AudioStream? track, float fadeDuration, float volumeDb = 0f)
    {
        float volume = Mathf.DbToLinear(volumeDb);
        if (_current != null && GodotObject.IsInstanceValid(_current) && _current.Stream == track && _current.Playing)
        {
            Fade(tree, _current, volume, fadeDuration, freeAfter: false);
            return;
        }

        // The fading-out one from an earlier change goes at once; the current one becomes the fading-out one
        if (_previous != null && GodotObject.IsInstanceValid(_previous))
            _previous.QueueFree();
        _previous = _current;
        _current = null;
        if (_previous != null && GodotObject.IsInstanceValid(_previous))
            Fade(tree, _previous, 0f, fadeDuration, freeAfter: true);

        if (track == null)
            return;

        _current = new AudioStreamPlayer
        {
            Name = "Music",
            Stream = track,
            Bus = Bus,
            VolumeLinear = 0f,
            ProcessMode = Node.ProcessModeEnum.Always,
            // Starts once it's in the tree
            Autoplay = true,
        };
        // Deferred: scenes ask for their music from _Ready, while the root is still adding them and refuses children
        tree.Root.CallDeferred(Node.MethodName.AddChild, _current);
        Fade(tree, _current, volume, fadeDuration, freeAfter: false);
    }

    // A tree tween, since the player may not be in the tree yet; it runs while the tree is paused
    private static void Fade(SceneTree tree, AudioStreamPlayer player, float volume, float duration, bool freeAfter)
    {
        Tween tween = tree.CreateTween().SetPauseMode(Tween.TweenPauseMode.Process);
        tween.TweenProperty(player, "volume_linear", volume, Mathf.Max(duration, 0.01f));
        if (freeAfter)
            tween.TweenCallback(Callable.From(player.QueueFree));
    }
}
