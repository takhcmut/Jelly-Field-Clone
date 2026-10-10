using UnityEngine;

[CreateAssetMenu(fileName = "JellyFieldLevel", menuName = "Jelly Field/Level Data")]
public class LevelDefinitionAsset : ScriptableObject
{
    public LevelData level = new LevelData();

    public LevelData CreateRuntimeData()
    {
        return level == null ? new LevelData() : level.Clone();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (level == null) level = new LevelData();
        if (level.spawnSlotCount < 1) level.spawnSlotCount = 1;
    }
#endif
}
