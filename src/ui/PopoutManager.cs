using Godot;
using System;
using System.Collections.Generic;

public partial class PopoutManager : Control
{
	[Export] PackedScene? popoutPrefab;
	Godot.Collections.Array<Control> currentPopouts = new Godot.Collections.Array<Control>();

	public void SpawnPopout(string _title, string _text)
	{
		MouseFilter = MouseFilterEnum.Stop;
		Control _currentPopout = popoutPrefab!.Instantiate<Control>();
		AddChild(_currentPopout);
		_currentPopout!.SetAnchorsPreset(Control.LayoutPreset.Center);

		Label label = _currentPopout.GetNode<Label>("Label");
		Label text = _currentPopout.GetNode<Label>("Text");
		Button button = _currentPopout.GetNode<Button>("Button");

		label.Text = _title;
		text.Text = _text;

		button.Pressed += ClosePopout;

		currentPopouts!.Add(_currentPopout);

		GetTree().Paused = true;
	}

	public void ClosePopout()
	{
		currentPopouts![currentPopouts.Count - 1]?.QueueFree();
		currentPopouts.RemoveAt(currentPopouts.Count - 1);

		if (currentPopouts.Count == 0)
		{
			GetTree().Paused = false;
			MouseFilter = MouseFilterEnum.Ignore;
		}
	}
}
