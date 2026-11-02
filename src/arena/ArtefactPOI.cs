using Godot;
using System;

public partial class ArtefactPOI : PointOfInterest
{
	[Export] Artefact? Artefact;
	[Export] public float RotationSpeed = 1.0f;

	[Export]
	public PackedScene? FloatingTextScene { get; set; }

	[Export]
	public Camera3D? Camera { get; set; }

	[Export]
	public Control? Container { get; set; }

	public override void _Process(double delta)
	{
		_mesh!.GlobalRotate(Vector3.Up, RotationSpeed * (float)delta);
		if (!MeshOutlineHighlight)
		{
			_mesh!.GlobalRotate(Vector3.Up, RotationSpeed * (float)delta);
		}
	}


	public override void Access()
	{
		if (base._destroyed)
			return;

		base.Access();
		ShowFloatingText(GlobalPosition);
		_character?.GetNode<Inventory>("Inventory").AddArtefact(Artefact!);

		_character!.poi = null;
		UnFocus();
		base.Destroy();
	}

	public void ShowFloatingText(Vector3 worldPosition)
	{
		var popup = FloatingTextScene?.Instantiate<FloatingText>();

		Container?.AddChild(popup);

		// Prevod 3D pozície na 2D pozíciu obrazovky
		Vector2 screenPosition = base.GetScreenPosition(Camera!, worldPosition, Container!);

		popup!.Text = "+ " + Artefact?.Name;
		popup!.Position = screenPosition;
	}
}
