using Godot;
using System;

public partial class ArtefactPOI : PointOfInterest
{
	[Export] Artefact? Artefact;

	[Export]
	public PackedScene? FloatingTextScene { get; set; }

	[Export]
	public Camera3D? Camera { get; set; }

	[Export]
	public Control? Container { get; set; }

	public override void Access()
	{
		base.Access();
		ShowFloatingText(GlobalPosition);
		_character?.GetNode<Inventory>("Inventory").AddArtefact(Artefact!);

		_character!.poi = null;
		UnFocus();
		QueueFree();
	}

	public void ShowFloatingText(Vector3 worldPosition)
	{
		var popup = FloatingTextScene?.Instantiate<FloatingText>();

		Container?.AddChild(popup);

		// Prevod 3D pozície na 2D pozíciu obrazovky
		Vector2 screenPosition = Camera!.UnprojectPosition(worldPosition);

		popup!.Text = "+ " + Artefact?.Name;
		popup!.Position = screenPosition;
	}
}
