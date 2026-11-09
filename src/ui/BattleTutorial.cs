using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// The battle's tutorial: short popouts (scenes/ui/popout.tscn, as on the map) that react to what happens, each shown
// once. Arena asks at the placement phase and at the start of every player turn, while it waits for the player
// anyway; the tips whose condition holds and that haven't been shown pause the tree until they're closed.
// Shown tips are kept in TutorialChecklist.battleTips and, in a battle from the map, written into the save slot right
// away (SaveManager.PatchSave), since a lost battle reloads the save made before it.
// Made by Arena, on its own CanvasLayer above the battle's interface.
public partial class BattleTutorial : Control
{
	private const string PopoutScene = "uid://3me630kro538";

	[Export]
	public Vector2 PopoutSize { get; set; } = new(700, 600);

	[Export]
	public int TextFontSize { get; set; } = 21;

	private readonly record struct Tip(string Id, string Title, string Text);

	// The popouts open now, the last one on top; each button closes the top one
	private readonly List<Control> _popouts = [];
	private TaskCompletionSource? _closed;

	// Player turns so far this battle, to spread the tips over the first turns
	private int _playerTurns;

	public static BattleTutorial Create(Node parent)
	{
		var layer = new CanvasLayer { Name = "TutorialLayer", Layer = 20 };
		parent.AddChild(layer);
		var tutorial = new BattleTutorial { Name = "BattleTutorial" };
		layer.AddChild(tutorial);
		return tutorial;
	}

	public override void _Ready()
	{
		// It runs while it pauses the tree
		ProcessMode = ProcessModeEnum.Always;
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Ignore;
	}

	public override void _ExitTree()
	{
		// Never leave the tree paused behind
		if (_popouts.Count > 0)
			GetTree().Paused = false;
	}

	#region Tips

	// Before the battle, while the player arranges the stacks
	public Task OnPlacementPhase() => ShowTips(
	[
		new Tip("battle_placement", "Before the Battle",
			"Drag your stacks to arrange them in the first two columns. Each stack is one slot of your army. "
			+ "Press Start the Battle when you're ready."),
	]);

	// At the start of a player stack's turn, before its input shows. retaliationSeen: a stack has struck back in melee
	// during this battle.
	public async Task OnPlayerTurn(Battle battle, Stack stack, bool retaliationSeen)
	{
		_playerTurns++;
		var tips = new List<Tip>
		{
			new("battle_turn", "Your Turn",
				"The stack outlined in gold acts now. Click a green tile to move there, or an enemy on a red tile to "
				+ "attack it. The turn order runs along the bottom: hover a square to find its stack."),
			new("battle_stats_hover", "Know Your Enemy",
				"Rest the mouse on any stack, yours or the enemy's, to see its stats: its top unit's health, "
				+ "Strength, Defense, Speed, and whether it can shoot or cast. A stat shown as 7 + 3 is the unit's "
				+ "own value plus what the hero and effects add."),
		};

		// Spread over the first turns rather than all at once
		if (_playerTurns >= 2)
			tips.Add(new("battle_strength_defense", "Strength and Defense",
				"A stack's damage grows with its Strength and with how many units it has. Defense cuts the damage a "
				+ "stack takes: 20 Defense halves it, and every point helps. Spells ignore Defense, so they're "
				+ "strongest against armoured enemies."));

		if (battle.Round >= 2 && stack.Hero != null)
			tips.Add(new("battle_hero_stats", "Oromë's Stats",
				"Oromë doesn't fight, but his stats strengthen your whole army: each point of his Strength adds 10% "
				+ "to your units' Strength, each point of Defense 10% to their Defense. Knowledge gives him 10 mana "
				+ "per point, unlocks stronger spells and makes his damage spells hit harder. Artefacts raise his "
				+ "stats: equip them in the inventory."));

		if (battle.AllStacks.Any(s => s.TopHealth < s.Unit.MaxHealth))
			tips.Add(new("battle_health", "Units and Health",
				"The number under a stack is how many units it has. Every unit has its own health, and damage hits "
				+ "the top unit first: it falls only when its health runs out, so a hit can hurt a stack without "
				+ "lowering the number. Hover a stack to see its top unit's health. When you aim an attack, the target "
				+ "shows the damage it would take and the units it would lose."));

		if (retaliationSeen)
			tips.Add(new("battle_retaliation", "Striking Back",
				"A stack attacked in melee strikes back once per round, if it survives. Ranged attacks and spells are "
				+ "never struck back at."));

		if (stack.Waited)
			tips.Add(new("battle_discard", "Waiting",
				"This stack waited, so its turn came again at the end of the round. It can't wait twice in a round: "
				+ "Discard Turn ends its turn without acting."));

		if (stack.HasLongRange)
			tips.Add(new("battle_ranged", "Shooting",
				$"{stack.Unit.Name} can shoot any enemy on the field from where it stands: hover an enemy for the "
				+ "arrow. An enemy it can reach on foot is fought in melee instead (the sword). Shots are never "
				+ "struck back at."));

		if (battle.GetEnemies(stack).FirstOrDefault(e => e.IsImmuneToRanged) is Stack immune)
			tips.Add(new("battle_ranged_immune", "Untouchable by Arrows",
				$"{immune.Unit.Name} can't be hit by ranged attacks. Fight it in melee or with spells."));

		if (stack.Unit.Spells.Count > 0)
			tips.Add(new("battle_unit_spells", "Unit Spells",
				$"{stack.Unit.Name} can cast spells: press Use Unit Spell to open its book. A cast is the stack's "
				+ "action for the turn and uses the stack's own mana."));

		if (stack.Hero is Hero hero && hero.GetKnownSpells().Count > 0)
			tips.Add(new("battle_hero_spells", "Hero Spells",
				"Use Hero Spell casts one of Oromë's spells, once per round. It doesn't use up the stack's turn. "
				+ "The hero's mana carries over between battles and refills only at springs."));

		if (stack.Unit.Spells.OfType<HealSpell>().Any()
			|| (stack.Hero?.GetKnownSpells().OfType<HealSpell>().Any() ?? false))
			tips.Add(new("battle_healing", "Healing",
				"Healing restores the health of a stack's top unit only. It never brings back units that have "
				+ "fallen, so the number under the stack stays the same. Aim it at a stack whose top unit is hurt."));

		if (stack.Effects.OfType<SlowEffect>().Any())
			tips.Add(new("battle_slowed", "Slowed",
				"This stack is slowed: it moves fewer tiles for a few turns."));

		// One frame first, so the turn queue and the outline show the new turn behind the popouts
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ShowTips(tips);
	}

