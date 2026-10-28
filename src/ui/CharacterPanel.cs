using Godot;
using System;

public partial class CharacterPanel : Panel
{
	[Export] Control inventoryPanel;
	[Export] Control magicPanel;
	[Export] public float[] inventoryYPos;
	[Export] public float[] magicYPos;
	[Export] public float movingSpeed;

	public override void _Ready()
	{
		this.VisibilityChanged += OnVisible;
		Button inventoryButton = GetNode<Button>("/root/Map/CanvasLayer/Canvas/Character/Categories/Inventory");
		inventoryButton.Pressed += ClickInventory;
		Button magicButton = GetNode<Button>("/root/Map/CanvasLayer/Canvas/Character/Categories/Magic");
		magicButton.Pressed += ClickMagic;
	}

	public void OnVisible()
	{
		if (!Visible)
		{
			ClickInventory();
		}
	}

	public void ClickInventory()
	{
		Tween tweenI = CreateTween();

		tweenI.SetTrans(Tween.TransitionType.Sine);
		tweenI.SetEase(Tween.EaseType.InOut);

		tweenI.TweenProperty(
			inventoryPanel,
			"position",
			new Vector2(inventoryPanel.Position.X, inventoryYPos[0]),
			movingSpeed
		);

		Tween tweenM = CreateTween();

		tweenM.SetTrans(Tween.TransitionType.Sine);
		tweenM.SetEase(Tween.EaseType.InOut);

		tweenM.TweenProperty(
			magicPanel,
			"position",
			new Vector2(magicPanel.Position.X, magicYPos[0]),
			movingSpeed
		);
	}

	public void ClickMagic()
	{
		Tween tweenI = CreateTween();

		tweenI.SetTrans(Tween.TransitionType.Sine);
		tweenI.SetEase(Tween.EaseType.InOut);

		tweenI.TweenProperty(
			inventoryPanel,
			"position",
			new Vector2(inventoryPanel.Position.X, inventoryYPos[1]),
			movingSpeed
		);

		Tween tweenM = CreateTween();

		tweenM.SetTrans(Tween.TransitionType.Sine);
		tweenM.SetEase(Tween.EaseType.InOut);

		tweenM.TweenProperty(
			magicPanel,
			"position",
			new Vector2(magicPanel.Position.X, magicYPos[1]),
			movingSpeed
		);
	}
}
