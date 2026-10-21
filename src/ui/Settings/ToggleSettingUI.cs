using Godot;
using System;

public partial class ToggleSettingUI : HBoxContainer
{
	[Export] private Label? _label;
	[Export] private CheckBox? _checkBox;

	private StringName? _settingId;

	public void Setup(string label)
	{
		_label!.Text = label;

		_checkBox!.Toggled += OnToggled;
	}

	private void OnToggled(bool value)
	{
		GD.Print($"{_label!.Text} -> {value}");
	}
}
