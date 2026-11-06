using Godot;
using Godot.Collections;
using System;

[GlobalClass]
public partial class SettingsPageResource : Resource
{
	[Export]
	public Array<Setting> settings = [];
}
