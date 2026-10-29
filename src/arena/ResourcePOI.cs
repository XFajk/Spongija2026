using Godot;
using System;

public partial class ResourcePOI : PointOfInterest
{
	[Export] public TypeOfResource type;
	[Export] public int Amount;
	 [Export]
	public PackedScene? FloatingTextScene { get; set; }

	[Export]
	public Camera3D? Camera { get; set; }

	[Export]
	public Control? Container { get; set; }

	public override void Access()
	{
		base.Access();
		_character?.GetNode<Inventory>("Inventory").AddResource(type, Amount);

		ShowFloatingText(GlobalPosition, Amount);

		_character!.poi = null;
		UnFocus();
		QueueFree();
	}

	public void ShowFloatingText(Vector3 worldPosition, int amount)
	{
		var popup = FloatingTextScene?.Instantiate<FloatingText>();

		Container?.AddChild(popup);

		// Prevod 3D pozície na 2D pozíciu obrazovky
		Vector2 screenPosition = Camera!.UnprojectPosition(worldPosition);

		popup!.Text = amount >= 0 ? $"+{amount}" : amount.ToString();
		popup!.Position = screenPosition;
	}
}

public enum TypeOfResource
{
	horses,
	silk,
	iron
}
