using Godot;
using System;

public partial class SliderSettingUI : HBoxContainer
{
	[Export] private Label? _label;
	[Export] private HSlider? _slider;
	[Export] private Label? _valueLabel;

	public void Setup(string label, float minValue, float maxValue, float step)
	{
		_label!.Text = label;

		_slider!.MinValue = minValue;
		_slider.MaxValue = maxValue;
		_slider.Step = step;
		_slider.Value = minValue + ((maxValue - minValue) / 2);

		UpdateValueLabel(minValue + ((maxValue - minValue) / 2));

		_slider!.ValueChanged += OnValueChanged;
	}

	private void OnValueChanged(double value)
	{
		UpdateValueLabel((float)value);
	}

	private void UpdateValueLabel(float value)
	{
		if (_valueLabel != null)
			_valueLabel.Text = value.ToString("000");
	}
}
