using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;

public partial class Inventory : Node, ISavable
{
	[Export] public string SaveId { get; set; } = "inventory";

	public AdventureCharacter? Character;

	[Export] public StackResource[]? Units;
	[Export] public SpellAbility[]? Spells;
	public Godot.Collections.Array<Artefact> Artefacts = new Godot.Collections.Array<Artefact>();
	[Export] public Artefact[]? EquippedArtefacts;
	[Export] public CraftingUnit[]? recipes;
	public StackResource[]? unitsToSwap;

	[Export] public int horses;
	[Export] public int silk;
	[Export] public int iron;
	public uint Mana;

	// The hero's own stats, before artefacts (CreateHero)
	[ExportGroup("Hero")]
	[Export] public uint HeroStrength = 2;
	[Export] public uint HeroDefense = 2;
	[Export] public uint HeroKnowledge = 5;
	[ExportGroup("")]

	// The hero's max mana with the equipped artefacts (Knowledge and the mana pouch)
	public uint MaxMana => CreateHero().MaxMana;

	public Unit? unitSplitting;
	public string resourceCombining = "";

	int unitToSplit;
	int placeToSplit;

	public StackResource? unitSwapping;
	public bool swappingMyUnit;
	int swappingIndex;

	public Artefact? artefactEquipping;
	public bool artefactEquipped;
	public int artefactIndex;

	public async override void _Ready()
	{
		Character = GetParent<AdventureCharacter>();

		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		UpdateUI();
	}

	public void UpdateUI()
	{
		Character?.inventoryPanel?.UpdateUI(this);
	}

	public void AddResource(TypeOfResource type, int _amount)
	{
		switch(type)
		{
			case TypeOfResource.horses:
				horses += _amount;
				break;
			case TypeOfResource.silk:
				silk += _amount;
				break;
			case TypeOfResource.iron:
				iron += _amount;
				break;
			default:
				break;
		}

		UpdateUI();
	}

	public void RemoveResource(TypeOfResource type, int _amount)
	{
		switch(type)
		{
			case TypeOfResource.horses:
				horses -= _amount;
				break;
			case TypeOfResource.silk:
				silk -= _amount;
				break;
			case TypeOfResource.iron:
				iron -= _amount;
				break;
			default:
				break;
		}

		UpdateUI();
	}

	// The hero the battles get: the hero stats, the equipped artefacts, the spells and the mana
	public Hero CreateHero()
	{
		var hero = new Hero { Strength = HeroStrength, Defense = HeroDefense, Knowledge = HeroKnowledge };
		// Before the mana, which is capped by the artefacts' max mana
		hero.Artefacts.AddRange(EquippedArtefacts?.Where(a => a != null) ?? []);
		foreach (SpellAbility spell in Spells ?? [])
			if (spell != null)
				hero.LearnSpell(spell);
		hero.Mana = Mana;
		return hero;
	}

	public void AddMana(uint _mana)
	{
		Mana += _mana;
		if (Mana > MaxMana)
		{
			Mana = MaxMana;
		}
	}

	public void AddUnits(StackResource[] _units)
	{
		unitsToSwap = [];
		for (int i = 0; i < _units.Length; i++)
		{
			bool inInventory = false;
			int indexInInventory = 0;

			for (int y = 0; y < Units?.Length; y++)
			{
				if (Units[y] != null)
				{
					if (Units[y].Unit == _units[i].Unit)
					{
						inInventory = true;
						indexInInventory = y;
						break;
					}
				}
			}

			if (inInventory)
			{
				Units![indexInInventory].Quantity += _units[i].Quantity;
				UpdateUI();
			}
			else
			{
				bool emptyPlace = false;
				int emptyIndex = 0;
				
				for (int y = 0; y < Units?.Length; y++)
				{
					if (Units[y] == null)
					{
						emptyPlace = true;
						emptyIndex = y;
						break;
					}
				}

				StackResource newUnit = new StackResource();
				newUnit.Unit = _units[i].Unit;
				newUnit.Quantity = _units[i].Quantity;

				if (emptyPlace)
				{
					Units![emptyIndex] = newUnit;
					UpdateUI();
				}
				else
				{

					unitsToSwap = unitsToSwap.Append(newUnit).ToArray();
				}
			}
		}

		if (unitsToSwap.Length > 0)
		{
			Character?.HidePanel();
			Character?.inventoryPanel?.SwapWarning();
		}
	}

