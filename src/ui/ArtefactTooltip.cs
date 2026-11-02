using Godot;
using System.Linq;

// An artefact's details: icon, name, slot, what it does and its description. ArtefactSlot makes one as its custom
// tooltip (scenes/ui/artefact_tooltip.tscn).
public partial class ArtefactTooltip : PanelContainer
{
	public const string Scene = "uid://c8artftltp3m6";

	private TextureRect? _icon;
	private Label? _nameLabel;
	private Label? _slotLabel;
	private Label? _effectsLabel;
	private Label? _descriptionLabel;

	// Unique names resolve right after instantiation, so this works before the tooltip enters the tree
	private void FindNodes()
	{
		_icon = GetNode<TextureRect>("%Icon");
		_nameLabel = GetNode<Label>("%Name");
		_slotLabel = GetNode<Label>("%Slot");
		_effectsLabel = GetNode<Label>("%Effects");
		_descriptionLabel = GetNode<Label>("%Description");
	}

	public static ArtefactTooltip? Create(Artefact artefact)
	{
		if (GD.Load<PackedScene>(Scene).Instantiate() is not ArtefactTooltip tooltip)
		{
			GD.PushError("The artefact tooltip scene's root isn't an ArtefactTooltip.");
			return null;
		}
		// Godot adds it to the tree only after this returns, so the nodes are looked up here instead of in _Ready
		tooltip.FindNodes();
		tooltip.ShowArtefact(artefact);
		return tooltip;
	}

	public void ShowArtefact(Artefact artefact)
	{
		_icon!.Texture = artefact.Icon;
		_icon.Visible = artefact.Icon != null;
		_nameLabel!.Text = artefact.Name;
		_slotLabel!.Text = "Slot: " + artefact.BodyPart switch
		{
			ArtefactBodyPart.head => "head",
			_ => "body",
		};
		string effects = string.Join("\n", artefact.GetEffectLines());
		_effectsLabel!.Text = effects;
		_effectsLabel.Visible = effects != "";
		_descriptionLabel!.Text = artefact.Description;
		_descriptionLabel.Visible = artefact.Description != "";
	}
}
