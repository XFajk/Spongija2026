using Godot;
using System;

public partial class CharacterPanel : Panel
{
	Inventory? inventory;
	[Export] Control? inventoryPanel;
	[Export] Control? magicPanel;
	[Export] public float[]? inventoryYPos;
	[Export] public float[]? magicYPos;
	[Export] public float movingSpeed;

	[Export] Label? horsesText;
	[Export] Label? silkText;
	[Export] Label? ironText;

	[Export] InventoryUnitSlot[]? icons;
	[Export] Control? splitWindow;
	[Export] HSlider? splitSlider;
	[Export] Label? splitNumber;
	[Export] Label? splitUnit;
	[Export] public SwapPanel? swapWindow;

	[Export] TextureRect[]? resourceCombiningIcons;

	public override void _Ready()
	{
		this.VisibilityChanged += OnVisible;
		Button inventoryButton = GetNode<Button>("/root/Map/CanvasLayer/Canvas/Character/Categories/Inventory/Button");
		inventoryButton.Pressed += ClickInventory;
		Button magicButton = GetNode<Button>("/root/Map/CanvasLayer/Canvas/Character/Categories/Magic/Button");
		magicButton.Pressed += ClickMagic;
		Button splitButton = GetNode<Button>("/root/Map/CanvasLayer/Canvas/Split/Confirm");
		splitButton.Pressed += Confirm;
		splitSlider!.ValueChanged += ChangeValueText;

		//Resources
		Button horseB = GetNode<Button>("/root/Map/CanvasLayer/Canvas/Character/Viewport/Inventory/Resources/Horses/Button");
		Button silkB = GetNode<Button>("/root/Map/CanvasLayer/Canvas/Character/Viewport/Inventory/Resources/Silk/Button");
		Button ironB = GetNode<Button>("/root/Map/CanvasLayer/Canvas/Character/Viewport/Inventory/Resources/Iron/Button");
		horseB.Pressed += ClickHorse;
		silkB.Pressed += ClickSilk;
		ironB.Pressed += ClickIron;
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
			new Vector2(inventoryPanel!.Position.X, inventoryYPos![0]),
			movingSpeed
		);

		Tween tweenM = CreateTween();

		tweenM.SetTrans(Tween.TransitionType.Sine);
		tweenM.SetEase(Tween.EaseType.InOut);

		tweenM.TweenProperty(
			magicPanel,
			"position",
			new Vector2(magicPanel!.Position.X, magicYPos![0]),
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
			new Vector2(inventoryPanel!.Position.X, inventoryYPos![1]),
			movingSpeed
		);

		Tween tweenM = CreateTween();

		tweenM.SetTrans(Tween.TransitionType.Sine);
		tweenM.SetEase(Tween.EaseType.InOut);

		tweenM.TweenProperty(
			magicPanel,
			"position",
			new Vector2(magicPanel!.Position.X, magicYPos![1]),
			movingSpeed
		);
	}

	public void UpdateUI(Inventory _inventory)
	{
		inventory = _inventory;
		horsesText!.Text = inventory.horses.ToString();
		silkText!.Text = inventory.silk.ToString();
		ironText!.Text = inventory.iron.ToString();

		for (int i = 0; i < inventory?.units?.Length; i++)
		{
			if (inventory.units[i] != null)
			{
				icons?[i].Setup((int)inventory.units[i].Quantity, inventory.units[i].Unit!.texture!);
			}
			else
			{
				icons?[i].Reset();
			}
		}
	}

	public void Highlight(int index)
	{
		icons?[index].Select();
	}

	public void Unhighlight(int index)
	{
		icons?[index].Deselect();
	}

	public void SetupSlit(int maxAmount, string unitName)
	{
		splitWindow?.Show();
		splitSlider!.MaxValue = maxAmount;
		splitSlider!.MinValue = 1;
		splitUnit!.Text = unitName;
	}

	public void Confirm()
	{
		splitWindow?.Hide();
		inventory?.Split((int)splitSlider!.Value);
	}

	void ChangeValueText(double value)
	{
		splitNumber!.Text = value.ToString();
	}

	public void ResetSplitting()
	{
		for (int i = 0; i < icons?.Length; i++)
		{
			Unhighlight(i);
		}

		inventory?.CancelSplitting();
		splitWindow?.Hide();
	}

	public void ClickHorse()
	{
		ClickResource(TypeOfResource.horses);
	}

	public void ClickSilk()
	{
		ClickResource(TypeOfResource.silk);
	}

	public void ClickIron()
	{
		ClickResource(TypeOfResource.iron);
	}

	public void ClickResource(TypeOfResource type)
	{
		int amount = 0;
		int index = 0;
		string resource = "";

		switch(type)
		{
			case TypeOfResource.horses:
				amount = inventory!.horses;
				index = 0;
				resource = "horse";
				break;
			case TypeOfResource.silk:
				amount = inventory!.silk;
				index = 1;
				resource = "silk";
				break;
			case TypeOfResource.iron:
				amount = inventory!.iron;
				index = 2;
				resource = "iron";
				break;
			default:
				break;
		}

		if (amount > 0)
		{
			ResetSplitting();

			for (int i = 0; i < resourceCombiningIcons?.Length; i++)
			{
				resourceCombiningIcons[i].Hide();
			}

			resourceCombiningIcons?[index].Show();
			inventory?.StartCombining(resource);
		}
		else
		{
			ResetCombining();
		}
	}

	public void SwapWarning()
	{
		swapWindow?.Show();
		swapWindow?.SetupWindow(inventory!);
		GetTree().Paused = true;
	}
	
	public void ResetCombining()
	{
		for (int i = 0; i < resourceCombiningIcons?.Length; i++)
		{
			resourceCombiningIcons[i].Hide();
		}

		inventory?.CancelCombining();
	}
}