	public void AddArtefact(Artefact artefact)
	{
		Artefacts!.Add(artefact);
		UpdateUI();
	}

	public void AddSpell(SpellAbility _spell)
	{
		SpellAbility _newSpell = _spell;
		Spells = Spells!.Append(_newSpell).ToArray();
		UpdateUI();
	}

	public void StartSplitting(int index)
	{
		Character?.inventoryPanel?.ResetCombining();
		Character?.inventoryPanel?.ResetEquipping();
		Character?.inventoryPanel?.Unhighlight(unitToSplit);
		Character?.inventoryPanel?.Highlight(index);
		unitToSplit = index;
		unitSplitting = Units![index].Unit;
	}

	public void SetupSplit(int index)
	{
		if (index != unitToSplit)
		{
			placeToSplit = index;
			Character?.inventoryPanel?.SetupSlit((int)Units![unitToSplit].Quantity, Units[unitToSplit].Unit!.Name);
		}
	}

	public void Split(int amount)
	{
		if (Units![placeToSplit] == null)
		{
			StackResource newUnit = new StackResource();
			newUnit.Unit = unitSplitting;
			newUnit.Quantity = (uint)amount;
			Units[placeToSplit] = newUnit;
			Units[unitToSplit].Quantity -= (uint)amount;
		}
		else
		{
			Units[placeToSplit].Quantity += (uint)amount;
			Units[unitToSplit].Quantity -= (uint)amount;
		}

		if (Units[unitToSplit].Quantity == 0)
		{
			Units[unitToSplit] = null!;
		}

		Character?.inventoryPanel?.Unhighlight(unitToSplit);

		unitToSplit = 0;
		unitSplitting = null;
		placeToSplit = 0;
		UpdateUI();
	}

	public void CancelSplitting()
	{
		unitToSplit = 0;
		unitSplitting = null;
		placeToSplit = 0;
		UpdateUI();
	}

	public void StartCombining(string _resource)
	{
		resourceCombining = _resource;
	}

	public void Combine(int index)
	{
		int resourceAmount = 0;
		TypeOfResource type;
		switch(resourceCombining)
		{
			case "horse":
				resourceAmount = horses;
				type = TypeOfResource.horses;
				break;
			case "iron":
				resourceAmount = iron;
				type = TypeOfResource.iron;
				break;
			case "silk":
				resourceAmount = silk;
				type = TypeOfResource.silk;
				break;
			default:
				type = TypeOfResource.horses;
				break;
		}

		CraftingUnit _recipe = new CraftingUnit();
		if (Units?[index] != null)
		{
			_recipe.Unit = Units?[index].Unit;
			_recipe.Resource = type;
		}

		foreach (CraftingUnit recipe in recipes!)
		{
			if (_recipe.Unit == recipe.Unit && _recipe.Resource == recipe.Resource)
			{
				_recipe.FinalUnit = recipe.FinalUnit;
			}
		}

		if (_recipe.FinalUnit != null)
		{
			bool inInventory = false;
			int indexInInventory = 0;
			for (int i = 0; i < Units?.Length; i++)
			{
				if (Units[i] != null)
				{
					if (Units[i].Unit == _recipe.FinalUnit)
					{
						inInventory = true;
						indexInInventory = i;
						break;
					}
				}
			}

			if (inInventory)
			{
				if (resourceAmount >= Units?[index].Quantity)
				{
					Units![indexInInventory].Quantity += (uint)Units![index].Quantity;
					RemoveResource(type, (int)Units![index].Quantity);
					Units![index] = null!;
				}
				else
				{
					Units![indexInInventory].Quantity += (uint)resourceAmount;
					Units![index].Quantity -= (uint)resourceAmount;
					RemoveResource(type, resourceAmount);
				}
			}
			else
			{
				if (resourceAmount >= Units?[index].Quantity)
				{
					Units![index].Unit = _recipe.FinalUnit;
					RemoveResource(type, (int)Units![index].Quantity);
				}
				else
				{
					Units![index].Quantity -= (uint)resourceAmount;

					StackResource newUnit = new StackResource();
					newUnit.Unit = _recipe.FinalUnit;
					newUnit.Quantity = (uint)resourceAmount;

					bool hasSpace = false;
					for (int i = 0; i < Units.Length; i++)
					{
						if (Units[i] == null)
						{
							Units[i] = newUnit;
							hasSpace = true;
							break;
						}
					}

					if (!hasSpace)
					{
						Character?.HidePanel();
						unitsToSwap = new StackResource[1];
						unitsToSwap[0] = newUnit;
						Character?.inventoryPanel?.SwapWarning();
					}

					RemoveResource(type, resourceAmount);
				}
			}
		}

		Character?.inventoryPanel?.ResetCombining();
		UpdateUI();
	}

