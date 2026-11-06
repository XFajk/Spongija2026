using Godot;
using System.Collections.Generic;
using System.Linq;

// The hero's necromancy spells raise Multiplier times as much (hero casts only; a unit's own cast is unchanged),
// on top of any stat bonuses.
[GlobalClass]
public partial class NecromancyArtefact : StatArtefact
{
	[Export(PropertyHint.Range, "1,5")]
	public int Multiplier { get; set; } = 2;

	// The stronger copy of each spell, made once and reused: the spellbook and Hero.CanCast compare spells by
	// instance, so every call must give the same one. A value derived from the spell, not battle state, so it can
	// live on the shared resource.
	private readonly Dictionary<NecromancySpell, NecromancySpell> _empowered = new();

	public override Hero Apply(Hero hero)
	{
		Hero result = base.Apply(hero);
		result.Spells = result.Spells.Select(s => s is NecromancySpell necromancy ? GetEmpowered(necromancy) : s).ToList();
		return result;
	}

	private NecromancySpell GetEmpowered(NecromancySpell spell)
	{
		if (!_empowered.TryGetValue(spell, out NecromancySpell? empowered))
		{
			empowered = (NecromancySpell)spell.Duplicate();
			empowered.Amount = spell.Amount * Multiplier;
			_empowered[spell] = empowered;
		}
		return empowered;
	}

	public override IEnumerable<string> GetEffectLines() =>
		base.GetEffectLines().Append($"Hero's necromancy raises x{Multiplier}");
}
