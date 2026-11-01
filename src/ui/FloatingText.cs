using Godot;
using System;

public partial class FloatingText : Label
{
	private float lifetime = 0.6f;
	private float elapsed = 0f;
	private float moveSpeed = 150f;

	public override void _Process(double delta)
	{		
		elapsed += (float)delta;

		Position += new Vector2(0, -moveSpeed * (float)delta);

		if (elapsed >= lifetime)
			QueueFree();
	}
}