	public void CancelCombining()
	{
		resourceCombining = "";
	}

	public void StartSwapping(int index, bool myUnits)
	{
		Character?.inventoryPanel?.swapWindow?.CancelSwapping();

		if (myUnits)
		{
			if (Units![index] != null)
			{
				unitSwapping = Units[index];
			}
		}
		else
		{
			unitSwapping = unitsToSwap![index];
		}

		swappingIndex = index;
		swappingMyUnit = myUnits;
	}

	public void Swap(int index)
	{
		if (swappingMyUnit)
		{
			StackResource newUnit = new StackResource();
			newUnit.Unit = unitsToSwap?[index].Unit;
			newUnit.Quantity = unitsToSwap![index].Quantity;

			StackResource newBonusUnit = new StackResource();
			newBonusUnit.Unit = unitSwapping!.Unit;;
			newBonusUnit.Quantity = unitSwapping.Quantity;

			Units![swappingIndex] = newUnit;
			unitsToSwap![index] = newBonusUnit;
		}
		else
		{
			StackResource newUnit = new StackResource();
			newUnit.Unit = unitSwapping!.Unit;
			newUnit.Quantity = unitSwapping!.Quantity;

			StackResource newBonusUnit = new StackResource();
			newBonusUnit.Unit = Units?[index].Unit;
			newBonusUnit.Quantity = Units![index].Quantity;

			Units![index] = newUnit;
			unitsToSwap![swappingIndex] = newBonusUnit;
		}

		Character?.inventoryPanel?.swapWindow?.UpdateUI(this);
		CancelSwapping();
	}

	public void CancelSwapping()
	{
		unitSwapping = null;
		swappingIndex = 0;
		swappingMyUnit = false;
		Character?.inventoryPanel?.swapWindow?.CancelSwapping();
	}

	public void StartEquipping(int index, bool _equipped)
	{
		Character?.inventoryPanel?.ResetCombining();
		Character?.inventoryPanel?.ResetSplitting();
		CancelSwapping();

		artefactIndex = index;
		artefactEquipped = _equipped;
		Character?.inventoryPanel?.UnhighlightArtefacts();

		if (artefactEquipped)
		{
			artefactEquipping = EquippedArtefacts?[index];
		}
		else
		{
			artefactEquipping = Artefacts?[index];
		}
	}

	public void Equip(int index)
	{
		if (artefactEquipped)
		{
			Artefact newEquippedArtefact = Artefacts![index];
			Artefact newArtefact = artefactEquipping!;

			EquippedArtefacts![artefactIndex] = newEquippedArtefact;
			Artefacts![index] = newArtefact;
		}
		else
		{
			Artefact newEquippedArtefact = artefactEquipping!;
			Artefact newArtefact = EquippedArtefacts![index];

			EquippedArtefacts![index] = newEquippedArtefact;
			Artefacts![artefactIndex] = newArtefact;
		}

		// Taking off the mana pouch or a Knowledge artefact can lower the max mana
		Mana = Math.Min(Mana, MaxMana);

		for (int i = 0; i < Artefacts.Count; i++)
		{
			if (Artefacts[i] == null)
			{
				Artefacts!.RemoveAt(i);
			}
		}
		
		Character?.inventoryPanel?.ResetEquipping();
	}

