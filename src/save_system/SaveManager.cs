using Godot;
using System;
using System.IO;

public partial class SaveManager : Node
{
	private const string SavePath = "user://saves/";

	public override void _Ready()
	{
		LoadGame(CurrentSave.CurrentSaveIndex);
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

	// Sets one value of one ISavable's entry in a slot's save file, leaving the rest as it is. For scenes without
	// the map's savable nodes (the battle): the next load sees the value. Does nothing if the slot has no save or
	// the save has no entry for the id (a new partial entry would miss the values its Load expects).
	public static void PatchSave(int slot, string saveId, string key, Variant value)
	{
		string path = $"{SavePath}slot_{slot}.json";
		if (!Godot.FileAccess.FileExists(path))
			return;

		Godot.Collections.Dictionary saveData;
		using (var reader = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read))
		{
			if (reader == null)
				return;
			var json = Json.ParseString(reader.GetAsText());
			if (json.VariantType != Variant.Type.Dictionary)
				return;
			saveData = json.AsGodotDictionary();
		}

		if (!saveData.ContainsKey(saveId) || saveData[saveId].VariantType != Variant.Type.Dictionary)
			return;
		var entry = saveData[saveId].AsGodotDictionary();
		entry[key] = value;
		saveData[saveId] = entry;

		using var writer = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write);
		writer?.StoreString(Json.Stringify(saveData, "\t"));
	}

	public void LoadGame(int slot)
	{
		string path = $"{SavePath}slot_{slot}.json";

		// No save yet: a new game. The tutorial checklist is static, so it would still hold the tips seen in the
		// save played before in this session; each save has its own.
		if (! Godot.FileAccess.FileExists(path))
		{
			TutorialChecklist.Reset();
			return;
		}

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
