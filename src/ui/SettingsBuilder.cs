using Godot;
using System;
using System.Collections.Generic;

public partial class SettingsBuilder : Control
{
	[Export] public SettingsPageResource? settings;

	[Export] public VBoxContainer? Container;

	private readonly List<Vector2I> _resolutions = new()
	{
		new(800, 600),
		new(1024, 768),
		new(1280, 720),
		new(1366, 768),
		new(1600, 900),
		new(1920, 1080),
		new(2560, 1080),
		new(2560, 1440),
		new(3440, 1440),
		new(3840, 2160)
	};

	public override void _Ready()
	{
		Build();
	}

	public void Build()
	{
		foreach (Node child in Container!.GetChildren())
			child.QueueFree();

		foreach (var setting in settings!.settings)
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
				(instance as ToggleSettingUI)!.GetNode<CheckBox>("CheckBox").Toggled += (toggled) => Call(toggle.function!, toggled);
				
				break;

			case SliderSetting slider:
				(instance as SliderSettingUI)?.Setup(
					slider.name, slider.minValue, slider.maxValue, slider.step);
				(instance as SliderSettingUI)!.GetNode<HSlider>("HSlider")!.ValueChanged += (value) => Call(setting.function!, value);

				if (setting.name == "Master")
				{
					(instance as SliderSettingUI)!.GetNode<HSlider>("HSlider")!.Value = Mathf.DbToLinear( AudioServer.GetBusVolumeDb(0)) * 100f;
					(instance as SliderSettingUI)!.UpdateValueLabel((float)(instance as SliderSettingUI)!.GetNode<HSlider>("HSlider")!.Value);
				}

				if (setting.name == "Music")
				{
					(instance as SliderSettingUI)!.GetNode<HSlider>("HSlider")!.Value = Mathf.DbToLinear( AudioServer.GetBusVolumeDb(2)) * 100f;
					(instance as SliderSettingUI)!.UpdateValueLabel((float)(instance as SliderSettingUI)!.GetNode<HSlider>("HSlider")!.Value);
				}

				if (setting.name == "Sfx")
				{
					(instance as SliderSettingUI)!.GetNode<HSlider>("HSlider")!.Value = Mathf.DbToLinear( AudioServer.GetBusVolumeDb(1)) * 100f;
					(instance as SliderSettingUI)!.UpdateValueLabel((float)(instance as SliderSettingUI)!.GetNode<HSlider>("HSlider")!.Value);
				}

				break;

			case Dropdown dropdown:
				(instance as DropdownSettingUI)?.Setup(
					dropdown.name, dropdown.options);
				(instance as DropdownSettingUI)!.GetNode<OptionButton>("OptionButton").ItemSelected += (index) => Call(dropdown.function!, index);
				
				if (setting.name == "Resolution")
				{
					(instance as DropdownSettingUI)!.GetNode<OptionButton>("OptionButton").Clear();

					Vector2I currentResolution = GetWindow().Size;

					if (!_resolutions.Contains(currentResolution))
						_resolutions.Add(currentResolution);

					_resolutions.Sort((a, b) =>
						(a.X * a.Y).CompareTo(b.X * b.Y));

					for (int i = 0; i < _resolutions.Count; i++)
					{
						Vector2I resolution = _resolutions[i];

						(instance as DropdownSettingUI)!.GetNode<OptionButton>("OptionButton").AddItem(
							$"{resolution.X}x{resolution.Y}"
						);

						if (resolution == currentResolution)
							(instance as DropdownSettingUI)!.GetNode<OptionButton>("OptionButton").Select(i);
					}
				}
				break;

			case TitleSetting title:
				(instance as TitleSettingsUI)?.Setup(title.name);
				break;

			default:
				GD.PushWarning($"Unknown type of Setting: {setting.name}");
				break;
		}
	}

	public void Fullscreen(bool _fullscreen)
	{
		if (_fullscreen)
		{
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
		}
		else
		{
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
		}
	}

	public void Vsync(bool _vsync)
	{
		if (_vsync)
		{
			DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Enabled);
		}
		else
		{
			DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
		}
	}

	public void Resolution(int _index)
	{
		DisplayServer.WindowSetSize(_resolutions[_index]);
	}

	public void Music(double value)
	{
		SetBuses("Music", value);
	}

	public void Master(double value)
	{
		SetBuses("Master", value);
	}

	public void Sfx(double value)
	{
		SetBuses("SFX", value);
	}

	public void SetBuses(string _bus, double value)
	{
		int busIndex = AudioServer.GetBusIndex(_bus);

		if (busIndex == -1)
			return;

		if (value <= 0)
		{
			AudioServer.SetBusMute(busIndex, true);
		}
		else
		{
			AudioServer.SetBusMute(busIndex, false);

			float volume = Mathf.LinearToDb(
				(float)value / 100f
			);

			AudioServer.SetBusVolumeDb(busIndex, volume);
		}
	}
}
