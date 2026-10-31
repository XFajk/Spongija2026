using Godot;
using System;

public partial class Tutorial : Node
{
	[Export] public PopoutManager? PopoutManager;

	public void ShowMovementPopout()
	{
		PopoutManager?.SpawnPopout("Camera", "Rotate camera with right button and move it with middle button. Then reset position with space button.");
		PopoutManager?.SpawnPopout("Movement", "Move by clicking on the ground. You can also click on different Points of Interest");
		TutorialChecklist.movementShown = true;
	}

	public void ShowInventory()
	{
		PopoutManager?.SpawnPopout("Inventory", "Open your inventory by clicking on your character. Close it with the close button or by clicking next on your screen");
		TutorialChecklist.inventoryShown = true;
	}

	public void ExplainInventory()
	{
		PopoutManager?.SpawnPopout("Combine", "Warrior = Iron + Pilgrim \n Archer = Silk + Pilgrim \n Cavarly = Horse + Pilgrim");
		PopoutManager?.SpawnPopout("Combine", "Get new units by combing Resources with units");
		PopoutManager?.SpawnPopout("Splitting", "Split your units by selecting the unit and pressing empty slot or slot with the same unit. Each slot represents Unit Group in a battle.");
		PopoutManager?.SpawnPopout("Categories", "Switch between categories with the buttons on the left side");
		TutorialChecklist.inventoryExplained = true;
	}

	public void ExplainSwapping()
	{
		PopoutManager?.SpawnPopout("Swapping", "If your inventory is full, you have to decide which unit groups you keep or leave behind.");
		TutorialChecklist.swappingExplained = true;
	}

	public void ExplainMagic()
	{
		PopoutManager?.SpawnPopout("Spells", "You can check your spells in the Spell book. These spells can be used in a battle. Each spell costs MANA, which can be refilled at SPRINGS.");
		PopoutManager?.SpawnPopout("Artefacts", "Equip artefacts from your inventory to get different advantages.");
		TutorialChecklist.magicExplained = true;
	}

	public async override void _Ready()
	{
		await ToSignal(
			GetTree().CreateTimer(0.5f),
			SceneTreeTimer.SignalName.Timeout
		);

		if (!TutorialChecklist.movementShown)
		{
			ShowMovementPopout();
		}
	}
}

public static class TutorialChecklist
{
	public static bool movementShown = false;
	public static bool inventoryShown = false;
	public static bool inventoryExplained = false;
	public static bool swappingExplained = false;
	public static bool magicExplained = false;
}
