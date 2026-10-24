using Godot;

public partial class ViewportRect : TextureRect 
{
	[Export]
	public SubViewport? GameViewport;


	public override void _Ready()
	{
		if (GameViewport != null)
		{
			Texture = GameViewport.GetTexture();
			GuiInput += ForwardInputToViewport;
		}
	}

	// A point in SubViewport pixels (e.g. from Camera3D.UnprojectPosition) as a global canvas position on this rect,
	// where UI can be drawn over it. The inverse of the rescaling in ForwardInputToViewport.
	public Vector2 ViewportToCanvas(Vector2 viewportPoint)
	{
		if (GameViewport == null)
			return viewportPoint;
		return GlobalPosition + viewportPoint * Size / (Vector2)GameViewport.Size;
	}

	// The TextureRect only shows the viewport's image, so mouse events are passed on by hand,
	// rescaled from TextureRect pixels to SubViewport pixels (same as SubViewportContainer does).
	private void ForwardInputToViewport(InputEvent @event)
	{
		if (@event is not InputEventMouse)
			return;

		if (GameViewport == null)
			return;

		var scale = (Vector2)GameViewport.Size / Size;
		GameViewport.PushInput(@event.XformedBy(Transform2D.Identity.Scaled(scale)));
	}
}
