using Godot;

// A spell slot in the spellbook: shows its spell's icon and name, and emits SpellPicked with it when pressed.
public partial class SpellButton : Button
{
	[Signal]
	public delegate void SpellPickedEventHandler(SpellAbility spell);

	private SpellAbility? _spell;

	// Setting it shows the spell's icon and name on the button
	public SpellAbility? Spell
	{
		get => _spell;
		set
		{
			_spell = value;
			Icon = value?.Icon;
			// The label exists as soon as the scene is instantiated, so this works before _Ready too
			if (GetNodeOrNull<Label>("SpellName") is Label label)
				label.Text = value?.Name ?? "";
		}
	}

	public override void _Ready()
	{
		Pressed += OnPressed;
	}

	private void OnPressed()
	{
		if (_spell != null)
			EmitSignal(SignalName.SpellPicked, _spell);
	}
}
