using Godot;

// Works around the macOS embedded game view (Godot 4.7.2): the editor remembers the last mouse mode the game asked
// for, doesn't forget it when the game stops, and applies it to the next run whenever the view gets focus. A new game
// starts out Visible, so setting Visible changes nothing and is never sent to the editor. The mode has to really
// change for the editor to hear about it, hence Hidden first.
public partial class MouseModeReset : Node
{
    public override void _Ready()
    {
        if (!OS.HasFeature("editor"))
            return;

        Input.MouseMode = Input.MouseModeEnum.Hidden;
        Input.MouseMode = Input.MouseModeEnum.Visible;
    }
}
