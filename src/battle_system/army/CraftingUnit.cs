using Godot;
using System;

[GlobalClass]
public partial class CraftingUnit : Resource
{
	[Export]
	public Unit? Unit { get; set; } = null;

	[Export]
	public TypeOfResource Resource;

	[Export]
	public Unit? FinalUnit { get; set; } = null;
}
