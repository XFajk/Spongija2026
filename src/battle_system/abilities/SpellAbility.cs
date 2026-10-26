using Godot;
using System.Threading.Tasks;

[GlobalClass]
public abstract partial class SpellAbility : Resource, IAbility
{
    // Whose stacks the spell can be cast on, from the caster's side
    public enum TargetSide
    {
        Enemies,
        Allies,
        Any,
    }

    [Export]
    public string Name { get; set; } = "";

    [Export]
    public TargetSide Targets { get; set; } = TargetSide.Enemies;

    // Shown on the spell's button in the spellbook.
    [Export]
    public Texture2D? Icon { get; set; }

    [Export]
    public uint ManaCost { get; set; }

    // Hero Knowledge needed to cast this spell as a hero spell.
    [Export]
    public uint MinKnowledge { get; set; }

    public abstract Task Use(Stack user, Stack target, Arena arena);

    // Cast by the hero instead of a unit: origin is where the hero stands (global, the arena's HeroSpellCastPosition),
    // so effects can come from there. caster is the stack whose turn it is (for its side and its hero).
    // By default it's cast like the stack's own spell.
    public virtual Task UseFromHero(Stack caster, Vector3 origin, Stack target, Arena arena) => Use(caster, target, arena);
}
