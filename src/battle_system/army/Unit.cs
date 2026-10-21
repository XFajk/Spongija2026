using Godot;

// Read-only stat lookup for one unit type. Shared between all stacks, so never modify it at runtime.
[GlobalClass]
public partial class Unit : Resource
{
    [Export]
    public string Name { get; set; } = "";

    [ExportGroup("Stats")]
    [Export]
    public int MaxHealth { get; set; } = 1;

    [Export]
    public int Strength { get; set; }

    [Export]
    public int Defense { get; set; }

    // Decides turn order and how many tiles the stack can move.
    [Export(PropertyHint.Range, "1,4")]
    public int SpeedTier { get; set; } = 1;

    // Starting mana of a new stack.
    [Export]
    public uint StartingMana { get; set; }

    [Export]
    public uint MaxMana { get; set; }

    // Tiles per side: 1 covers one tile, 2 covers a 2x2 square.
    [Export(PropertyHint.Range, "1,2")]
    public int Size { get; set; } = 1;

    // Whether this unit type can retaliate at all. Once-per-round state is Stack.Retaliated.
    [Export]
    public bool CanRetaliate { get; set; } = true;

    [ExportGroup("Abilities")]
    [Export]
    public MeleeAttackAbility? MeleeAttack { get; set; }

    [Export]
    public LongRangeAttackAbility? LongRangeAttack { get; set; }

    [Export]
    public Godot.Collections.Array<SpellAbility> Spells { get; set; } = new();

    [Export]
    public Godot.Collections.Array<PassiveAbility> Passives { get; set; } = new();

    [Export]
    public PackedScene? Model { get; set; }
}
