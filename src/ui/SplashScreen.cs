using Godot;
using System;

public partial class SplashScreen : VideoStreamPlayer
{

	[Export]
	public PackedScene? BeginningScene;

	public override void _Ready()
	{
		LoadSettings();

		Finished += () =>
		{
			if (BeginningScene != null)
				GetTree().ChangeSceneToPacked(BeginningScene);
		};
	}

	public void LoadSettings()
	{
		string path = $"{"user://saves/"}settings.json";

		if (! Godot.FileAccess.FileExists(path))
			return;

		using var file = Godot.FileAccess.Open(
			path,
			 Godot.FileAccess.ModeFlags.Read
		);

		if (file == null)
			return;

		var json = Json.ParseString(file.GetAsText());

		if (json.VariantType != Variant.Type.Dictionary)
			return;

		var saveData = json.AsGodotDictionary();

		foreach(var _data in saveData)
		{
			string key = _data.Key.AsString();

			switch (key)
			{
				case "Fullscreen":
					LoadFullscreen((bool)_data.Value);
					break;

				case "V-Sync":
					Vsync((bool)_data.Value);
					break;

				case "Resolution":
					string[] res = _data.Value.AsString().Split("x");
					Vector2I resolution = new Vector2I(int.Parse(res[0]), int.Parse(res[1]));
					if (DisplayServer.WindowGetMode() != DisplayServer.WindowMode.Fullscreen)
					{
						DisplayServer.WindowSetSize(resolution);
						GD.Print($"Resolution set to: {resolution.X}x{resolution.Y}");
					}
					break;

				case "Master":
					SetBuses("Master", (double)_data.Value);
					break;

				case "Sfx":
					SetBuses("SFX", (double)_data.Value);
					break;

				case "Music":
					SetBuses("Music", (double)_data.Value);
					break;
			}
		}
	}

	public void LoadFullscreen(bool _fullscreen)
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
