using Godot;
using System;

public partial class SpellPOI : PointOfInterest
{
	[Export] SpellAbility? Spell;

	[Export]
	public PackedScene? FloatingTextScene { get; set; }

	[Export]
	public Camera3D? Camera { get; set; }

	[Export]
	public Control? Container { get; set; }

	public override void Access()
	{
		if (base._destroyed)
			return;

		base.Access();
		ShowFloatingText(GlobalPosition);
		_character?.GetNode<Inventory>("Inventory").AddSpell(Spell!);

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

		popup!.Text = "+ " + Spell?.Name;
		popup!.Position = screenPosition;
	}
}
