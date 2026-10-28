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
			// Godot shows a tooltip only for a non-empty TooltipText; _MakeCustomTooltip draws it instead
			TooltipText = value?.Name ?? "";
		}
	}

	// For the tooltip: whether it's a hero spell (shows the Knowledge needed), and why it can't be cast now
	// (null if it can). Set by the spellbook when it fills the button.
	public bool IsHeroSpell { get; set; }
	public string? CantCastReason { get; set; }

	public override GodotObject? _MakeCustomTooltip(string forText) =>
		_spell != null ? SpellTooltip.Create(_spell, IsHeroSpell, CantCastReason) : null;

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
