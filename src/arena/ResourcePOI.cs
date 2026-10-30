using Godot;
using System;

public partial class ResourcePOI : PointOfInterest
{
	[Export] public TypeOfResource type;
	[Export] public int Amount;

	public override void Access()
	{
		base.Access();
		_character?.GetNode<Inventory>("Inventory").AddResource(type, Amount);

		_character!.poi = null;
		UnFocus();
		QueueFree();
	}
}

public enum TypeOfResource
{
	horses,
	silk,
	iron
}
