using Godot;
using System;
using System.Threading.Tasks;

public partial class FadeText : Label
{
	public async Task Setup(string _text)
	{
		Show();
		Text = _text;
		Modulate = new Color(1, 1, 1, 0);

		Tween tween = CreateTween();

		tween.TweenProperty(this, "modulate:a", 1.0f, 2.0f);

		tween.TweenInterval(2.0);

		tween.TweenProperty(this, "modulate:a", 0.0f, 1.0f);
		tween.TweenInterval(0.5f);

		await ToSignal(tween, Tween.SignalName.Finished);
		Hide();
	}
}
