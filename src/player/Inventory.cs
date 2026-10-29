using Godot;
using System;
using System.Security.AccessControl;

public partial class Inventory : Node
{
	public AdventureCharacter? character;

	[Export] public StackResource[]? units;
	[Export] public SpellAbility[]? spells;
	[Export] public CraftingUnit[]? recipes;
	public StackResource[]? unitsToSwap;

	[Export] public int horses;
	[Export] public int silk;
	[Export] public int iron;
	
	public Unit? unitSplitting;
	public string resourceCombining = "";

	int unitToSplit;
	int placeToSplit;

	public StackResource? unitSwapping;
	public bool swappingMyUnit;
	int swappingIndex;

	public async override void _Ready()
	{
		character = GetParent<AdventureCharacter>();

		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		UpdateUI();
	}

	public void UpdateUI()
	{
		character?.inventoryPanel?.UpdateUI(this);
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

	public void StartSplitting(int index)
	{
		character?.inventoryPanel?.ResetCombining();
		character?.inventoryPanel?.Unhighlight(unitToSplit);
		character?.inventoryPanel?.Highlight(index);
		unitToSplit = index;
		unitSplitting = units![index].Unit;
	}

	public void SetupSplit(int index)
	{
		if (index != unitToSplit)
		{
			placeToSplit = index;
			character?.inventoryPanel?.SetupSlit((int)units![unitToSplit].Quantity, units[unitToSplit].Unit!.Name);
		}
	}

	public void Split(int amount)
	{
		if (units![placeToSplit] == null)
		{
			StackResource newUnit = new StackResource();
			newUnit.Unit = unitSplitting;
			newUnit.Quantity = (uint)amount;
			units[placeToSplit] = newUnit;
			units[unitToSplit].Quantity -= (uint)amount;
		}
		else
		{
			units[placeToSplit].Quantity += (uint)amount;
			units[unitToSplit].Quantity -= (uint)amount;
		}

		if (units[unitToSplit].Quantity == 0)
		{
			units[unitToSplit] = null!;
		}

		character?.inventoryPanel?.Unhighlight(unitToSplit);

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
		GD.Print(resourceCombining);
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
		if (units?[index] != null)
		{
			_recipe.Unit = units?[index].Unit;
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
			for (int i = 0; i < units?.Length; i++)
			{
				if (units[i] != null)
				{
					if (units[i].Unit == _recipe.FinalUnit)
					{
						inInventory = true;
						indexInInventory = i;
						break;
					}
				}
			}

			if (inInventory)
			{
				if (resourceAmount >= units?[index].Quantity)
				{
					units![indexInInventory].Quantity += (uint)units![index].Quantity;
					RemoveResource(type, (int)units![index].Quantity);
					units![index] = null!;
				}
				else
				{
					units![indexInInventory].Quantity += (uint)resourceAmount;
					units![index].Quantity -= (uint)resourceAmount;
					RemoveResource(type, resourceAmount);
				}
			}
			else
			{
				if (resourceAmount >= units?[index].Quantity)
				{
					units![index].Unit = _recipe.FinalUnit;
					RemoveResource(type, (int)units![index].Quantity);
				}
				else
				{
					units![index].Quantity -= (uint)resourceAmount;

					StackResource newUnit = new StackResource();
					newUnit.Unit = _recipe.FinalUnit;
					newUnit.Quantity = (uint)resourceAmount;

					bool hasSpace = false;
					for (int i = 0; i < units.Length; i++)
					{
						if (units[i] == null)
						{
							units[i] = newUnit;
							hasSpace = true;
							break;
						}
					}

					if (!hasSpace)
					{
						character?.HidePanel();
						unitsToSwap = new StackResource[1];
						unitsToSwap[0] = newUnit;
						character?.inventoryPanel?.SwapWarning();
					}

					RemoveResource(type, resourceAmount);
				}
			}
		}

		character?.inventoryPanel?.ResetCombining();
		character?.inventoryPanel?.UpdateUI(this);
	}

	public void CancelCombining()
	{
		resourceCombining = "";
	}

	public void StartSwapping(int index, bool myUnits)
	{
		if (myUnits)
		{
			if (units![index] != null)
			{
				unitSwapping = units[index];
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
		GD.Print(swappingIndex, index, swappingMyUnit);
		if (swappingMyUnit)
		{
			StackResource newUnit = new StackResource();
			newUnit.Unit = unitsToSwap?[index].Unit;
			newUnit.Quantity = unitsToSwap![index].Quantity;

			StackResource newBonusUnit = new StackResource();
			newBonusUnit.Unit = unitSwapping!.Unit;;
			newBonusUnit.Quantity = unitSwapping.Quantity;

			units![swappingIndex] = newUnit;
			unitsToSwap![index] = newBonusUnit;
		}
		else
		{
			StackResource newUnit = new StackResource();
			newUnit.Unit = unitSwapping!.Unit;
			newUnit.Quantity = unitSwapping!.Quantity;

			StackResource newBonusUnit = new StackResource();
			newBonusUnit.Unit = units?[index].Unit;
			newBonusUnit.Quantity = units![index].Quantity;

			units![index] = newUnit;
			unitsToSwap![swappingIndex] = newBonusUnit;
		}

		character?.inventoryPanel?.swapWindow?.UpdateUI(this);
		CancelSwapping();
	}

	public void CancelSwapping()
	{
		unitSwapping = null;
		swappingIndex = 0;
		swappingMyUnit = false;
		character?.inventoryPanel?.swapWindow?.CancelSwapping();
	}
}
