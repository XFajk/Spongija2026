using Godot;

public static class SceneTransitions
{
    public const string ArenaScene = "uid://dlub2l8df1s83";

    // Switches to the arena with both armies stored on it, ready for the placement phase.
    public static void EnterArena(SceneTree tree, Army playerArmy, Army computerArmy)
    {
        Arena arena = GD.Load<PackedScene>(ArenaScene).Instantiate<Arena>();
        arena.SetArmies(playerArmy, computerArmy);
        // Deferred: ChangeSceneToNode removes the current scene right away, which fails if we're called from its _Ready.
        tree.CallDeferred(SceneTree.MethodName.ChangeSceneToNode, arena);
    }
}
