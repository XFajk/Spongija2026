using Godot;
using System;

// The arena's pause menu, opened and closed with Escape: Resume, Settings, Give Up (lose the battle right away) and
// Main Menu. No saving: saves belong to the map, and a battle from the map was saved right before it began. Made by
// Arena (Create) on its own CanvasLayer above the battle tutorial; it runs while it pauses the tree.
public partial class ArenaPauseMenu : Control
{
	public const string Scene = "uid://dq3xn6pvh2jle";
	public const string MainMenuScene = "uid://1cwqotqa3spu";

	// Above the battle tutorial (20) and the end screen (10), under ScreenFade (100)
	private const int Layer = 30;

	// Seconds the battle takes to fade to black before the main menu loads, and the menu to fade back in
	[Export(PropertyHint.Range, "0,3")] public float QuitFadeDuration = 0.8f;

	[Export(PropertyHint.MultilineText)]
	public string GiveUpQuestion { get; set; } = "Give up the battle?\nIt counts as lost.";

	[Export(PropertyHint.MultilineText)]
	public string MainMenuQuestion { get; set; } = "Leave for the main menu?\nThe battle counts as lost.";

	private Arena? _arena;

	private Control? _buttons;
	private Control? _confirm;
	private Label? _confirmLabel;
	private SettingsBuilder? _settings;

	// What the confirmation's Yes does
	private Action? _confirmed;

	private bool _leaving;

	public static ArenaPauseMenu Create(Arena arena)
	{
		var layer = new CanvasLayer { Name = "PauseMenuLayer", Layer = Layer, ProcessMode = ProcessModeEnum.Always };
		arena.AddChild(layer);
		var menu = GD.Load<PackedScene>(Scene).Instantiate<ArenaPauseMenu>();
		menu._arena = arena;
		layer.AddChild(menu);
		return menu;
	}

	public override void _Ready()
	{
		Visible = false;
		_buttons = GetNode<Control>("%Buttons");
		_confirm = GetNode<Control>("%Confirm");
		_confirmLabel = GetNode<Label>("%ConfirmLabel");
		_settings = GetNode<SettingsBuilder>("%SettingsMenu");

		GetNode<Button>("%Resume").Pressed += Resume;
		GetNode<Button>("%Settings").Pressed += OpenSettings;
		GetNode<Button>("%GiveUp").Pressed += () => AskConfirmation(GiveUpQuestion, GiveUp);
		GetNode<Button>("%MainMenu").Pressed += () => AskConfirmation(MainMenuQuestion, QuitToMainMenu);
		GetNode<Button>("%Yes").Pressed += () => _confirmed?.Invoke();
		GetNode<Button>("%No").Pressed += ShowButtons;
		_settings.GetNode<Button>("Background/Back").Pressed += CloseSettings;
	}

	public override void _ExitTree()
	{
		// Never leave the tree paused behind
		if (Visible && !_leaving)
			GetTree().Paused = false;
	}

	// Escape opens the menu, and inside it goes back one step (settings or a question to the buttons, the buttons
	// to the battle). Unhandled input, so Escape still cancels spell targeting first (Arena._Input).
	public override void _UnhandledInput(InputEvent @event)
	{
		if (!@event.IsActionPressed("ui_cancel") || @event.IsEcho() || _leaving)
			return;

		if (Visible)
			Back();
		else if (!TryOpen())
			return;
		GetViewport().SetInputAsHandled();
	}

	// Not while the tree is paused by something else (the tutorial's popouts) or after the battle has ended
	private bool TryOpen()
	{
		if (_arena == null || _arena.IsBattleFinished || GetTree().Paused)
			return false;

		_arena.PrepareForPause();
		ShowButtons();
		Visible = true;
		GetTree().Paused = true;
		return true;
	}

	private void Back()
	{
		if (_settings!.Visible)
			CloseSettings();
		else if (_confirm!.Visible)
			ShowButtons();
		else
			Resume();
	}

	private void Resume()
	{
		Visible = false;
		GetTree().Paused = false;
	}

	private void ShowButtons()
	{
		_confirmed = null;
		_settings!.Visible = false;
		_confirm!.Visible = false;
		_buttons!.Visible = true;
	}

	private void OpenSettings()
	{
		_buttons!.Visible = false;
		_settings!.Visible = true;
	}

	private void CloseSettings() => ShowButtons();

	private void AskConfirmation(string question, Action onYes)
	{
		_confirmed = onYes;
		_confirmLabel!.Text = question;
		_buttons!.Visible = false;
		_confirm!.Visible = true;
	}

	private void GiveUp()
	{
		Visible = false;
		_arena?.GiveUp();
	}

	// To the main menu after fading to black. A battle from the map was saved before it began, so loading that save
	// starts again in front of the fight, as after a loss.
	private async void QuitToMainMenu()
	{
		_leaving = true;
		SceneTree tree = GetTree();
		await ScreenFade.FadeOut(tree, QuitFadeDuration);

		EndFightScreen.TriggeringFightPOI = null;
		tree.Paused = false;
		Control menu = GD.Load<PackedScene>(MainMenuScene).Instantiate<Control>();
		float fade = QuitFadeDuration;
		menu.Ready += () => _ = ScreenFade.FadeIn(tree, fade);
		tree.CallDeferred(SceneTree.MethodName.ChangeSceneToNode, menu);
	}
}
