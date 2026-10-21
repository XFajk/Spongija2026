using Godot;
using System.Threading.Tasks;

[GlobalClass]
public abstract partial class SpellAbility : Resource, IAbility
{
    [Export]
    public string Name { get; set; } = "";

    [Export]
    public uint ManaCost { get; set; }

    // Hero Knowledge needed to cast this spell as a hero spell.
    [Export]
    public uint MinKnowledge { get; set; }

    public abstract Task Use(Stack user, Stack target, Arena arena);
}
