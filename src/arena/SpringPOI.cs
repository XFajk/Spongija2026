using Godot;
using System;

public partial class SpringPOI : PointOfInterest
{
	[Export] Texture2D? icon;

	public int Amount;
	[Export] public int MaxAmount;
	[Export] public float RestoringSpeed;
	float restoringSpeed;

	[Export]
	public PackedScene? FloatingTextScene { get; set; }

	[Export]
	public Camera3D? Camera { get; set; }

	[Export]
	public Control? Container { get; set; }

	public override void _Ready()
	{
		base._Ready();
		Amount = MaxAmount;
		restoringSpeed = RestoringSpeed;
	}


	public override void _Process(double delta)
	{
		if (Amount < MaxAmount)
		{
			restoringSpeed -= 1 * (float)delta;

			if (restoringSpeed <= 0)
			{
				Amount++;
				restoringSpeed = RestoringSpeed;
			}
		}
	}


	public override void Access()
	{
		base.Access();
		if (Amount > 0)
		{
			_character?.GetNode<Inventory>("Inventory").AddMana((uint)Mathf.Floor(Amount));
			ShowFloatingText(GlobalPosition, Amount);
			Amount = 0;
		}

		_character!.poi = null;
		UnFocus();
	}

	public void ShowFloatingText(Vector3 worldPosition, float amount)
	{
		var popup = FloatingTextScene?.Instantiate<FloatingText>();

		Container?.AddChild(popup);

		Vector2 screenPosition = base.GetScreenPosition(Camera!, worldPosition, Container!);

		popup!.Text = amount >= 0 ? $"+{amount}" : ((uint)Mathf.Floor(Amount)).ToString();
		popup!.Position = screenPosition;
		popup!.GetNode<TextureRect>("TextureRect").Show();
		popup!.GetNode<TextureRect>("TextureRect").Texture = icon;
	}
}
