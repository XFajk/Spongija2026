using System.Threading.Tasks;

public interface IAbility
{
    // Does the ability's game logic first, then awaits its animations.
    // The turn waits for the returned task before moving on.
    Task Use(Stack user, Stack target, Arena arena);
}
