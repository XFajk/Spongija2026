using Godot;
using System;

public partial class DropdownSettingUI : HBoxContainer
{
	[Export] private Label _label;
	[Export] private OptionButton _optionButton;

	public void Setup(string label, string[] options)
	{
		_label.Text = label;

		_optionButton.Clear();

		foreach (var option in options)
			_optionButton.AddItem(option);

		_optionButton.ItemSelected += OnItemSelected;
	}

	private void OnItemSelected(long index)
	{
		string selected = _optionButton.GetItemText((int)index);
		GD.Print($"{_label.Text} -> {selected}");
	}
}
