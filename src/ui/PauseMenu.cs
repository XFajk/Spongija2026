using Godot;
using System;

public partial class PauseMenu : Container
{
	[Export] AdventureCamera? camera;

	public void Setup()
	{
		camera?.HideCustomCursor();
	}
}
