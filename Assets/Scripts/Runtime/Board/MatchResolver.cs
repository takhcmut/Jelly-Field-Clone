using System.Collections.Generic;
using UnityEngine;

public sealed class RegionRemoval
{
    public Vector2Int cell;
    public JellyColor color;
    public int regionId;
}

public sealed class MatchResolver
{
    public List<RegionRemoval> FindMatches(IReadOnlyDictionary<Vector2Int, JellyBlockData> board)
    {
        var matches = new List<RegionRemoval>();
        var directions = new[] { Vector2Int.right, Vector2Int.up };

        foreach (var pair in board)
        foreach (var direction in directions)
        {
            var otherCell = pair.Key + direction;
            if (!board.TryGetValue(otherCell, out var otherBlock)) continue;

            foreach (var first in pair.Value.regions)
            foreach (var second in otherBlock.regions)
            {
                if (first.color != second.color) continue;
                if (!TouchesSharedBoundary(first.rect, second.rect, direction)) continue;
                if (SharedLength(first.rect, second.rect, direction) <= .001f) continue;
                AddUnique(matches, pair.Key, first);
                AddUnique(matches, otherCell, second);
            }
        }

        return matches;
    }

    static void AddUnique(List<RegionRemoval> matches, Vector2Int cell, JellyRegionData region)
    {
        for (int i = 0; i < matches.Count; i++)
            if (matches[i].cell == cell && matches[i].regionId == region.id) return;
        matches.Add(new RegionRemoval { cell = cell, color = region.color, regionId = region.id });
    }

    static bool TouchesSharedBoundary(Rect first, Rect second, Vector2Int direction)
    {
        const float tolerance = .001f;
        if (direction == Vector2Int.right)
            return Mathf.Abs(first.xMax - 1f) <= tolerance && Mathf.Abs(second.xMin) <= tolerance;
        return Mathf.Abs(first.yMax - 1f) <= tolerance && Mathf.Abs(second.yMin) <= tolerance;
    }

    static float SharedLength(Rect first, Rect second, Vector2Int direction)
    {
        if (direction == Vector2Int.right)
            return Mathf.Max(0, Mathf.Min(first.yMax, second.yMax) - Mathf.Max(first.yMin, second.yMin));
        return Mathf.Max(0, Mathf.Min(first.xMax, second.xMax) - Mathf.Max(first.xMin, second.xMin));
    }
}
