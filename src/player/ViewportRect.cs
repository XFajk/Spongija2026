using Godot;

public partial class ViewportRect : TextureRect 
{
	[Export]
	public SubViewport GameViewport;


	public override void _Ready()
	{
		Texture = GameViewport.GetTexture();
		GuiInput += ForwardInputToViewport;
	}

	// The TextureRect only shows the viewport's image, so mouse events are passed on by hand,
	// rescaled from TextureRect pixels to SubViewport pixels (same as SubViewportContainer does).
	private void ForwardInputToViewport(InputEvent @event)
	{
		if (@event is not InputEventMouse)
			return;

		var scale = (Vector2)GameViewport.Size / Size;
		GameViewport.PushInput(@event.XformedBy(Transform2D.Identity.Scaled(scale)));
	}
}
