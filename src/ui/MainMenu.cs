using Godot;
using System;

public partial class MainMenu : Control
{
	private Control? _titleMenu;
	private Control? _settingsMenu;
	private Control? _quitMenu;
	private SaveBrowser? _saveBrowser;
	private Button? cancelSaveMenu;

	private Button? _playButton;
	private Button? _settingsButton;
	private Button? _quitButton;

	private Button? _settingsBackButton;
	private Button? _quitBackButton;

	public override void _Ready()
	{
		_titleMenu = GetNode<Control>("TitleMenu");
		_settingsMenu = GetNode<Control>("SettingsMenu");
		_quitMenu = GetNode<Control>("QuitMenu");

		_playButton = GetNode<Button>("TitleMenu/PanelContainer/MarginContainer/VBoxContainer/PLAY");
		if (_playButton == null)
		{
			GD.PrintErr("PlayButton not found in TitleMenu");
		}

		_settingsButton = GetNode<Button>("TitleMenu/PanelContainer/MarginContainer/VBoxContainer/SETTINGS");
		if (_settingsButton == null)
		{
			GD.PrintErr("SettingsButton not found in TitleMenu");
		}

		_quitButton = GetNode<Button>("TitleMenu/PanelContainer/MarginContainer/VBoxContainer/QUIT");
		if (_quitButton == null)
		{
			GD.PrintErr("QuitButton not found in TitleMenu");
		}

		_settingsBackButton = GetNode<Button>("SettingsMenu/Background/Back");
		if (_settingsBackButton == null)
		{
			GD.PrintErr("Settings Menu Back Button not found in SettingsMenu");
		}

		_quitBackButton = GetNode<Button>("QuitMenu/PanelContainer/MarginContainer/VBoxContainer/BACK");
		if (_quitBackButton == null)
		{
			GD.PrintErr("QUIT Menu Back Button not found in QuitMenu");
		}

		_saveBrowser = GetNode<SaveBrowser>("/root/MainMenu/select_file");
		if (_saveBrowser == null)
		{
			GD.PrintErr("Save Browser not found in QuitMenu");
		}

		cancelSaveMenu = GetNode<Button>("/root/MainMenu/select_file/CancelSaveMenu");
		if (cancelSaveMenu == null)
		{
			GD.PrintErr("Save Browser Cancel not found in QuitMenu");
		}

		Button exitButton = GetNode<Button>("/root/MainMenu/QuitMenu/PanelContainer/MarginContainer/VBoxContainer/QUIT");
		exitButton.Pressed += () =>
		{
			GetTree().Quit();
		};

		_playButton!.Pressed += OnPlayButtonPressed;
		cancelSaveMenu!.Pressed += OnCancelSaveButtonPressed;
		_settingsButton!.Pressed += OnSettingsButtonPressed;
		_quitButton!.Pressed += OnQuitButtonPressed;

		_settingsBackButton!.Pressed += OnSettingsBackButtonPressed;
		_quitBackButton!.Pressed += OnQuitBackButtonPressed;
	}

	public override void _Process(double delta)
	{
	}

	private void OnPlayButtonPressed()
	{
		_titleMenu!.Hide();
		_saveBrowser!.Show();
		cancelSaveMenu!.Show();
		_saveBrowser.Setup(true);
	}

	public void OnCancelSaveButtonPressed()
	{
		_titleMenu!.Show();
		_saveBrowser!.Hide();
		cancelSaveMenu!.Hide();
	}

	private void OnSettingsButtonPressed()
	{
		_titleMenu!.Hide();
		_settingsMenu!.Show();
	}

	private void OnQuitButtonPressed()
	{
		_titleMenu!.Hide();
		_quitMenu!.Show();
	}

	private void OnSettingsBackButtonPressed()
	{
		_titleMenu!.Show();
		_settingsMenu!.Hide();
	}

	private void OnQuitBackButtonPressed()
	{
		_titleMenu!.Show();
		_quitMenu!.Hide();
	}
}
