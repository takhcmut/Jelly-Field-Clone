using System;
using System.Collections.Generic;
using UnityEngine;

public enum JellyColor { Yellow, Blue, Red, Green, Pink }
public enum BlockLayout { One, TwoHorizontal, TwoVertical, ThreeLeft, ThreeRight, ThreeTop, ThreeBottom, Four }
public enum JellyGameState { Playing, ResolvingCascade, LevelCompleted, AllLevelsCompleted }

[Serializable]
public class JellyRegionData
{
    public int id;
    public JellyColor color;
    [NonSerialized] public Rect rect;
    public JellyRegionData() { }
    public JellyRegionData(int id, JellyColor color) { this.id = id; this.color = color; }
    public JellyRegionData Clone() => new JellyRegionData(id, color);
}

[Serializable]
public class JellyBlockData
{
    public BlockLayout layout;
    public List<JellyRegionData> regions = new List<JellyRegionData>();

    public JellyBlockData Clone()
    {
        var copy = new JellyBlockData { layout = layout };
        foreach (var region in regions) copy.regions.Add(region.Clone());
        return copy;
    }

    public void ApplyGeometry()
    {
        int count = regions.Count;
        if (count == 0) return;
        if (count == 1) layout = BlockLayout.One;
        else if (count == 2 && layout != BlockLayout.TwoHorizontal && layout != BlockLayout.TwoVertical) layout = BlockLayout.TwoHorizontal;
        else if (count == 3 && (layout == BlockLayout.One || layout == BlockLayout.Four || (int)layout < (int)BlockLayout.ThreeLeft)) layout = BlockLayout.ThreeBottom;
        else if (count == 4) layout = BlockLayout.Four;
        var rects = GeometryFor(layout);
        for (int i = 0; i < regions.Count; i++) regions[i].rect = rects[Mathf.Min(i, rects.Count - 1)];
    }

    public static List<Rect> GeometryFor(BlockLayout layout)
    {
        var half = new Rect(0, 0, .5f, 1);
        switch (layout)
        {
            case BlockLayout.TwoVertical: return new List<Rect> { new Rect(0, 0, 1, .5f), new Rect(0, .5f, 1, .5f) };
            case BlockLayout.ThreeLeft: return new List<Rect> { new Rect(0, 0, .5f, 1), new Rect(.5f, .5f, .5f, .5f), new Rect(.5f, 0, .5f, .5f) };
            case BlockLayout.ThreeRight: return new List<Rect> { new Rect(.5f, 0, .5f, 1), new Rect(0, .5f, .5f, .5f), new Rect(0, 0, .5f, .5f) };
            case BlockLayout.ThreeTop: return new List<Rect> { new Rect(0, .5f, 1, .5f), new Rect(0, 0, .5f, .5f), new Rect(.5f, 0, .5f, .5f) };
            case BlockLayout.ThreeBottom: return new List<Rect> { new Rect(0, 0, 1, .5f), new Rect(0, .5f, .5f, .5f), new Rect(.5f, .5f, .5f, .5f) };
            case BlockLayout.Four: return new List<Rect> { new Rect(0, 0, .5f, .5f), new Rect(.5f, 0, .5f, .5f), new Rect(0, .5f, .5f, .5f), new Rect(.5f, .5f, .5f, .5f) };
            case BlockLayout.TwoHorizontal: return new List<Rect> { half, new Rect(.5f, 0, .5f, 1) };
            default: return new List<Rect> { new Rect(0, 0, 1, 1) };
        }
    }
}

[Serializable]
public class ObjectiveData
{
    public JellyColor color;
    public int required;
    [NonSerialized] public int progress;
}

[Serializable]
public class CellData
{
    public Vector2Int coordinate;
    public JellyBlockData block;
    public CellData Clone() => new CellData { coordinate = coordinate, block = block == null ? null : block.Clone() };
}

[Serializable]
public class LevelData
{
    public string name;
    public List<Vector2Int> validCells = new List<Vector2Int>();
    public List<CellData> initialCells = new List<CellData>();
    public List<ObjectiveData> objectives = new List<ObjectiveData>();
    public int spawnSlotCount;
    public List<JellyBlockData> initialSpawns = new List<JellyBlockData>();
    public List<JellyBlockData> spawnSequence = new List<JellyBlockData>();
    public int fallbackSeed;

    public LevelData Clone()
    {
        var copy = new LevelData { name = name, spawnSlotCount = spawnSlotCount, fallbackSeed = fallbackSeed };
        copy.validCells.AddRange(validCells);
        foreach (var cell in initialCells) copy.initialCells.Add(cell.Clone());
        foreach (var objective in objectives) copy.objectives.Add(new ObjectiveData { color = objective.color, required = objective.required });
        foreach (var spawn in initialSpawns) copy.initialSpawns.Add(spawn.Clone());
        foreach (var spawn in spawnSequence) copy.spawnSequence.Add(spawn.Clone());
        return copy;
    }
}
