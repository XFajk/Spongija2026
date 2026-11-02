using Godot;
using System.Collections.Generic;

// Raises the hero's stats by fixed amounts while equipped.
[GlobalClass]
public partial class StatArtefact : Artefact
{
	[ExportGroup("Bonuses")]
	[Export(PropertyHint.Range, "0,10")]
	public uint Strength { get; set; }

	[Export(PropertyHint.Range, "0,10")]
	public uint Defense { get; set; }

	[Export(PropertyHint.Range, "0,10")]
	public uint Knowledge { get; set; }

	// On top of what Knowledge gives (Hero.MaxMana)
	[Export(PropertyHint.Range, "0,200")]
	public uint MaxMana { get; set; }

	public override Hero Apply(Hero hero)
	{
		Hero result = hero.CopyBase();
		result.Strength += Strength;
		result.Defense += Defense;
		result.Knowledge += Knowledge;
		result.MaxManaBonus += MaxMana;
		return result;
	}

	public override IEnumerable<string> GetEffectLines()
	{
		if (Strength > 0)
			yield return $"+{Strength} Strength";
		if (Defense > 0)
			yield return $"+{Defense} Defense";
		if (Knowledge > 0)
			yield return $"+{Knowledge} Knowledge";
		if (MaxMana > 0)
			yield return $"+{MaxMana} max mana";
	}
}
