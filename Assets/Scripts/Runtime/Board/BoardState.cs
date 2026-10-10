using System.Collections.Generic;
using UnityEngine;

public sealed class BoardState
{
    readonly Dictionary<Vector2Int, JellyBlockData> cells = new Dictionary<Vector2Int, JellyBlockData>();
    readonly HashSet<Vector2Int> validCells = new HashSet<Vector2Int>();

    public IReadOnlyDictionary<Vector2Int, JellyBlockData> Cells => cells;
    public IReadOnlyCollection<Vector2Int> ValidCells => validCells;

    public void Load(LevelData level)
    {
        cells.Clear();
        validCells.Clear();
        foreach (var coordinate in level.validCells) validCells.Add(coordinate);
        foreach (var cell in level.initialCells)
        {
            if (cell.block == null || !validCells.Contains(cell.coordinate)) continue;
            var block = cell.block.Clone();
            block.ApplyGeometry();
            cells[cell.coordinate] = block;
        }
    }

    public bool IsEmpty(Vector2Int coordinate) => validCells.Contains(coordinate) && !cells.ContainsKey(coordinate);

    public bool TryPlace(Vector2Int coordinate, JellyBlockData block)
    {
        if (!IsEmpty(coordinate) || block == null) return false;
        cells[coordinate] = block.Clone();
        cells[coordinate].ApplyGeometry();
        return true;
    }

    public void RemoveRegions(IReadOnlyList<RegionRemoval> removals)
    {
        foreach (var pair in cells)
        {
            pair.Value.regions.RemoveAll(region => Contains(removals, pair.Key, region.id));
            pair.Value.ApplyGeometry();
        }

        var emptyCells = new List<Vector2Int>();
        foreach (var pair in cells) if (pair.Value.regions.Count == 0) emptyCells.Add(pair.Key);
        foreach (var cell in emptyCells) cells.Remove(cell);
    }

    bool Contains(IReadOnlyList<RegionRemoval> removals, Vector2Int cell, int regionId)
    {
        for (int i = 0; i < removals.Count; i++)
            if (removals[i].cell == cell && removals[i].regionId == regionId) return true;
        return false;
    }
}
