using Godot;
using System;

public partial class PopoutManager : Control
{
	[Export] AdventureCamera agent;
	[Export] PackedScene popoutPrefab;
	Control currentPopout;

	public void SpawnPopout(string _title, string _text)
	{
		currentPopout = popoutPrefab.Instantiate<Control>();
		AddChild(currentPopout);
		currentPopout.SetAnchorsPreset(Control.LayoutPreset.Center);
		agent.agent.canMove = false;

		Label label = currentPopout.GetNode<Label>("Label");
		Label text = currentPopout.GetNode<Label>("Text");
		Button button = currentPopout.GetNode<Button>("Button");

		label.Text = _title;
		text.Text = _text;

		button.Pressed += ClosePopout;

		GetTree().Paused = true;
	}

	public void ClosePopout()
	{
		currentPopout.QueueFree();
		currentPopout = null;
		agent.agent.canMove = true;

		GetTree().Paused = false;
	}
}
