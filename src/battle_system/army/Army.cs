using System.Collections.Generic;

public class Army
{
    public const int MaxSlots = 5;

    public Hero? Hero;

    public List<Stack> Stacks { get; } = new();

    public bool IsDefeated => Stacks.TrueForAll(s => s.IsDead);

    public bool HasFreeSlot => Stacks.Count < MaxSlots;

    // Returns false when all slots are taken.
    public bool AddStack(Stack stack)
    {
        if (!HasFreeSlot)
            return false;
        stack.Army = this;
        Stacks.Add(stack);
        return true;
    }

    // Returns the removed stacks.
    public List<Stack> RemoveDeadStacks()
    {
        List<Stack> dead = Stacks.FindAll(s => s.IsDead);
        Stacks.RemoveAll(s => s.IsDead);
        return dead;
    }
}
