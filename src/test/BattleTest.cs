using Godot;

// Test entry point: run scenes/battle_test.tscn (F6) to jump straight into a battle with these armies.
public partial class BattleTest : Node
{
    public override void _Ready()
    {
        var pilgrim = new Unit { Name = "Pilgrim", MaxHealth = 5, Strength = 1, Defense = 1, SpeedTier = 4 };
        var warrior = new Unit { Name = "Warrior", MaxHealth = 15, Strength = 4, Defense = 4, SpeedTier = 5 };
        var cavalry = new Unit { Name = "Cavalry", Size = 2, MaxHealth = 40, Strength = 8, Defense = 5, SpeedTier = 7 };
        var wolf = new Unit { Name = "Wolf", MaxHealth = 10, Strength = 3, Defense = 2, SpeedTier = 6 };
        var spider = new Unit { Name = "Giant Spider", Size = 2, MaxHealth = 35, Strength = 6, Defense = 6, SpeedTier = 3 };

        var playerArmy = new Army { Hero = new Hero { Strength = 2, Defense = 2, Knowledge = 5 } };
        playerArmy.AddStack(new Stack(pilgrim, 30));
        playerArmy.AddStack(new Stack(warrior, 5));
        playerArmy.AddStack(new Stack(cavalry, 3));

        var computerArmy = new Army();
        computerArmy.AddStack(new Stack(wolf, 12));
        computerArmy.AddStack(new Stack(wolf, 8));
        computerArmy.AddStack(new Stack(spider, 2));

        SceneTransitions.EnterArena(GetTree(), playerArmy, computerArmy);
    }
}
