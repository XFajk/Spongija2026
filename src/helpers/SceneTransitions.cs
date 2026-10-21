using Godot;

public static class SceneTransitions
{
    public const string ArenaScene = "uid://dlub2l8df1s83";

    // Switches to the arena with both armies stored on it, ready for the placement phase.
    public static void EnterArena(SceneTree tree, Army playerArmy, Army computerArmy)
    {
        Arena arena = GD.Load<PackedScene>(ArenaScene).Instantiate<Arena>();
        arena.SetArmies(playerArmy, computerArmy);
        ChangeSceneToNode(tree, arena);
    }

    // Like SceneTree.ChangeSceneToPacked, but with an instance that was set up before entering the tree.
    // Deferred, so it's safe to call from _Ready.
    public static void ChangeSceneToNode(SceneTree tree, Node scene)
    {
        Callable.From(() =>
        {
            Node? old = tree.CurrentScene;
            tree.Root.AddChild(scene);
            tree.CurrentScene = scene;
            old?.QueueFree();
        }).CallDeferred();
    }
}
