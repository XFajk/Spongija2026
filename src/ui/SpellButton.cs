using Godot;

// A spell slot in the spellbook: shows its spell's icon (the hovered one while the mouse is over it) and name, and
// emits SpellPicked with it when pressed.
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
			UpdateIcon();
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
		MouseEntered += () => SetHovered(true);
		MouseExited += () => SetHovered(false);
	}

	// Set by the mouse signals, not IsHovered(): that's still true while MouseExited is being emitted
	private bool _hovered;

	private void SetHovered(bool hovered)
	{
		_hovered = hovered;
		UpdateIcon();
	}

	// The hovered icon while the mouse is over the button, unless it's disabled; otherwise the normal one.
	// Call after changing Disabled, since Godot has no signal for it.
	public void UpdateIcon()
	{
		Icon = _hovered && !Disabled ? _spell?.GetHoveredIcon() : _spell?.Icon;
	}

	private void OnPressed()
	{
		if (_spell != null)
			EmitSignal(SignalName.SpellPicked, _spell);
	}
}
