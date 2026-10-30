using Godot;
using System;
using System.Collections.Generic;

public partial class SwapPanel : Panel
{
	Inventory? _inventory;
	[Export] SwapUnitSlot[]? keepSlots;
	List<SwapUnitSlot> leaveSlots = new List<SwapUnitSlot>();
	[Export] PackedScene? swapSlotPrefab;
	[Export] Control? container;

	public override void _Ready()
	{
		Button closeButton = GetNode<Button>("Button");
		closeButton.Pressed += CloseWindow;
	}


	public void SetupWindow(Inventory inventory)
	{
		_inventory = inventory;

		for (int i = 0; i < keepSlots?.Length; i++)
		{
			if (_inventory?.units?[i] != null)
			{
				keepSlots[i].Setup((int)_inventory.units[i].Quantity, _inventory.units[i]?.Unit?.texture!);
			}
			else
			{
				keepSlots[i].Reset();
			}
		}

		for (int i = 0; i < _inventory?.unitsToSwap?.Length; i++)
		{
			SwapUnitSlot _prefab = swapSlotPrefab!.Instantiate<SwapUnitSlot>();
			container?.AddChild(_prefab);
			_prefab.Setup((int) _inventory.unitsToSwap[i].Quantity,  _inventory.unitsToSwap?[i].Unit?.texture!);
			_prefab.index = i;
			leaveSlots.Add(_prefab);
		}
	}

	public void UpdateUI(Inventory _inventory)
	{
		for (int i = 0; i < keepSlots?.Length; i++)
		{
			if (_inventory?.units?[i] != null)
			{
				keepSlots[i].Setup((int)_inventory.units[i].Quantity, _inventory.units[i]?.Unit?.texture!);
			}
			else
			{
				keepSlots[i].Reset();
			}
		}

		for (int i = 0; i < leaveSlots.Count; i++)
		{
			leaveSlots[i].Setup((int) _inventory!.unitsToSwap![i].Quantity,  _inventory.unitsToSwap?[i].Unit?.texture!);
		}
	}

	public void CancelSwapping()
	{
		for(int i = 0; i < keepSlots?.Length; i++)
		{
			keepSlots[i].Deselect();
		}

		for(int i = 0; i < leaveSlots?.Count; i++)
		{
			leaveSlots[i].Deselect();
		}
	}

	public void CloseWindow()
	{
		for (int i = 0; i < keepSlots?.Length; i++)
		{
			keepSlots[i].Reset();
		}

		for (int i = 0; i < leaveSlots.Count; i++)
		{
			leaveSlots[i].QueueFree();
		}

		leaveSlots = new List<SwapUnitSlot>();
		Hide();
		GetTree().Paused = false;
		_inventory?.UpdateUI();
	}
}
