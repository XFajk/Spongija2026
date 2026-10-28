using Godot;
using System.Collections.Generic;

public static class NodeExtensions
{
    // Finds the nearest descendant of type T anywhere in the subtree (breadth-first, so shallower nodes win).
    // Unlike Node.FindChild, it matches by C# type and ignores ownership, so it also searches inside instanced sub-scenes.
    public static T? FindDescendant<T>(this Node root) where T : class
    {
        var queue = new Queue<Node>(root.GetChildren());
        while (queue.Count > 0)
        {
            Node node = queue.Dequeue();
            if (node is T match)
                return match;
            foreach (Node child in node.GetChildren())
                queue.Enqueue(child);
        }
        return null;
    }

    // Every descendant of type T in the subtree, in breadth-first order.
    public static List<T> FindDescendants<T>(this Node root) where T : class
    {
        var result = new List<T>();
        var queue = new Queue<Node>(root.GetChildren());
        while (queue.Count > 0)
        {
            Node node = queue.Dequeue();
            if (node is T match)
                result.Add(match);
            foreach (Node child in node.GetChildren())
                queue.Enqueue(child);
        }
        return result;
    }
}
