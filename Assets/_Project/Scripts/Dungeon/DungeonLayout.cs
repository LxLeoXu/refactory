using System.Collections.Generic;
using UnityEngine;

// Pure layout data: connected tree, exact room count, reproducible without
// touching Unity's shared random stream. Order is also stable for a fixed seed.
public static class DungeonLayout
{
    public static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.down, Vector2Int.right, Vector2Int.left };
    public sealed class Plan
    {
        public readonly List<Vector2Int> cells = new List<Vector2Int>();
        public readonly Dictionary<Vector2Int,int> depth = new Dictionary<Vector2Int,int>();
        public Vector2Int boss;
    }
    public static Plan Build(int count, int seed)
    {
        var rng = new System.Random(seed);
        var result = new Plan();
        result.cells.Add(Vector2Int.zero); result.depth.Add(Vector2Int.zero,0);
        count = Mathf.Clamp(count,2,128);
        while(result.cells.Count < count)
        {
            var candidates = new List<Vector2Int>();
            var seen = new HashSet<Vector2Int>();
            foreach(var cell in result.cells)
                foreach(var direction in Directions)
                {
                    var next = cell + direction;
                    if(result.depth.ContainsKey(next) || !seen.Add(next)) continue;
                    int neighbours = 0;
                    foreach(var d in Directions) if(result.depth.ContainsKey(next+d)) neighbours++;
                    if(neighbours == 1) candidates.Add(next);
                }
            if(candidates.Count == 0) throw new System.InvalidOperationException("Dungeon frontier exhausted.");
            var chosen = candidates[rng.Next(candidates.Count)];
            foreach(var direction in Directions)
                if(result.depth.TryGetValue(chosen+direction,out int parentDepth))
                { result.depth.Add(chosen,parentDepth+1); break; }
            result.cells.Add(chosen);
            if(result.depth[chosen] > result.depth[result.boss]) result.boss = chosen;
        }
        return result;
    }
}
