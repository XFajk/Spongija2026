using Godot;
using System;

public partial class SettingsBuilder : Control
{
	[Export] public SettingsPageResource? VideoSettings;
	[Export] public SettingsPageResource? InputSettings;
	[Export] public SettingsPageResource? AudioSettings;
	[Export] public SettingsPageResource? MyscSettings;


	[Export] public VBoxContainer? Container;

	public int TabShown = -1;

	private Button? _videoButton;
	private Button? _inputButton;
	private Button? _audioButton;
	private Button? _myscButton;

	private ColorRect? _videoButtonBg;
	private ColorRect? _inputButtonBg;
	private ColorRect? _audioButtonBg;
	private ColorRect? _myscButtonBg;

	public override void _Ready()
	{
		GetNodes();
		BuildVideo();
	}

	public override void _Process(double delta)
	{
	}

	private void GetNodes()
	{
		_videoButton = GetNode<Button>("Background/Buttons/Buttons/VBoxContainer/Video");
		if (_videoButton == null)
		{
			GD.PrintErr("VideoButton not found in SettingsMenu");
		}
		_inputButton = GetNode<Button>("Background/Buttons/Buttons/VBoxContainer/Input");
		if (_inputButton == null)
		{
			GD.PrintErr("InputButton not found in SettingsMenu");
		}
		_audioButton = GetNode<Button>("Background/Buttons/Buttons/VBoxContainer/Audio");
		if (_audioButton == null)
		{
			GD.PrintErr("AudioButton not found in SettingsMenu");
		}
		_myscButton = GetNode<Button>("Background/Buttons/Buttons/VBoxContainer/Mysc");
		if (_myscButton == null)
		{
			GD.PrintErr("MyscButton not found in SettingsMenu");
		}

		_videoButton!.Pressed += BuildVideo;
		_inputButton!.Pressed += BuildInput;
		_audioButton!.Pressed += BuildAudio;
		_myscButton!.Pressed += BuildMysc;

		_videoButtonBg = GetNode<ColorRect>("Background/Buttons/Backgrounds/VBoxContainer/VideoBg");
		if (_videoButtonBg == null)
		{
			GD.PrintErr("Video Button Background not found in SettingsMenu");
		}
		_inputButtonBg = GetNode<ColorRect>("Background/Buttons/Backgrounds/VBoxContainer/InputBg");
		if (_inputButtonBg == null)
		{
			GD.PrintErr("Input Button Background not found in SettingsMenu");
		}
		_audioButtonBg = GetNode<ColorRect>("Background/Buttons/Backgrounds/VBoxContainer/AudioBg");
		if (_audioButtonBg == null)
		{
			GD.PrintErr("Audio Button Background not found in SettingsMenu");
		}
		_myscButtonBg = GetNode<ColorRect>("Background/Buttons/Backgrounds/VBoxContainer/MyscBg");
		if (_myscButtonBg == null)
		{
			GD.PrintErr("Mysc Button Background not found in SettingsMenu");
		}
	}

	public void BuildVideo()
	{
		if (TabShown == 0)
			return;

		TabShown = 0;

		_videoButtonBg!.Color = Color.Color8(66, 1, 9, 255);
		_inputButtonBg!.Color = Color.Color8(66, 1, 9, 0);
		_audioButtonBg!.Color = Color.Color8(66, 1, 9, 0);
		_myscButtonBg!.Color = Color.Color8(66, 1, 9, 0);

		foreach (Node child in Container!.GetChildren())
			child.QueueFree();

		foreach (var setting in VideoSettings!.settings)
		{
			if (setting.SceneTemplate == null)
			{
				GD.PushWarning($"Setting '{setting.name}' does not have scene assigned!");
				continue;
			}

			Node instance = setting.SceneTemplate.Instantiate();
			BindSetting(setting, instance);
			Container!.AddChild(instance);
		}
	}

	public void BuildInput()
	{
		if (TabShown == 1)
			return;

		TabShown = 1;

		_inputButtonBg!.Color = Color.Color8(66, 1, 9, 255);
		_videoButtonBg!.Color = Color.Color8(66, 1, 9, 0);
		_audioButtonBg!.Color = Color.Color8(66, 1, 9, 0);
		_myscButtonBg!.Color = Color.Color8(66, 1, 9, 0);

		foreach (Node child in Container!.GetChildren())
			child.QueueFree();

		foreach (var setting in InputSettings!.settings)
		{
			if (setting.SceneTemplate == null)
			{
				GD.PushWarning($"Setting '{setting.name}' does not have scene assigned!");
				continue;
			}

			Node instance = setting.SceneTemplate.Instantiate();
			BindSetting(setting, instance);
			Container!.AddChild(instance);
		}
	}

	public void BuildAudio()
	{
		if (TabShown == 2)
			return;

		TabShown = 2;

		_audioButtonBg!.Color = Color.Color8(66, 1, 9, 255);
		_videoButtonBg!.Color = Color.Color8(66, 1, 9, 0);
		_inputButtonBg!.Color = Color.Color8(66, 1, 9, 0);
		_myscButtonBg!.Color = Color.Color8(66, 1, 9, 0);

		foreach (Node child in Container!.GetChildren())
			child.QueueFree();

		foreach (var setting in AudioSettings!.settings)
		{
			if (setting.SceneTemplate == null)
			{
				GD.PushWarning($"Setting '{setting.name}' does not have scene assigned!");
				continue;
			}

			Node instance = setting.SceneTemplate.Instantiate();
			BindSetting(setting, instance);
			Container!.AddChild(instance);
		}
	}

	public void BuildMysc()
	{
		if (TabShown == 3)
			return;

		TabShown = 3;

		_myscButtonBg!.Color = Color.Color8(66, 1, 9, 255);
		_inputButtonBg!.Color = Color.Color8(66, 1, 9, 0);
		_audioButtonBg!.Color = Color.Color8(66, 1, 9, 0);
		_videoButtonBg!.Color = Color.Color8(66, 1, 9, 0);

		foreach (Node child in Container!.GetChildren())
			child.QueueFree();

		foreach (var setting in MyscSettings!.settings)
		{
			if (setting.SceneTemplate == null)
			{
				GD.PushWarning($"Setting '{setting.name}' does not have scene assigned!");
				continue;
			}

			Node instance = setting.SceneTemplate.Instantiate();
			BindSetting(setting, instance);
			Container!.AddChild(instance);
		}
	}

	private void BindSetting(Setting setting, Node instance)
	{
		switch (setting)
		{
			case ToggleSetting toggle:
				(instance as ToggleSettingUI)?.Setup(toggle.name);
				break;

			case SliderSetting slider:
				(instance as SliderSettingUI)?.Setup(
					slider.name, slider.minValue, slider.maxValue, slider.step);
				break;

			case Dropdown dropdown:
				(instance as DropdownSettingUI)?.Setup(
					dropdown.name, dropdown.options);
				break;

			case TitleSetting title:
				(instance as TitleSettingsUI)?.Setup(title.name);
				break;

			default:
				GD.PushWarning($"Unknown type of Setting: {setting.name}");
				break;
		}
	}
}
