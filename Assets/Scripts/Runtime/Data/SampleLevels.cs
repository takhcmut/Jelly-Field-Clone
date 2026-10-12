using System.Collections.Generic;
using UnityEngine;

public static class SampleLevels
{
    static JellyBlockData Block(BlockLayout layout, params JellyColor[] colors)
    {
        var result = new JellyBlockData { layout = layout };
        for (int i = 0; i < colors.Length; i++) result.regions.Add(new JellyRegionData(i, colors[i]));
        result.ApplyGeometry();
        return result;
    }

    static LevelData Make(string name, int width, int height, int count, int slots, int seed)
    {
        var level = new LevelData { name = name, spawnSlotCount = slots, fallbackSeed = seed };
        for (int y = 0; y < height && level.validCells.Count < count; y++)
            for (int x = 0; x < width && level.validCells.Count < count; x++) level.validCells.Add(new Vector2Int(x, y));

        level.objectives.Add(new ObjectiveData { color = JellyColor.Yellow, required = 4 });
        level.objectives.Add(new ObjectiveData { color = JellyColor.Blue, required = 1 });
        level.objectives.Add(new ObjectiveData { color = JellyColor.Purple, required = 3 });

        var placements = new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(0, 1), new Vector2Int(2, 1) };
        var blocks = new[]
        {
            Block(BlockLayout.TwoHorizontal, JellyColor.Yellow, JellyColor.Blue),
            Block(BlockLayout.ThreeBottom, JellyColor.Purple, JellyColor.Yellow, JellyColor.Green),
            Block(BlockLayout.Four, JellyColor.Yellow, JellyColor.Purple, JellyColor.Pink, JellyColor.Blue),
            Block(BlockLayout.TwoVertical, JellyColor.Purple, JellyColor.Green),
            Block(BlockLayout.One, JellyColor.Yellow)
        };

        for (int i = 0; i < placements.Length && i < blocks.Length; i++)
            level.initialCells.Add(new CellData { coordinate = placements[i], block = blocks[i] });

        level.initialSpawns.Add(Block(BlockLayout.TwoHorizontal, JellyColor.Yellow, JellyColor.Blue));
        if (slots > 1) level.initialSpawns.Add(Block(BlockLayout.ThreeRight, JellyColor.Purple, JellyColor.Yellow, JellyColor.Green));
        level.spawnSequence.Add(Block(BlockLayout.Four, JellyColor.Yellow, JellyColor.Purple, JellyColor.Blue, JellyColor.Pink));
        level.spawnSequence.Add(Block(BlockLayout.TwoVertical, JellyColor.Yellow, JellyColor.Yellow));
        level.spawnSequence.Add(Block(BlockLayout.One, JellyColor.Purple));
        level.spawnSequence.Add(Block(BlockLayout.ThreeTop, JellyColor.Blue, JellyColor.Green, JellyColor.Yellow));
        return level;
    }

    public static List<LevelData> CreateAll()
    {
        return new List<LevelData>
        {
            Make("Level 1", 4, 3, 11, 1, 101),
            Make("Level 2", 4, 3, 12, 2, 202),
            Make("Level 3", 5, 3, 13, 2, 303)
        };
    }
}
