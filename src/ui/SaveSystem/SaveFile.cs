using Godot;
using System;

public partial class SaveFile : Button
{
	public bool _save;
	public int index;
	public SaveBrowser? _browser;

	public override void _Ready()
	{
		Pressed += Click;
	}

	void Click()
	{
		_browser!.Select(index, _save);

		if (_browser.manager == null)
		{
			Node3D map = GD.Load<PackedScene>("uid://cfsvco3a36qfu").Instantiate<Node3D>();
			GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToNode, map);
		}
	}
}
