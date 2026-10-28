using Godot;
using System;

public partial class SplashScreen : VideoStreamPlayer
{

	[Export]
	public PackedScene? BeginningScene;

	public override void _Ready()
	{
		Finished += () =>
		{
			if (BeginningScene != null)
				GetTree().ChangeSceneToPacked(BeginningScene);
		};
	}
}
