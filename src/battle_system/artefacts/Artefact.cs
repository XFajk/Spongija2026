using Godot;
using System.Collections.Generic;

// An item the hero equips. Shared through .tres files like abilities, so never modify it at runtime; concrete
// artefacts are subclasses (StatArtefact, NecromancyArtefact). Hero's getters run every equipped artefact's Apply.
[GlobalClass]
public abstract partial class Artefact : Resource
{
	[Export]
	public string Name { get; set; } = "";

	// Shown in the artefact's tooltip in the inventory
	[Export(PropertyHint.MultilineText)]
	public string Description { get; set; } = "";

	[Export]
	public ArtefactBodyPart BodyPart { get; set; } = ArtefactBodyPart.body;

	[Export]
	public Texture2D? Icon { get; set; }

	// The hero with this artefact's effect added: a new Hero (start from hero.CopyBase()), the given one unchanged
	public abstract Hero Apply(Hero hero);

	// What it does, one line per effect (e.g. "+2 Strength"), for the tooltip
	public abstract IEnumerable<string> GetEffectLines();
}

public enum ArtefactBodyPart
{
	head,
	body
}