	#endregion

	#region Popouts

	// Shows the tips not shown yet, the first on top, and completes when the last is closed
	private async Task ShowTips(List<Tip> tips)
	{
		List<Tip> fresh = tips.Where(t => !TutorialChecklist.battleTips.Contains(t.Id)).ToList();
		if (fresh.Count == 0 || !IsInsideTree())
			return;

		foreach (Tip tip in fresh)
			TutorialChecklist.battleTips.Add(tip.Id);
		// Only for a battle from the map; battle_test.tscn mustn't change a save
		if (EndFightScreen.TriggeringFightPOI != null)
			SaveManager.PatchSave(CurrentSave.CurrentSaveIndex, Tutorial.DefaultSaveId, Tutorial.BattleTipsKey,
				TutorialChecklist.BattleTipsToArray());

		_closed ??= new TaskCompletionSource();
		// Reversed, so the first tip ends up on top and is read first
		for (int i = fresh.Count - 1; i >= 0; i--)
			SpawnPopout(fresh[i]);
		await _closed.Task;
	}

	private void SpawnPopout(Tip tip)
	{
		if (GD.Load<PackedScene>(PopoutScene).Instantiate() is not Control popout)
			return;
		// The scene is sized for the map's short tips: bigger, with a smaller font, so these aren't cut off
		popout.CustomMinimumSize = PopoutSize;
		AddChild(popout);
		popout.SetAnchorsPreset(LayoutPreset.Center);
		popout.GetNode<Label>("Label").Text = tip.Title;
		Label text = popout.GetNode<Label>("Text");
		text.Text = tip.Text;
		if (text.LabelSettings?.Duplicate() is LabelSettings settings)
		{
			settings.FontSize = TextFontSize;
			text.LabelSettings = settings;
		}
		popout.GetNode<Button>("Button").Pressed += CloseTopPopout;
		_popouts.Add(popout);

		// Blocks the battle behind the popouts; a custom cursor (sword, arrow) may have hidden the mouse
		MouseFilter = MouseFilterEnum.Stop;
		Input.MouseMode = Input.MouseModeEnum.Visible;
		GetTree().Paused = true;
	}

	private void CloseTopPopout()
	{
		if (_popouts.Count == 0)
			return;
		_popouts[^1].QueueFree();
		_popouts.RemoveAt(_popouts.Count - 1);
		if (_popouts.Count > 0)
			return;

		GetTree().Paused = false;
		MouseFilter = MouseFilterEnum.Ignore;
		TaskCompletionSource? closed = _closed;
		_closed = null;
		closed?.TrySetResult();
	}

	#endregion
}
