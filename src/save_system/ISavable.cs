using Godot;
using System;

public interface ISavable
{
	string SaveId {get; }
	Godot.Collections.Dictionary<string, Variant> Save();
	void Load(Godot.Collections.Dictionary data);
}
