#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public static class JellyFieldSceneBuilder
{
    [MenuItem("Tools/JellyField/Build Demo Scene")]
    public static void BuildDemoScene()
    {
        var assets = CreateSampleLevelAssetsInternal();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var game = new GameObject("JellyFieldGame");
        game.AddComponent<GameController>();
        var board = new GameObject("Board").transform;
        board.SetParent(game.transform, false);
        var grid = new GameObject("Grid").transform;
        grid.SetParent(game.transform, false);
        var spawn = new GameObject("Spawn Area").transform;
        spawn.SetParent(game.transform, false);
        var camera = new GameObject("Main Camera");
        camera.tag = "MainCamera";
        camera.AddComponent<Camera>();
        camera.transform.position = new Vector3(1.5f, 2.5f, -20f);
        camera.transform.rotation = Quaternion.identity;
        var serializedGame = new SerializedObject(game.GetComponent<GameController>());
        serializedGame.FindProperty("levelAssets").arraySize = assets.Count;
        for (int i = 0; i < assets.Count; i++) serializedGame.FindProperty("levelAssets").GetArrayElementAtIndex(i).objectReferenceValue = assets[i];
        serializedGame.FindProperty("gameCamera").objectReferenceValue = camera.GetComponent<Camera>();
        serializedGame.FindProperty("boardRoot").objectReferenceValue = board;
        serializedGame.FindProperty("gridRoot").objectReferenceValue = grid;
        serializedGame.FindProperty("spawnRoot").objectReferenceValue = spawn;
        serializedGame.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainScene.unity");
        Selection.activeGameObject = game;
        Debug.Log("Jelly Field demo scene created at Assets/Scenes/MainScene.unity. Press Play to run the provisional three-level MVP.");
    }

    [MenuItem("Tools/JellyField/Create Sample Level Assets")]
    public static void CreateSampleLevelAssets()
    {
        CreateSampleLevelAssetsInternal();
    }

    static List<LevelDefinitionAsset> CreateSampleLevelAssetsInternal()
    {
        const string root = "Assets/Data";
        const string folder = "Assets/Data/JellyField";
        const string levelFolder = "Assets/Data/JellyField/Levels";
        EnsureFolder("Assets", "Data");
        EnsureFolder(root, "JellyField");
        EnsureFolder(folder, "Levels");

        var result = new List<LevelDefinitionAsset>();
        foreach (var sample in SampleLevels.CreateAll())
        {
            var path = levelFolder + "/" + sample.name.Replace(" ", "_") + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<LevelDefinitionAsset>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<LevelDefinitionAsset>();
                AssetDatabase.CreateAsset(asset, path);
            }
            asset.level = sample;
            EditorUtility.SetDirty(asset);
            result.Add(asset);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Created or refreshed provisional Jelly Field level assets in Assets/Data/JellyField/Levels.");
        return result;
    }

    static void EnsureFolder(string parent, string name)
    {
        var path = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
    }
}
#endif
