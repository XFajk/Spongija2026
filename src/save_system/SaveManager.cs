using Godot;
using System;
using System.IO;

public partial class SaveManager : Node
{
	private const string SavePath = "user://saves/";

	public override void _Ready()
	{
		LoadGame(1);
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionPressed("jump"))
		{
			SaveGame(1);
		}
	}


	public void SaveGame(int slot)
	{
		var saveData = new Godot.Collections.Dictionary<string, Variant>();

		var objects = GetTree().GetNodesInGroup("Savable");

		foreach (var obj in objects)
		{
			if (obj is not ISavable savable)
				continue;

			saveData[savable.SaveId] = savable.Save();
		}

		if (!DirAccess.DirExistsAbsolute(SavePath))
			DirAccess.MakeDirRecursiveAbsolute(SavePath);

		string json = Json.Stringify(saveData, "\t");

		using var file =  Godot.FileAccess.Open(
			$"{SavePath}slot_{slot}.json",
			 Godot.FileAccess.ModeFlags.Write
		);

		file?.StoreString(json);
	}

	public void LoadGame(int slot)
	{
		string path = $"{SavePath}slot_{slot}.json";

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

		var objects = GetTree().GetNodesInGroup("Savable");

		foreach (var obj in objects)
		{
			if (obj is not ISavable savable)
				continue;

			if (!saveData.ContainsKey(savable.SaveId))
				continue;

			savable.Load(
				saveData[savable.SaveId].AsGodotDictionary()
			);
		}
	}
}
