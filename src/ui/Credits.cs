using Godot;
using System;
using System.Threading.Tasks;

public partial class Credits : Control
{
	[Export]
	public float FadeDuration { get; set; } = 2.0f;

	[Export]
	public float PauseDuration { get; set; } = 1.0f;

	[Export]
	public float ScrollDuration { get; set; } = 20.0f;

	private Control? viewport;
	private Control? creditsImage;

	private ColorRect? maskName;
	private ColorRect? maskStudio;

	public override async void _Ready()
	{
		viewport = GetNode<Control>("Viewport");
		creditsImage = GetNode<Control>("Viewport/Credits");

		maskName = GetNode<ColorRect>(
			"Viewport/Name");

		maskStudio = GetNode<ColorRect>(
			"Viewport/Studio");

		maskName.Modulate = Colors.White;
		maskStudio.Modulate = Colors.White;

		await Pause(PauseDuration);

		await ShowPart(maskName);

		await Pause(PauseDuration);

		await ShowPart(maskStudio);

		await Pause(PauseDuration);


		await Scroll();

	   	Control menu = GD.Load<PackedScene>("uid://1cwqotqa3spu").Instantiate<Control>();
		GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToNode, menu);
	}

	private async Task ShowPart(ColorRect mask)
	{
		Tween tween = CreateTween();

		tween.TweenProperty(
			mask,
			"modulate:a",
			0.0f,
			FadeDuration
		);

		await ToSignal(tween, Tween.SignalName.Finished);
	}

	private async Task Scroll()
	{
		await ToSignal(
			GetTree(),
			SceneTree.SignalName.ProcessFrame
		);

		float startY = creditsImage!.Position.Y;

		float endY = -creditsImage.Size.Y;

		Tween tween = CreateTween();

		tween.TweenProperty(
			creditsImage,
			"position:y",
			endY,
			ScrollDuration
		).SetTrans(Tween.TransitionType.Linear);

		await ToSignal(tween, Tween.SignalName.Finished);
	}

	private async Task Pause(float seconds)
	{
		await ToSignal(
			GetTree().CreateTimer(seconds),
			Timer.SignalName.Timeout
		);
	}
}
