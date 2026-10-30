using Godot;
using System;
using System.Collections.Generic;

public partial class CharacterPanel : Panel
{
	[Export] AdventureCamera? camera;
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

	[Export] ArtefactSlot[]? equippedArtefacts;
	List<ArtefactSlot>? artefacts = new List<ArtefactSlot>();
	[Export] PackedScene? artefactUIPrefab;
	[Export] GridContainer? artefactUIContainer;
	[Export] Label? artefactsEmpty;

	[Export] Control? fightWindow;

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

		//Spells
		Button spellB = GetNode<Button>("/root/Map/CanvasLayer/Canvas/Character/Viewport/Magic/Spells");
		SpellDisplay _display = GetNode<SpellDisplay>("/root/Map/CanvasLayer/Canvas/Spells");
		spellB!.Pressed += _display.OpenBook;

		//Fight
		Button fightB = GetNode<Button>("/root/Map/CanvasLayer/Canvas/SetupFight/Fight/Button");
		fightB.Pressed += camera!.Agent!.EnterFight;
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

		for (int i = 0; i < inventory?.Units?.Length; i++)
		{
			if (inventory.Units[i] != null)
			{
				icons?[i].Setup((int)inventory.Units[i].Quantity, inventory.Units[i].Unit!.texture!);
			}
			else
			{
				icons?[i].Reset();
			}
		}

		for (int i = 0; i < inventory?.EquippedArtefacts?.Length; i++)
		{
			if (inventory.EquippedArtefacts[i] != null)
			{
				equippedArtefacts?[i].Setup(_inventory?.EquippedArtefacts?[i].Icon!, _inventory!.EquippedArtefacts![i].BodyPart!);
			}
			else
			{
				equippedArtefacts?[i].Reset();
			}
		}

		if (inventory!.Artefacts.Count <= artefacts!.Count)
		{
			for (int i = inventory!.Artefacts.Count; i < artefacts?.Count; i++)
			{
				artefacts[i].QueueFree();
			}

			List<ArtefactSlot> newList = new List<ArtefactSlot>();

			for (int i = 0; i < inventory!.Artefacts.Count; i++)
			{
				artefacts![i].index = i;
				artefacts[i].Setup(inventory.Artefacts[i].Icon!, inventory.Artefacts[i].BodyPart);
				newList.Add(artefacts[i]);
			}

			artefacts = newList;
		}
		else
		{
			for (int i = artefacts!.Count; i < _inventory?.Artefacts?.Count; i++)
			{
				ArtefactSlot _prefab = artefactUIPrefab!.Instantiate<ArtefactSlot>();
				artefactUIContainer?.AddChild(_prefab);
				_prefab.index = i;
				_prefab.Setup(_inventory.Artefacts[i].Icon!, _inventory.Artefacts[i].BodyPart);
				artefacts?.Add(_prefab);
			}
		}

		artefactsEmpty!.Visible = _inventory?.Artefacts?.Count == 0;
	}

	public void GetReady()
	{
		camera?.Agent?.HidePanel();
		swapWindow?.CancelSwapping();
		fightWindow?.Show();
		GetTree().Paused = true;
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
			ResetEquipping();

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

	public void UnhighlightArtefacts()
	{
		for (int i = 0; i < artefacts?.Count; i++)
		{
			artefacts[i].Deselect();
		}

		for (int i = 0; i < equippedArtefacts?.Length; i++)
		{
			equippedArtefacts[i].Deselect();
		}
	}

	public void ResetEquipping()
	{
		UnhighlightArtefacts();
		inventory?.CancelEquipping();
	}

	public override void _Input(InputEvent @event)
	{
		if (!Visible)
			return;
		if (@event is not InputEventMouseButton { Pressed: true } click)
			return;
		if (GetGlobalRect().HasPoint(click.Position) || splitWindow!.GetGlobalRect().HasPoint(click.Position))
			return;

		GetViewport().SetInputAsHandled();
		// The camera polls the button instead of reading events, so it has to be told this click is taken
		camera?.IgnoreAdventureCurrentClick();
		camera?.IgnoreCurrentClick();
		camera?.Agent?.HidePanel();
	}
}
