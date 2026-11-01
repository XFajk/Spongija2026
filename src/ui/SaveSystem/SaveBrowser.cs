using Godot;
using System;
using System.Collections.Generic;

public partial class SaveBrowser : Panel
{
	[Export] public SaveManager? manager;
	[Export] PackedScene? saveFilePrefab;
	[Export] public Control? container;
	[Export] Button? createButton;
	List<int> _indexes = new List<int>();

	public override void _Ready()
	{
		createButton!.Pressed += CreateFile;
	}


	public void Setup(bool _save)
	{
		foreach(SaveFile _file in container!.GetChildren())
		{
			_file.QueueFree();
		}

		createButton!.Visible = _save;

		List<string> _saves = GetSaveFiles();
		_indexes = new List<int>();
		for (int i = 0; i < _saves.Count; i++)
		{
			_indexes.Add(GetNumber(_saves[i]));
		}

		_indexes.Sort();

		for (int i = 0; i < _indexes.Count; i++)
		{
			SaveFile _file = saveFilePrefab!.Instantiate<SaveFile>();
			container!.AddChild(_file);
			_file.index = _indexes[i];
			_file._browser = this;
			_file._save = _save;
			_file.Text = "Save: " + _file.index.ToString();
		}

		if (!_save)
		{
			if (_indexes.Count == 0)
			{
				Setup(true);
			}
		}
	}

	private List<string> GetSaveFiles()
	{
		List<string> saves = new();

		using var dir = DirAccess.Open("user://saves/");

		if (dir == null)
			return saves;

		dir.ListDirBegin();

		string fileName = dir.GetNext();

		while (fileName != "")
		{
			if (!dir.CurrentIsDir() && fileName.StartsWith("slot_"))
			{
				saves.Add(fileName);
			}

			fileName = dir.GetNext();
		}

		dir.ListDirEnd();

		return saves;
	}

	private int GetNumber(string saveFile)
	{
		string idString = saveFile.Replace("slot_", "");
		string stripString = idString.Replace(".json", "");

		if (int.TryParse(stripString, out int slotId))
		{
			return slotId;
		}
		else return 0;
	}

	public void CreateFile()
	{
		int newSlotId = _indexes.Count > 0 ? _indexes[_indexes.Count - 1] + 1 : 1;
		CurrentSave.CurrentSaveIndex = newSlotId;

		if (manager != null)
		{
			manager.SaveGame(CurrentSave.CurrentSaveIndex);
		}
		else
		{
			Node3D map = GD.Load<PackedScene>("uid://cfsvco3a36qfu").Instantiate<Node3D>();
			GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToNode, map);
		}

		Hide();
		GetTree().Paused = false;
	}

	public void Select(int _index, bool _save)
	{
		CurrentSave.CurrentSaveIndex = _index;
		if (_save)
		{
			if (manager != null)
			{
				manager.SaveGame(CurrentSave.CurrentSaveIndex);
			}

			Hide();
			GetTree().Paused = false;
		}
		else
		{	
			GetTree().Paused = false;
			GetTree().ReloadCurrentScene();
		}
	}

	public void Delete(int _index, bool _save)
	{
		string path = ProjectSettings.GlobalizePath($"user://saves/slot_{_index}.json");
		GD.Print(path);

		if (FileAccess.FileExists($"user://saves/slot_{_index}.json"))
		{
			Error error = DirAccess.RemoveAbsolute(path);
		}

		Setup(_save);
	}
}
