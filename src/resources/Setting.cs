using Godot;
using System;

[GlobalClass]
public partial class Setting : Resource
{
	[Export]
	public string name = "";

	[Export] public PackedScene? SceneTemplate;
	[Export] public string? function;
}
