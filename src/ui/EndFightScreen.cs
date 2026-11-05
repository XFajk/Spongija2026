using Godot;
using System;
using System.Collections.Generic;

public partial class EndFightScreen : Panel
{
	public const string MapScene = "uid://cfsvco3a36qfu";

	// The FightPOI that started the battle (set by FightPOI.Access), removed from the map after a win
	public static NodePath? TriggeringFightPOI;

	// The player's units that died in the fight, shown in earnedContainer. The opponent's aren't shown, as most
	// enemy units have no icon.
	public Godot.Collections.Array<StackResource> playerLostUnits = new Godot.Collections.Array<StackResource>();

	// After a win: how many of each of the player's stacks are left, in the order the stacks were made from the
	// inventory's slots (BattleStarter), 0 for a stack that died. Written back to the inventory before saving.
	public List<uint> survivingQuantities = new List<uint>();

	// After a win: the hero's mana left after the battle (BattleStarter gives the hero the inventory's Mana), or
	// null if the player's army had no hero
	public uint? heroMana;

	[Export] public PackedScene? iconPrefab;
	[Export] public Control? earnedContainer;
	[Export] public Label? statusLabel;
	[Export] public Button? confirmButton;

	// Seconds the battle takes to fade to black before the map loads; the map fades back in itself (BlackScreen)
	[Export(PropertyHint.Range, "0,3")] public float FadeOutDuration = 0.8f;

	bool _won;

	public override void _Ready()
	{
		confirmButton!.Pressed += Confirm;
	}

	public void SetWon(bool won)
	{
		_won = won;
		statusLabel!.Text = won ? "You won" : "You lost";
	}

	public void Setup()
	{
		foreach(TextureRect _icon in earnedContainer!.GetChildren())
		{
			_icon.QueueFree();
		}

		for (int i = 0; i < playerLostUnits.Count; i++)
		{
			TextureRect _prefab = iconPrefab!.Instantiate<TextureRect>();
			_prefab.GetNode<TextureRect>("Icon").Texture = playerLostUnits[i].Unit!.texture;
			_prefab.GetNode<Label>("Amount").Text = playerLostUnits[i].Quantity.ToString();
			earnedContainer.AddChild(_prefab);
		}
	}

	// Back to the adventure map after fading to black. Lost: the map loads the last save (made before the battle),
	// as if it never happened. Won: the map loads it too, then the defeated fight and the losses are applied and saved.
	async void Confirm()
	{
		confirmButton!.Disabled = true;
		await ScreenFade.FadeOut(GetTree(), FadeOutDuration);
		Node3D map = GD.Load<PackedScene>(MapScene).Instantiate<Node3D>();
		// The map's BlackScreen starts black and fades in by itself, so the overlay can go once it's there
		map.Ready += ScreenFade.Clear;

		// Without a FightPOI the battle didn't come from the map (battle_test.tscn), so its army isn't the inventory's
		if (_won && TriggeringFightPOI is NodePath fightPOI)
		{
			List<uint> survivors = survivingQuantities;
			uint? mana = heroMana;
			// The map's Ready comes after all its children's _Ready, so after SaveManager has loaded the old save,
			// and before their first _Process, so the fight is gone before it could start again with the hero
			// standing next to it.
			map.Ready += () => ApplyWin(map, fightPOI, survivors, mana);
		}

		TriggeringFightPOI = null;
		GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToNode, map);
	}

	// The inventory becomes what the army was when the battle ended: survivors and the hero's mana
	static void ApplyWin(Node3D map, NodePath fightPOI, List<uint> survivors, uint? heroMana)
	{
		if (map.GetTree().Root.GetNodeOrNull<FightPOI>(fightPOI) is FightPOI poi)
		{
			poi._Destroy();
		}

		// The same slots BattleStarter made the stacks from, in the same order
		Inventory inventory = map.GetNode<Inventory>("AdventureCharacter/Inventory");
		int stack = 0;
		for (int i = 0; i < inventory.Units!.Length && stack < survivors.Count; i++)
		{
			if (inventory.Units[i] == null || inventory.Units[i].Unit == null)
				continue;

			if (survivors[stack] == 0)
			{
				inventory.Units[i] = null!;
			}
			else
			{
				inventory.Units[i].Quantity = survivors[stack];
			}
			stack++;
		}

		if (heroMana is uint mana)
		{
			inventory.Mana = mana;
		}

		map.GetNode<SaveManager>("SaveSystem").SaveGame(CurrentSave.CurrentSaveIndex);
	}
}
