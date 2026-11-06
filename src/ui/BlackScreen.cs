using Godot;
using System;

public partial class BlackScreen : ColorRect
{
	public override async void _Ready()
	{
		Color = new Color(0, 0, 0, 1);

		await ToSignal(GetTree().CreateTimer(1.0), "timeout");

		Tween tween = CreateTween();

		tween.TweenProperty(
			this,
			"color:a",
			0.0f,
			1.5f
		);
		await ToSignal(tween, "finished");
		QueueFree();
	}

}
