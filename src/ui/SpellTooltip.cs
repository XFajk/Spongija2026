using Godot;

// A spell's details: icon, name, cost, Knowledge needed, targets and description, and why it can't be cast now
// if it can't. SpellButton makes one as its custom tooltip (scenes/ui/spell_tooltip.tscn).
public partial class SpellTooltip : PanelContainer
{
	public const string Scene = "uid://c7spltlt4q8nx";

	private TextureRect? _icon;
	private Label? _nameLabel;
	private Label? _costLabel;
	private Label? _knowledgeLabel;
	private Label? _targetsLabel;
	private Label? _descriptionLabel;
	private Label? _cantCastLabel;

	// Unique names resolve right after instantiation, so this works before the tooltip enters the tree
	private void FindNodes()
	{
		_icon = GetNode<TextureRect>("%Icon");
		_nameLabel = GetNode<Label>("%Name");
		_costLabel = GetNode<Label>("%Cost");
		_knowledgeLabel = GetNode<Label>("%Knowledge");
		_targetsLabel = GetNode<Label>("%Targets");
		_descriptionLabel = GetNode<Label>("%Description");
		_cantCastLabel = GetNode<Label>("%CantCast");
	}

	// Instantiates the scene and shows the spell. cantCastReason is shown in red; null if it can be cast.
	// The Knowledge line shows only for hero spells.
	public static SpellTooltip? Create(SpellAbility spell, bool heroSpell, string? cantCastReason)
	{
		if (GD.Load<PackedScene>(Scene).Instantiate() is not SpellTooltip tooltip)
		{
			GD.PushError("The spell tooltip scene's root isn't a SpellTooltip.");
			return null;
		}
		// Godot adds it to the tree only after this returns, so the nodes are looked up here instead of in _Ready
		tooltip.FindNodes();
		tooltip.ShowSpell(spell, heroSpell, cantCastReason);
		return tooltip;
	}

	public void ShowSpell(SpellAbility spell, bool heroSpell, string? cantCastReason)
	{
		_icon!.Texture = spell.Icon;
		_icon.Visible = spell.Icon != null;
		_nameLabel!.Text = spell.Name;
		_costLabel!.Text = $"Mana cost: {spell.ManaCost}";
		_knowledgeLabel!.Text = $"Knowledge needed: {spell.MinKnowledge}";
		_knowledgeLabel.Visible = heroSpell;
		_targetsLabel!.Text = "Targets: " + spell.Targets switch
		{
			SpellAbility.TargetSide.Enemies => "enemies",
			SpellAbility.TargetSide.Allies => "allies",
			_ => "any stack",
		};
		_descriptionLabel!.Text = spell.Description;
		_descriptionLabel.Visible = spell.Description != "";
		_cantCastLabel!.Text = cantCastReason ?? "";
		_cantCastLabel.Visible = cantCastReason != null;
	}
}
