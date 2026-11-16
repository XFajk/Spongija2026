using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// The battle's tutorial, through a PopoutManager as on the map, reacting to what happens. Two kinds of tips:
// - popouts, which pause the tree until closed: Arena asks at the start of every player turn, while it waits for the
//   player anyway, and the tips whose condition holds and that haven't been shown open
// - hints, text at the top of the screen that pauses nothing and ignores the mouse, shown while the player tries what
//   they explain (placing the stacks, moving, looking at a stack's stats) until Arena reports it done
// Each tip is shown once: a popout counts when it opens, a hint once the player has done it. Shown tips are kept in
// TutorialChecklist.battleTips and, in a battle from the map, written into the save slot right away
// (SaveManager.PatchSave), since a lost battle reloads the save made before it.
// Made by Arena, on its own CanvasLayer above the battle's interface.
public partial class BattleTutorial : Node
{
	private const string PlacementHint = "battle_placement";
	private const string TurnHint = "battle_turn";
	private const string StatsHint = "battle_stats_hover";

	private readonly record struct Tip(string Id, string Title, string Text);

	// Seconds between one hint going and the next showing, so the first fades out before the next fades in
	[Export]
	public float HintGap { get; set; } = 0.4f;

	private PopoutManager? _popouts;
	// One hint shows at a time; the others wait in order
	private (Tip Tip, Control Node)? _currentHint;
	private readonly List<Tip> _queuedHints = [];

	// Player turns so far this battle, to spread the tips over the first turns
	private int _playerTurns;

	public static BattleTutorial Create(Node parent)
	{
		var layer = new CanvasLayer { Name = "TutorialLayer", Layer = 20 };
		parent.AddChild(layer);
		var tutorial = new BattleTutorial { Name = "BattleTutorial" };
		layer.AddChild(tutorial);
		tutorial._popouts = PopoutManager.Create(layer);
		return tutorial;
	}

	public override void _Ready()
	{
		// It runs while its popouts pause the tree
		ProcessMode = ProcessModeEnum.Always;
	}

	#region Tips

	// Before the battle, while the player arranges the stacks; until OnPlacementFinished
	public void OnPlacementPhase() => ShowHint(new(PlacementHint, "Before the Battle",
		"Drag your stacks to arrange them in the first two columns. Each stack is one slot of your army. "
		+ "Press Start the Battle when you're ready."));

	// Start Battle was pressed
	public void OnPlacementFinished() => CompleteHint(PlacementHint);

	// The player decided a turn (moved, attacked, cast, waited)
	public void OnPlayerTurnSubmitted() => CompleteHint(TurnHint);

	// A stack's tooltip showed. Before its hint did, the player has found it alone and doesn't need it.
	public void OnStackStatsSeen() => CompleteHint(StatsHint);

	// The battle ended: no hint stays over the end screen. Hints not done yet show again next battle.
	public void HideHints()
	{
		_popouts?.HideAllHints();
		_currentHint = null;
		_queuedHints.Clear();
	}

	// At the start of a player stack's turn, before its input shows. retaliationSeen: a stack has struck back in melee
	// during this battle.
	public async Task OnPlayerTurn(Battle battle, Stack stack, bool retaliationSeen)
	{
		_playerTurns++;
		ShowHint(new(TurnHint, "Your Turn",
			"The stack outlined in gold acts now. Click a green tile to move there, or an enemy on a red tile to "
			+ "attack it. The turn order runs along the bottom: hover a square to find its stack."));
		ShowHint(new(StatsHint, "Know Your Enemy",
			"Rest the mouse on any stack, yours or the enemy's, to see its stats: its top unit's health, "
			+ "Strength, Defense, Speed, and whether it can shoot or cast. A stat shown as 7 + 3 is the unit's "
			+ "own value plus what the hero and effects add."));

		var tips = new List<Tip>();

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

	#region Popouts and hints

	// Shows the tips not shown yet as popouts, the first on top, and completes when the last is closed
	private async Task ShowTips(List<Tip> tips)
	{
		List<Tip> fresh = tips.Where(t => !TutorialChecklist.battleTips.Contains(t.Id)).ToList();
		if (fresh.Count == 0 || !IsInsideTree() || _popouts == null)
			return;

		MarkShown(fresh.Select(t => t.Id));
		// Reversed, so the first tip ends up on top and is read first
		for (int i = fresh.Count - 1; i >= 0; i--)
			_popouts.SpawnPopout(fresh[i].Title, fresh[i].Text);
		await _popouts.WaitUntilClosed();
	}

	// Shows the tip as a hint, or queues it behind the one showing, unless it's been done before or is already there
	private void ShowHint(Tip tip)
	{
		if (_popouts == null || TutorialChecklist.battleTips.Contains(tip.Id)
			|| _currentHint?.Tip.Id == tip.Id || _queuedHints.Any(t => t.Id == tip.Id))
			return;
		if (_currentHint != null)
		{
			_queuedHints.Add(tip);
			return;
		}
		_currentHint = (tip, _popouts.ShowHint(tip.Title, tip.Text));
	}

	// The player did what the hint explains: it goes (the next queued one showing after HintGap), or leaves the queue,
	// and won't show again. Doing it before the hint shows counts too.
	private async void CompleteHint(string id)
	{
		if (TutorialChecklist.battleTips.Contains(id))
			return;
		MarkShown([id]);
		_queuedHints.RemoveAll(t => t.Id == id);
		if (_currentHint?.Tip.Id != id)
			return;

		_popouts?.HideHint(_currentHint.Value.Node);
		_currentHint = null;
		if (_queuedHints.Count == 0)
			return;
		await ToSignal(GetTree().CreateTimer(HintGap, processAlways: false), SceneTreeTimer.SignalName.Timeout);
		// Another hint may have shown meanwhile, or the battle ended and cleared the queue
		if (_currentHint != null || _queuedHints.Count == 0 || !IsInsideTree())
			return;
		Tip next = _queuedHints[0];
		_queuedHints.RemoveAt(0);
		ShowHint(next);
	}

	private static void MarkShown(IEnumerable<string> ids)
	{
		foreach (string id in ids)
			TutorialChecklist.battleTips.Add(id);
		// Only for a battle from the map; battle_test.tscn mustn't change a save
		if (EndFightScreen.TriggeringFightPOI != null)
			SaveManager.PatchSave(CurrentSave.CurrentSaveIndex, Tutorial.DefaultSaveId, Tutorial.BattleTipsKey,
				TutorialChecklist.BattleTipsToArray());
	}

	#endregion
}
