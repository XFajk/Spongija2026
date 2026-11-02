using Godot;
using System.Threading.Tasks;

// A black overlay over everything for fading between scenes. It's a child of the tree's root, not of a scene, so it
// stays black while the scene changes under it: fade out, change the scene, then fade in (or Clear) from the new one.
// It blocks the mouse while it's there, so nothing is clicked mid-fade, and it runs while the tree is paused.
public static class ScreenFade
{
    // Above every scene's own canvas layers (the arena's end screen is 10)
    private const int Layer = 100;

    private static CanvasLayer? _layer;
    private static ColorRect? _rect;

    // From the current opacity to black
    public static async Task FadeOut(SceneTree tree, float duration)
    {
        await FadeTo(tree, GetRect(tree, startAlpha: 0f), 1f, duration);
    }

    // From black (or the current opacity) to clear, then the overlay is removed
    public static async Task FadeIn(SceneTree tree, float duration)
    {
        ColorRect rect = GetRect(tree, startAlpha: 1f);
        await FadeTo(tree, rect, 0f, duration);
        // A newer fade may have replaced it meanwhile
        if (rect == _rect)
            Clear();
    }

    // Removes the overlay at once, e.g. when the new scene covers the screen itself (the map's BlackScreen)
    public static void Clear()
    {
        // Deferred, so it comes after the deferred AddChild in GetRect if that hasn't run yet
        if (_layer != null && GodotObject.IsInstanceValid(_layer))
            _layer.CallDeferred(Node.MethodName.QueueFree);
        _layer = null;
        _rect = null;
    }

    // The overlay, made at startAlpha if there isn't one yet
    private static ColorRect GetRect(SceneTree tree, float startAlpha)
    {
        if (_rect != null && GodotObject.IsInstanceValid(_rect))
            return _rect;

        _layer = new CanvasLayer { Name = "ScreenFade", Layer = Layer, ProcessMode = Node.ProcessModeEnum.Always };
        _rect = new ColorRect
        {
            Color = new Color(0, 0, 0, startAlpha),
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        _rect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _layer.AddChild(_rect);
        // Deferred: fades often start from a scene's _Ready (BattleTest, Arena), while the root is still adding
        // that scene and refuses new children
        tree.Root.CallDeferred(Node.MethodName.AddChild, _layer);
        return _rect;
    }

    // A tree tween, not the rect's: the rect may not be in the tree yet (GetRect adds it deferred). It runs while
    // the tree is paused, like the overlay.
    private static async Task FadeTo(SceneTree tree, ColorRect rect, float alpha, float duration)
    {
        if (duration <= 0)
        {
            rect.Color = rect.Color with { A = alpha };
            return;
        }
        Tween tween = tree.CreateTween().SetPauseMode(Tween.TweenPauseMode.Process);
        tween.TweenProperty(rect, "color:a", alpha, duration);
        await rect.ToSignal(tween, Tween.SignalName.Finished);
    }
}