	public void CancelEquipping()
	{
		artefactEquipping = null;
		artefactIndex = 0;
		artefactEquipped = false;
		UpdateUI();
	}

	//Save
	public Godot.Collections.Dictionary<string, Variant> Save()
	{
		var _units = new Godot.Collections.Array<Variant>();

		foreach (var _unit in Units!)
		{
			if (_unit == null)
			{
				_units.Add(default(Variant));
				continue;
			}

			_units.Add(new Godot.Collections.Dictionary
			{
				{ "path", _unit.ResourcePath },
				{ "unit", _unit.Unit?.ResourcePath ?? "" },
				{ "quantity", _unit.Quantity }
			});
		}

		var _spells = new Godot.Collections.Array<Variant>();

		foreach (var _spell in Spells!)
		{
			_spells.Add(new Godot.Collections.Dictionary
			{
				{ "path", _spell.ResourcePath },
			});
		}

		var _equipped = new Godot.Collections.Array<Variant>();

		foreach (var _equip in EquippedArtefacts!)
		{
			if (_equip == null)
			{
				_equipped.Add(default(Variant));
				continue;
			}

			_equipped.Add(new Godot.Collections.Dictionary
			{
				{ "path", _equip.ResourcePath },
			});
		}

		var owned = new Godot.Collections.Array<Variant>();

		foreach (var _arte in Artefacts!)
		{
			owned.Add(new Godot.Collections.Dictionary
			{
				{ "path", _arte.ResourcePath },
			});
		}

		return new()
		{
			{ "units", _units },
			{ "spells", _spells! },
			{ "artefacts", owned! },
			{ "artefacts_equipped", _equipped! },
			{ "mana", Mana },
			{ "horses", horses },
			{ "silk", silk },
			{ "iron", iron },
		};
	}

	public void Load(Godot.Collections.Dictionary data)
	{
		Mana = (uint)data["mana"];
		horses = (int)data["horses"];
		silk = (int)data["silk"];
		iron = (int)data["iron"];

		var _units = data["units"].AsGodotArray<Variant>();

		Units = new StackResource[_units.Count];

		for (int i = 0; i < _units.Count; i++)
		{
			if (_units[i].VariantType == Variant.Type.Nil)
				continue;

			var itemData = _units[i].AsGodotDictionary();

			var item = new StackResource();

			string path = itemData["unit"].AsString();

			if (path != "")
			{
				item.Unit = GD.Load<Unit>(path);
				item.Quantity = (uint)itemData["quantity"];
			}

			Units[i] = item;
		}

		var _spells = data["spells"].AsGodotArray<Variant>();

		Spells = new SpellAbility[_spells.Count];

		for (int i = 0; i < _spells.Count; i++)
		{
			var itemData = _spells[i].AsGodotDictionary();
			string path = itemData["path"].AsString();
			Spells[i] = GD.Load<SpellAbility>(path);
		}

		var _equipped = data["artefacts_equipped"].AsGodotArray<Variant>();

		EquippedArtefacts = new Artefact[_equipped.Count];

		for (int i = 0; i < _equipped.Count; i++)
		{
			if (_equipped[i].VariantType == Variant.Type.Nil)
				continue;

			var itemData = _equipped[i].AsGodotDictionary();
			string path = itemData["path"].AsString();
			EquippedArtefacts[i] = GD.Load<Artefact>(path);
		}

		var _artefacts = data["artefacts"].AsGodotArray<Variant>();

		Artefacts = new Godot.Collections.Array<Artefact>();

		for (int i = 0; i < _artefacts.Count; i++)
		{
			var itemData = _artefacts[i].AsGodotDictionary();
			string path = itemData["path"].AsString();
			Artefacts.Add(GD.Load<Artefact>(path));
		}
	}
}
