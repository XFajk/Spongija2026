using Godot;
using System;

public partial class FloatingText : Label
{
	 public override void _Ready()
	{
		var tween = CreateTween();
		tween.SetParallel(true);

		tween.TweenProperty(
			this, "position:y",
			Position.Y - 2, 1
		);

		tween.TweenProperty(
			this, "modulate:a",
			0.0f, 1
		);

		tween.Chain().TweenCallback(
			Callable.From(QueueFree)
		);
	}
}
