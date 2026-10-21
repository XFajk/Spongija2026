using Godot;
using System.Collections.Generic;

// Result of Battle.GetReachability: every anchor a stack can move to this turn,
// plus where each one was reached from, so a path can be rebuilt without searching again.
// Only valid until the board changes (a stack moves or dies).
public class Reachability
{
    public Stack Stack { get; }

    // The stack's anchor when the search ran.
    public Vector2I Start { get; }

    // Reachable anchor -> the anchor it was reached from.
    private readonly Dictionary<Vector2I, Vector2I> _cameFrom;

    public Reachability(Stack stack, Vector2I start, Dictionary<Vector2I, Vector2I> cameFrom)
    {
        Stack = stack;
        Start = start;
        _cameFrom = cameFrom;
    }

    // Anchors the stack can move to. Excludes Start.
    public IEnumerable<Vector2I> Anchors => _cameFrom.Keys;

    public bool CanReach(Vector2I anchor) => _cameFrom.ContainsKey(anchor);

    // Anchors to walk through in order, ending with the target and excluding Start.
    // Always a shortest path, because the search reaches every anchor first by a shortest route.
    // Empty if the target isn't reachable.
    public List<Vector2I> GetPath(Vector2I target)
    {
        var path = new List<Vector2I>();
        if (!CanReach(target))
            return path;

        for (Vector2I anchor = target; anchor != Start; anchor = _cameFrom[anchor])
            path.Add(anchor);
        path.Reverse();
        return path;
    }

    // Every tile covered by some reachable anchor, for highlighting.
    public HashSet<Vector2I> GetTiles()
    {
        var tiles = new HashSet<Vector2I>();
        foreach (Vector2I anchor in Anchors)
            tiles.UnionWith(Stack.GetTilesAt(anchor));
        return tiles;
    }

    // Reachable anchors whose footprint contains the tile (at most 4 for a size 2 stack), for hover.
    public List<Vector2I> GetAnchorsCovering(Vector2I tile)
    {
        var anchors = new List<Vector2I>();
        for (int x = 0; x < Stack.Size; x++)
            for (int y = 0; y < Stack.Size; y++)
            {
                Vector2I anchor = tile - new Vector2I(x, y);
                if (CanReach(anchor))
                    anchors.Add(anchor);
            }
        return anchors;
    }
}
