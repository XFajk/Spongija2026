using Godot;
using System.Threading.Tasks;

public static class SceneTransitions
{
    public const string ArenaScene = "uid://dlub2l8df1s83";

    // Seconds the screen takes to fade to black before the arena replaces the scene; the arena fades back in itself
    // (Arena.FadeInDuration)
    public const float EnterArenaFadeOut = 0.8f;

    // Fades to black, then switches to the arena with both armies stored on it, ready for the placement phase.
    public static async Task EnterArena(SceneTree tree, Army playerArmy, Army computerArmy)
    {
        await ScreenFade.FadeOut(tree, EnterArenaFadeOut);
        Arena arena = GD.Load<PackedScene>(ArenaScene).Instantiate<Arena>();
        arena.SetArmies(playerArmy, computerArmy);
        // Deferred: ChangeSceneToNode removes the current scene right away, which fails if we're called from its _Ready.
        tree.CallDeferred(SceneTree.MethodName.ChangeSceneToNode, arena);
    }
}
