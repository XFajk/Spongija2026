using Godot;

// A stack's details: name, description, numbers, stats and what it can do. ArenaUserInterface shows it after
// the mouse rests on a stack for a while (scenes/ui/stack_tooltip.tscn) and refreshes it every frame.
public partial class StackTooltip : PanelContainer
{
	private Label? _nameLabel;
	private Label? _descriptionLabel;
	private Label? _quantityLabel;
	private Label? _healthLabel;
	private Label? _manaLabel;
	private Label? _strengthLabel;
	private Label? _defenseLabel;
	private Label? _speedLabel;
	private Label? _attacksLabel;
	private Label? _spellsLabel;
	private Label? _effectsLabel;

	public Stack? Stack { get; private set; }

	public override void _Ready()
	{
		_nameLabel = GetNode<Label>("%Name");
		_descriptionLabel = GetNode<Label>("%Description");
		_quantityLabel = GetNode<Label>("%Quantity");
		_healthLabel = GetNode<Label>("%Health");
		_manaLabel = GetNode<Label>("%Mana");
		_strengthLabel = GetNode<Label>("%Strength");
		_defenseLabel = GetNode<Label>("%Defense");
		_speedLabel = GetNode<Label>("%Speed");
		_attacksLabel = GetNode<Label>("%Attacks");
		_spellsLabel = GetNode<Label>("%Spells");
		_effectsLabel = GetNode<Label>("%Effects");
		// Lies over the battle, so the mouse must go through it to the units and tiles
		IgnoreMouse(this);
	}

	// Shows the stack. Call again (Refresh) when it may have changed, e.g. every frame while shown.
	public void ShowStack(Stack stack)
	{
		Stack = stack;
		Refresh();
	}

	public void Refresh()
	{
		if (Stack is not Stack stack)
			return;
		Unit unit = stack.Unit;

		_nameLabel!.Text = unit.Name;
		_descriptionLabel!.Text = unit.Description;
		_descriptionLabel.Visible = unit.Description != "";

		_quantityLabel!.Text = $"Units: {stack.Quantity}";
		_healthLabel!.Text = $"Health: {stack.TopHealth}/{unit.MaxHealth}";
		_manaLabel!.Text = $"Mana: {stack.Mana}/{unit.MaxMana}";
		_manaLabel.Visible = unit.MaxMana > 0;

		// The unit's base value, then what the effects and the hero add to it together
		_strengthLabel!.Text = "Strength: " + FormatStat(unit.Strength, stack.GetStrength());
		_defenseLabel!.Text = "Defense: " + FormatStat(unit.Defense, stack.GetDefense());
		_speedLabel!.Text = "Speed: " + FormatStat(unit.SpeedTier, stack.GetSpeedTier());

		_attacksLabel!.Text = $"Melee: {(stack.HasMelee ? "yes" : "no")}    Ranged: {(stack.HasLongRange ? "yes" : "no")}";
		_spellsLabel!.Text = $"Spells: {unit.Spells.Count}";
		_effectsLabel!.Text = $"Active effects: {stack.Effects.Count}";
	}

	// "7", "7 + 3" or "7 - 2"
	private static string FormatStat(int baseValue, int current)
	{
		int bonus = current - baseValue;
		if (bonus == 0)
			return baseValue.ToString();
		return bonus > 0 ? $"{baseValue} + {bonus}" : $"{baseValue} - {-bonus}";
	}

	private static void IgnoreMouse(Node node)
	{
		if (node is Control control)
			control.MouseFilter = MouseFilterEnum.Ignore;
		foreach (Node child in node.GetChildren())
			IgnoreMouse(child);
	}
}
