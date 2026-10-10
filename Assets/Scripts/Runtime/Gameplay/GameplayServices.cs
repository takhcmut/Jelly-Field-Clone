using System.Collections.Generic;
using UnityEngine;

public sealed class ObjectiveTracker
{
    readonly Dictionary<JellyColor, int> progress = new Dictionary<JellyColor, int>();
    List<ObjectiveData> objectives = new List<ObjectiveData>();

    public void Load(LevelData level)
    {
        progress.Clear();
        objectives = level.objectives;
        foreach (var objective in objectives) progress[objective.color] = 0;
    }

    public void Count(IReadOnlyList<RegionRemoval> removals)
    {
        foreach (var removal in removals)
            if (progress.ContainsKey(removal.color)) progress[removal.color]++;
    }

    public bool IsComplete()
    {
        foreach (var objective in objectives)
            if (!progress.ContainsKey(objective.color) || progress[objective.color] < objective.required) return false;
        return true;
    }

    public int GetProgress(JellyColor color) => progress.TryGetValue(color, out var value) ? value : 0;
}

public sealed class SpawnController
{
    readonly List<JellyBlockData> currentSlots = new List<JellyBlockData>();
    LevelData level;
    int sequenceIndex;

    public IReadOnlyList<JellyBlockData> CurrentSlots => currentSlots;

    public void Load(LevelData source)
    {
        level = source;
        sequenceIndex = 0;
        currentSlots.Clear();
        for (int i = 0; i < level.spawnSlotCount; i++)
        {
            var block = i < level.initialSpawns.Count ? level.initialSpawns[i] : NextBlock();
            currentSlots.Add(block.Clone());
        }
    }

    public JellyBlockData Get(int slot) => slot >= 0 && slot < currentSlots.Count ? currentSlots[slot].Clone() : null;

    public bool Consume(int slot)
    {
        if (slot < 0 || slot >= currentSlots.Count) return false;
        currentSlots[slot] = NextBlock();
        return true;
    }

    JellyBlockData NextBlock()
    {
        if (level.spawnSequence.Count > 0)
        {
            var block = level.spawnSequence[sequenceIndex % level.spawnSequence.Count].Clone();
            sequenceIndex++;
            return block;
        }

        if (level.initialSpawns.Count == 0) return new JellyBlockData();
        var fallback = level.initialSpawns[sequenceIndex % level.initialSpawns.Count].Clone();
        sequenceIndex++;
        return fallback;
    }
}
