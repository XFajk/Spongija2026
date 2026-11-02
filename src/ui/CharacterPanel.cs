using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
	FightPOI? _currentFightPOI;

	[Export]
	public PackedScene? FloatingTextScene { get; set; }

	[Export]
	public Control? Container { get; set; }

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
		Button fightB = GetNode<Button>("/root/Map/CanvasLayer/Canvas/SetupFight/Fight");
		fightB.Pressed += camera!.Agent!.EnterFight;
		Button defectB = GetNode<Button>("/root/Map/CanvasLayer/Canvas/SetupFight/Defection");
		defectB.Pressed += DefectUnits;
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

		if (!TutorialChecklist.magicExplained)
		{
			tweenM.Finished += () =>
			{
				GetNode<Tutorial>("/root/Map/Tutorial").ExplainMagic();
			};
		}
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
				equippedArtefacts?[i].Setup(inventory.EquippedArtefacts[i]);
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
				artefacts[i].Setup(inventory.Artefacts[i]);
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
				_prefab.Setup(_inventory.Artefacts[i]);
				artefacts?.Add(_prefab);
			}
		}

		artefactsEmpty!.Visible = _inventory?.Artefacts?.Count == 0;
	}

	public void GetReady(bool _surrender, FightPOI _poi)
	{
		camera?.Agent?.HidePanel();
		camera?.HideCustomCursor();
		swapWindow?.CancelSwapping();
		fightWindow?.Show();
		fightWindow!.GetNode<Control>("Defection").Visible = _surrender;
		_currentFightPOI = _poi;
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

	public void DefectUnits()
	{
		GetTree().Paused = false;
		fightWindow!.Hide();

		StackResource[] _units = new StackResource[camera!.Agent!.GetNode<BattleStarter>("BattleStarter").ComputerUnits!.Count];
		for (int i = 0; i < camera!.Agent!.GetNode<BattleStarter>("BattleStarter").ComputerUnits!.Count; i++)
		{
			_units[i] = camera!.Agent!.GetNode<BattleStarter>("BattleStarter").ComputerUnits![i];
		}
		
		ShowUnitsText(_units);
		_currentFightPOI!._Destroy();
		_currentFightPOI = null;

		inventory?.AddUnits(_units);
	}

	public async void ShowUnitsText(StackResource[] _units)
	{
		// No fight panel opens for a FightPOI that always recruits, so _currentFightPOI is null then
		await ShowFloatingText(_currentFightPOI?.GlobalPosition ?? Vector3.Zero, _units);
	}

	public async Task ShowFloatingText(Vector3 worldPosition, StackResource[] Units)
	{
		for (int i = 0; i < Units?.Length; i++)
		{
			var popup = FloatingTextScene?.Instantiate<FloatingText>();

			Container?.AddChild(popup);

			float X = camera!.GetViewport().GetVisibleRect().Size.X / 2;
			float Y = camera!.GetViewport().GetVisibleRect().Size.X / 2;

			popup!.Text = "+ " + Units[i]?.Quantity.ToString() + " " + Units[i]?.Unit?.Name;
			popup!.Position = new Vector2(X, Y);

			if (i == Units?.Length - 1)
			{
				await ToSignal(
					GetTree().CreateTimer(1f),
					SceneTreeTimer.SignalName.Timeout
				);
			}
			else
			{
				await ToSignal(
					GetTree().CreateTimer(0.5f),
					SceneTreeTimer.SignalName.Timeout
				);
			}
		}
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
