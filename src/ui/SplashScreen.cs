using Godot;
using System;

public partial class SplashScreen : VideoStreamPlayer
{

	[Export]
	public PackedScene BeginningScene;

	public override void _Ready()
	{
		Finished += () =>
		{
			GetTree().ChangeSceneToPacked(BeginningScene);
		};
	}

	public override void _Process(double delta)
	{
	}
	private void OnFinished()
	{
	}
}
