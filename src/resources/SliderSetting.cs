using Godot;
using System;

[GlobalClass]
public partial class SliderSetting : Setting
{
	[Export]
	public float maxValue;
	[Export]
	public float minValue;
	[Export]
	public float step;
}
