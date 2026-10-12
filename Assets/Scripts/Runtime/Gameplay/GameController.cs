using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; }

    [Header("Level configuration")]
    [SerializeField] LevelDefinitionAsset[] levelAssets;
    [SerializeField] bool useRuntimeFallbackSamples = true;

    [Header("Scene references")]
    [SerializeField] Camera gameCamera;
    [SerializeField] Transform boardRoot;
    [SerializeField] Transform gridRoot;
    [SerializeField] Transform spawnRoot;

    [Header("Board visuals")]
    [SerializeField] GameObject jellyRegionPrefab;
    [SerializeField] GameObject cellPrefab;
    [SerializeField, Range(.5f, 1f)] float cellVisualScale = .82f;
    [SerializeField, Min(0f)] float dragDepthOffset = .6f;

    [Header("Camera")]
    [SerializeField, Range(0f, 45f)] float cameraElevationDegrees = 30f;
    [SerializeField, Min(10f)] float cameraDistance = 20f;

    [Header("Jelly materials")]
    [SerializeField] JellyMaterialPalette jellyMaterials = new JellyMaterialPalette();

    [Header("Cascade timing")]
    [SerializeField, Min(0f)] float cascadePopDuration = .3f;
    [SerializeField, Min(0f)] float cascadeBeforeRemovalDelay = .2f;
    [SerializeField, Min(0f)] float cascadeWaveDelay = .45f;

    public JellyGameState State { get; private set; }
    public int CurrentLevelIndex { get; private set; }

    readonly List<LevelData> levels = new List<LevelData>();
    readonly List<JellyBlockView> boardViews = new List<JellyBlockView>();
    readonly List<JellyBlockView> spawnViews = new List<JellyBlockView>();
    readonly BoardState board = new BoardState();
    readonly MatchResolver matchResolver = new MatchResolver();
    readonly ObjectiveTracker objectives = new ObjectiveTracker();
    readonly SpawnController spawns = new SpawnController();
    readonly GameHud ui = new GameHud();
    readonly BoardGridView gridView = new BoardGridView();

    bool resolving;
    int draggedSlot = -1;
    Vector3 dragOffset;
    Vector3 lastDragWorld;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindAnyObjectByType<GameController>() == null)
            new GameObject("GameController").AddComponent<GameController>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Application.targetFrameRate = 60;
        Screen.orientation = ScreenOrientation.Portrait;
        LoadLevelDefinitions();
        EnsureSceneReferences();
        ui.Build(Retry, NextLevel);
        LoadLevel(0);
    }

    void LoadLevelDefinitions()
    {
        levels.Clear();
        if (levelAssets != null)
            foreach (var asset in levelAssets)
                if (asset != null) levels.Add(asset.CreateRuntimeData());

        if (levels.Count == 0 && useRuntimeFallbackSamples)
            levels.AddRange(SampleLevels.CreateAll());

        if (levels.Count == 0)
            Debug.LogError("Jelly Field has no level assets assigned and runtime fallback samples are disabled.");
    }

    void EnsureSceneReferences()
    {
        if (gameCamera == null) gameCamera = Camera.main;
        if (gameCamera == null)
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            gameCamera = cameraObject.AddComponent<Camera>();
        }
        gameCamera.orthographic = true;
        gameCamera.backgroundColor = new Color(.08f, .12f, .16f);
        gameCamera.clearFlags = CameraClearFlags.SolidColor;

        if (boardRoot == null) boardRoot = CreateChildRoot("Board");
        if (gridRoot == null) gridRoot = CreateChildRoot("Grid");
        if (spawnRoot == null) spawnRoot = CreateChildRoot("Spawn Area");
    }

    Transform CreateChildRoot(string name)
    {
        var root = new GameObject(name).transform;
        root.SetParent(transform, false);
        return root;
    }

    public void LoadLevel(int index)
    {
        if (levels.Count == 0) return;
        StopAllCoroutines();
        resolving = false;
        draggedSlot = -1;
        CurrentLevelIndex = Mathf.Clamp(index, 0, levels.Count - 1);
        State = JellyGameState.Playing;

        var level = levels[CurrentLevelIndex];
        board.Load(level);
        objectives.Load(level);
        spawns.Load(level);
        gridView.Build(board.ValidCells, gridRoot, cellPrefab, cellVisualScale);
        RebuildViews();
        ui.ShowLevel(level, CurrentLevelIndex, levels.Count, objectives);
        ui.SetMessage("Drag a jelly into an empty cell");
        ui.SetCompletionVisible(false);
        FrameCamera();
    }

    public void Retry() => LoadLevel(CurrentLevelIndex);

    public void NextLevel()
    {
        if (CurrentLevelIndex + 1 >= levels.Count)
        {
            State = JellyGameState.AllLevelsCompleted;
            ui.SetCompletionVisible(false);
            ui.SetMessage("ALL LEVELS COMPLETE");
            return;
        }
        LoadLevel(CurrentLevelIndex + 1);
    }

    void RebuildViews()
    {
        ClearViews(boardViews);
        ClearViews(spawnViews);

        foreach (var pair in board.Cells)
        {
            var view = JellyBlockView.Create(pair.Value, boardRoot, CellWorld(pair.Key), pair.Key, -1, jellyRegionPrefab, jellyMaterials);
            boardViews.Add(view);
        }

        for (int slot = 0; slot < spawns.CurrentSlots.Count; slot++)
        {
            var view = JellyBlockView.Create(spawns.Get(slot), spawnRoot, SpawnWorld(slot), Vector2Int.zero, slot, jellyRegionPrefab, jellyMaterials);
            spawnViews.Add(view);
        }
    }

    void ClearViews(List<JellyBlockView> views)
    {
        foreach (var view in views) if (view != null) Destroy(view.gameObject);
        views.Clear();
    }

    Vector3 CellWorld(Vector2Int coordinate) => new Vector3(coordinate.x, coordinate.y + 2.2f, 0);
    Vector3 SpawnWorld(int slot) => new Vector3(slot * 1.7f + .3f, -.2f, 0);

    void FrameCamera()
    {
        var level = levels[CurrentLevelIndex];
        var maxX = 0;
        var maxY = 0;
        foreach (var cell in level.validCells) { maxX = Mathf.Max(maxX, cell.x); maxY = Mathf.Max(maxY, cell.y); }
        var target = new Vector3(maxX * .5f, (maxY + 1.2f) * .5f + .7f, 0);
        var elevationRadians = cameraElevationDegrees * Mathf.Deg2Rad;
        var cameraOffset = new Vector3(0, -Mathf.Sin(elevationRadians) * cameraDistance, -Mathf.Cos(elevationRadians) * cameraDistance);
        gameCamera.transform.position = target + cameraOffset;
        gameCamera.transform.rotation = Quaternion.LookRotation(target - gameCamera.transform.position, Vector3.up);
        gameCamera.orthographicSize = Mathf.Max(4.6f, (maxY + 3.7f) * .55f);
        gameCamera.aspect = Mathf.Max(.45f, (float)Screen.width / Screen.height);
    }

    void Update()
    {
        if (resolving || State != JellyGameState.Playing) return;
        if (Input.GetMouseButtonDown(0)) BeginDrag(Input.mousePosition);
        if (Input.GetMouseButton(0) && draggedSlot >= 0) ContinueDrag(Input.mousePosition);
        if (Input.GetMouseButtonUp(0) && draggedSlot >= 0) EndDrag(Input.mousePosition);
        if (Input.touchCount > 0) HandleTouch(Input.GetTouch(0));
    }

    void HandleTouch(Touch touch)
    {
        if (touch.phase == TouchPhase.Began) BeginDrag(touch.position);
        else if (touch.phase == TouchPhase.Moved && draggedSlot >= 0) ContinueDrag(touch.position);
        else if ((touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) && draggedSlot >= 0) EndDrag(touch.position);
    }

    bool TryScreenToBoard(Vector2 screen, out Vector3 world)
    {
        var ray = gameCamera.ScreenPointToRay(screen);
        var boardPlane = new Plane(Vector3.forward, Vector3.zero);
        if (boardPlane.Raycast(ray, out var distance))
        {
            world = ray.GetPoint(distance);
            return true;
        }

        world = default;
        return false;
    }

    void BeginDrag(Vector3 screen)
    {
        if (!TryScreenToBoard(screen, out var world)) return;
        for (int slot = 0; slot < spawnViews.Count; slot++)
        {
            if (Vector2.Distance(world, spawnViews[slot].transform.position) < .9f)
            {
                draggedSlot = slot;
                dragOffset = spawnViews[slot].transform.position - world;
                lastDragWorld = world;
                spawnViews[slot].SetSelected(true);
                return;
            }
        }
    }

    void ContinueDrag(Vector3 screen)
    {
        if (!TryScreenToBoard(screen, out var world)) return;
        var dragVelocity = (world - lastDragWorld) / Mathf.Max(Time.deltaTime, .0001f);
        var dragPosition = world + dragOffset;
        dragPosition.z = -dragDepthOffset;
        spawnViews[draggedSlot].transform.position = dragPosition;
        spawnViews[draggedSlot].SetDragging(true);
        spawnViews[draggedSlot].SetDragVelocity(dragVelocity);
        lastDragWorld = world;
    }

    void EndDrag(Vector3 screen)
    {
        var slot = draggedSlot;
        draggedSlot = -1;
        spawnViews[slot].SetSelected(false);
        spawnViews[slot].SetDragging(false);
        if (!TryScreenToBoard(screen, out var world))
        {
            StartCoroutine(ReturnSpawn(slot));
            return;
        }
        var coordinate = new Vector2Int(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.y - 2.2f));

        if (!board.IsEmpty(coordinate))
        {
            StartCoroutine(ReturnSpawn(slot));
            return;
        }

        if (!board.TryPlace(coordinate, spawns.Get(slot)))
        {
            StartCoroutine(ReturnSpawn(slot));
            return;
        }

        spawns.Consume(slot);
        RebuildViews();
        StartCoroutine(ResolveCascade());
    }

    IEnumerator ReturnSpawn(int slot)
    {
        var view = spawnViews[slot];
        var start = view.transform.position;
        for (float t = 0; t < 1; t += Time.deltaTime * 5)
        {
            view.transform.position = Vector3.Lerp(start, SpawnWorld(slot), t);
            yield return null;
        }
        view.transform.position = SpawnWorld(slot);
    }

    IEnumerator ResolveCascade()
    {
        resolving = true;
        State = JellyGameState.ResolvingCascade;
        while (true)
        {
            var removals = matchResolver.FindMatches(board.Cells);
            if (removals.Count == 0) break;
            var matchedCells = new HashSet<Vector2Int>();
            foreach (var removal in removals) matchedCells.Add(removal.cell);
            foreach (var view in boardViews)
                if (matchedCells.Contains(view.Cell)) view.PlayPop(cascadePopDuration);
            yield return new WaitForSeconds(cascadePopDuration + cascadeBeforeRemovalDelay);
            objectives.Count(removals);
            board.RemoveRegions(removals);
            RebuildViews();
            ui.ShowLevel(levels[CurrentLevelIndex], CurrentLevelIndex, levels.Count, objectives);
            yield return new WaitForSeconds(cascadeWaveDelay);
        }

        resolving = false;
        State = objectives.IsComplete() ? JellyGameState.LevelCompleted : JellyGameState.Playing;
        if (State == JellyGameState.LevelCompleted)
        {
            ui.SetMessage("Every objective is complete");
            ui.SetCompletionVisible(true);
        }
        else if (!HasEmptyCell()) ui.SetMessage("No moves remain - retry the level");
    }

    bool HasEmptyCell()
    {
        foreach (var cell in board.ValidCells) if (board.IsEmpty(cell)) return true;
        return false;
    }
}
