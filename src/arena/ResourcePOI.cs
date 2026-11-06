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

	[Export] Texture2D? icon;

	public override void Access()
	{
		if (base._destroyed)
			return;

		base.Access();
		_character?.GetNode<Inventory>("Inventory").AddResource(type, Amount);

		ShowFloatingText(GlobalPosition, Amount);

		_character!.poi = null;
		UnFocus();
		base.Destroy();
	}

	public void ShowFloatingText(Vector3 worldPosition, int amount)
	{
		var popup = FloatingTextScene?.Instantiate<FloatingText>();

		Container?.AddChild(popup);

		// Prevod 3D pozície na 2D pozíciu obrazovky
		Vector2 screenPosition = base.GetScreenPosition(Camera!, worldPosition, Container!);

		popup!.Text = amount >= 0 ? $"+{amount}" : amount.ToString();
		popup!.Position = screenPosition;
		popup!.GetNode<TextureRect>("TextureRect").Show();
		popup!.GetNode<TextureRect>("TextureRect").Texture = icon;
	}
}

public enum TypeOfResource
{
	horses,
	silk,
	iron
}
