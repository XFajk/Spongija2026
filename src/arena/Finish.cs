using Godot;
using System;

public partial class Finish : Area3D
{
	[Export]
	public Node3D? _Character { get; set; }

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node3D body)
	{
		if (body == _Character)
		{
			OutroScene map = GD.Load<PackedScene>("uid://luthgvdm1kug").Instantiate<OutroScene>();
			GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToNode, map);
		}
	}
}
