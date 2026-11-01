using Godot;
using System;

public interface ISavable
{
	string SaveId {get; }
	Godot.Collections.Dictionary<string, Variant> Save();
	void Load(Godot.Collections.Dictionary data);
}

public static class CurrentSave
{
	public static int CurrentSaveIndex;
}
