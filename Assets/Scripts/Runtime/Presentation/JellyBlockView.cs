using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class JellyMaterialPalette
{
    public Material yellow;
    public Material blue;
    public Material purple;
    public Material green;
    public Material pink;

    public Material Get(JellyColor color)
    {
        switch (color)
        {
            case JellyColor.Yellow: return yellow;
            case JellyColor.Blue: return blue;
            case JellyColor.Purple: return purple;
            case JellyColor.Green: return green;
            case JellyColor.Pink: return pink;
            default: return null;
        }
    }
}

public class JellyBlockView : MonoBehaviour
{
    sealed class RegionVisual
    {
        public Transform root;
        public Transform visual;
        public Rect rect;
    }

    public JellyBlockData Data { get; set; }
    public Vector2Int Cell { get; private set; }
    readonly Dictionary<int, RegionVisual> regionVisuals = new Dictionary<int, RegionVisual>();
    bool isDragging;

    public static JellyBlockView Create(JellyBlockData data, Transform parent, Vector3 position, Vector2Int cell, int spawnSlot, GameObject regionPrefab = null, JellyMaterialPalette materialPalette = null, float regionFillScale = 1.02f)
    {
        var objectRoot = new GameObject(spawnSlot >= 0 ? "Spawn Jelly " + spawnSlot : "Jelly " + cell);
        objectRoot.transform.SetParent(parent, false);
        objectRoot.transform.localPosition = position;
        var view = objectRoot.AddComponent<JellyBlockView>();
        view.Data = data;
        view.Cell = cell;
        view.Build(regionPrefab, materialPalette, regionFillScale);
        return view;
    }

//     void Build(GameObject regionPrefab, JellyMaterialPalette materialPalette, float regionFillScale)
//     {
//         Data.ApplyGeometry();
//         ClearRuntimeVisuals();
//         foreach (var region in Data.regions)
//         {
//             var regionRoot = new GameObject("Region " + region.id);
//             regionRoot.transform.SetParent(transform, false);
//             regionRoot.transform.localPosition = RegionCenter(region.rect);
//             regionRoot.transform.localScale = Vector3.one;

//             var visual = regionPrefab == null ? GameObject.CreatePrimitive(PrimitiveType.Cube) : Instantiate(regionPrefab);
//             visual.transform.SetParent(regionRoot.transform, false);
//             visual.transform.localPosition = Vector3.zero;
//             var prefabScale = visual.transform.localScale;
//             visual.transform.localScale = Vector3.Scale(prefabScale, new Vector3(region.rect.width * regionFillScale, region.rect.height * regionFillScale, .32f));
//             CenterVisual(visual, regionRoot.transform);

//             var material = materialPalette == null ? null : materialPalette.Get(region.color);
//             if (material != null)
//                 foreach (var renderer in visual.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial = material;

//             regionVisuals[region.id] = new RegionVisual
// {
//     root = regionRoot.transform,
//     visual = visual.transform,
//     rect = region.rect
// };
//         }
//     }


void Build(
    GameObject regionPrefab,
    JellyMaterialPalette materialPalette,
    float regionFillScale)
{
    Data.ApplyGeometry();
    ClearRuntimeVisuals();

    foreach (var region in Data.regions)
    {
        var regionRoot = new GameObject("Region " + region.id);
        regionRoot.transform.SetParent(transform, false);
        regionRoot.transform.localPosition = RegionCenter(region.rect);
        regionRoot.transform.localRotation = Quaternion.identity;
        regionRoot.transform.localScale = Vector3.one;

        var visual = regionPrefab == null
            ? GameObject.CreatePrimitive(PrimitiveType.Cube)
            : Instantiate(regionPrefab);

        visual.transform.SetParent(regionRoot.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;

        // Không nhân trực tiếp với prefabScale nữa.
        // Kích thước thực tế sẽ được căn theo Renderer bounds.
        visual.transform.localScale = Vector3.one;

        FitVisualToRegion(
            visual,
            regionRoot.transform,
            region.rect,
            regionFillScale
        );

        var material = materialPalette == null
            ? null
            : materialPalette.Get(region.color);

        if (material != null)
        {
            foreach (var renderer in
                     visual.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterial = material;
            }
        }

        regionVisuals[region.id] = new RegionVisual
        {
            root = regionRoot.transform,
            visual = visual.transform,
            rect = region.rect
        };
    }
}


static void FitVisualToRegion(
    GameObject visual,
    Transform anchor,
    Rect rect,
    float fillScale)
{
    var renderers = visual.GetComponentsInChildren<Renderer>();

    // Trường hợp không có Renderer để đo kích thước.
    if (renderers.Length == 0)
    {
        visual.transform.localScale = new Vector3(
            rect.width * fillScale,
            rect.height * fillScale,
            0.32f
        );

        return;
    }

    // Đưa tâm hình học của visual về tâm region.
    CenterVisual(visual, anchor);

    var bounds = renderers[0].bounds;

    for (int i = 1; i < renderers.Length; i++)
        bounds.Encapsulate(renderers[i].bounds);

    var sourceSize = bounds.size;

    var targetSize = new Vector3(
        rect.width * fillScale,
        rect.height * fillScale,
        0.32f
    );

    // Tính scale dựa trên kích thước mesh thực tế,
    // không dựa vào scale gốc của prefab.
    float scaleX = sourceSize.x > 0.0001f
        ? targetSize.x / sourceSize.x
        : targetSize.x;

    float scaleY = sourceSize.y > 0.0001f
        ? targetSize.y / sourceSize.y
        : targetSize.y;

    float scaleZ = sourceSize.z > 0.0001f
        ? targetSize.z / sourceSize.z
        : 1f;

    visual.transform.localScale = Vector3.Scale(
        visual.transform.localScale,
        new Vector3(scaleX, scaleY, scaleZ)
    );

    // Căn lại tâm sau khi thay đổi kích thước.
    CenterVisual(visual, anchor);
}

    public void BeginRegionRemoval(IReadOnlyCollection<int> removedIds, JellyBlockData resultingBlock, float duration)
    {
        StopAllCoroutines();
        StartCoroutine(RegionRemovalRoutine(removedIds, resultingBlock, duration));
    }

    public void CommitResolvedData(JellyBlockData resultingBlock)
    {
        Data = resultingBlock == null ? null : resultingBlock.Clone();
        if (Data == null || Data.regions.Count == 0)
            Destroy(gameObject);
    }

    // IEnumerator RegionRemovalRoutine(IReadOnlyCollection<int> removedIds, JellyBlockData resultingBlock, float duration)
    // {
    //     var targetRects = new Dictionary<int, Rect>();
    //     resultingBlock.ApplyGeometry();
    //     foreach (var region in resultingBlock.regions) targetRects[region.id] = region.rect;

    //     var startPositions = new Dictionary<int, Vector3>();
    //     var startScales = new Dictionary<int, Vector3>();
    //     foreach (var pair in regionVisuals)
    //     {
    //         startPositions[pair.Key] = pair.Value.root.localPosition;
    //         startScales[pair.Key] = pair.Value.root.localScale;
    //     }

    //     float safeDuration = Mathf.Max(.01f, duration);
    //     for (float time = 0; time < safeDuration; time += Time.deltaTime)
    //     {
    //         float t = Mathf.Clamp01(time / safeDuration);
    //         float eased = t * t * (3f - 2f * t);
    //         foreach (var pair in regionVisuals)
    //         {
    //             var visual = pair.Value;
    //             bool removed = ContainsId(removedIds, pair.Key);
    //             if (removed)
    //             {
    //                 visual.root.localScale = Vector3.Lerp(startScales[pair.Key], Vector3.zero, eased);
    //                 visual.root.localPosition = startPositions[pair.Key];
    //                 continue;
    //             }

    //             if (!targetRects.TryGetValue(pair.Key, out var targetRect)) continue;
    //             var targetPosition = RegionCenter(targetRect);
    //             var scaleRatio = new Vector3(targetRect.width / Mathf.Max(visual.rect.width, .0001f), targetRect.height / Mathf.Max(visual.rect.height, .0001f), 1f);
    //             visual.root.localPosition = Vector3.Lerp(startPositions[pair.Key], targetPosition, eased);
    //             visual.root.localScale = Vector3.Lerp(startScales[pair.Key], scaleRatio, eased);
    //         }
    //         yield return null;
    //     }

    //     var destroyed = new List<int>();
    //     foreach (var pair in regionVisuals)
    //     {
    //         if (ContainsId(removedIds, pair.Key))
    //         {
    //             Destroy(pair.Value.root.gameObject);
    //             destroyed.Add(pair.Key);
    //             continue;
    //         }
    //         if (targetRects.TryGetValue(pair.Key, out var targetRect))
    //         {
    //             pair.Value.root.localPosition = RegionCenter(targetRect);
    //             pair.Value.root.localScale = Vector3.one;
    //             pair.Value.rect = targetRect;
    //         }
    //     }
    //     foreach (var id in destroyed) regionVisuals.Remove(id);
    // }


IEnumerator RegionRemovalRoutine(
    IReadOnlyCollection<int> removedIds,
    JellyBlockData resultingBlock,
    float duration)
{
    var targetRects = new Dictionary<int, Rect>();
    resultingBlock.ApplyGeometry();

    foreach (var region in resultingBlock.regions)
        targetRects[region.id] = region.rect;

    var startPositions = new Dictionary<int, Vector3>();
    var startVisualScales = new Dictionary<int, Vector3>();
    var startVisualLocalPositions = new Dictionary<int, Vector3>();

    var targetVisualScales = new Dictionary<int, Vector3>();
    var targetVisualLocalPositions = new Dictionary<int, Vector3>();

    foreach (var pair in regionVisuals)
    {
        int id = pair.Key;
        var region = pair.Value;

        startPositions[id] = region.root.localPosition;
        startVisualScales[id] = region.visual.localScale;
        startVisualLocalPositions[id] = region.visual.localPosition;

        if (ContainsId(removedIds, id))
            continue;

        if (!targetRects.TryGetValue(id, out var targetRect))
            continue;

        // Tính tỉ lệ kích thước mới so với region hiện tại.
        float scaleX = targetRect.width /
            Mathf.Max(region.rect.width, 0.0001f);

        float scaleY = targetRect.height /
            Mathf.Max(region.rect.height, 0.0001f);

        var scaleRatio = new Vector3(scaleX, scaleY, 1f);

        // Scale trực tiếp mesh visual, không scale region root.
        targetVisualScales[id] = Vector3.Scale(
            startVisualScales[id],
            scaleRatio
        );

        // Giữ tâm mesh được căn chỉnh nếu prefab có pivot lệch tâm.
        targetVisualLocalPositions[id] = Vector3.Scale(
            startVisualLocalPositions[id],
            scaleRatio
        );
    }

    float safeDuration = Mathf.Max(0.01f, duration);
    float elapsed = 0f;

    while (elapsed < safeDuration)
    {
        elapsed += Time.deltaTime;

        float t = Mathf.Clamp01(elapsed / safeDuration);
        float eased = t * t * (3f - 2f * t);

        foreach (var pair in regionVisuals)
        {
            int id = pair.Key;
            var region = pair.Value;

            if (ContainsId(removedIds, id))
            {
                // Region bị match: thu nhỏ mesh cho đến khi biến mất.
                region.visual.localScale = Vector3.Lerp(
                    startVisualScales[id],
                    Vector3.zero,
                    eased
                );

                continue;
            }

            if (!targetRects.TryGetValue(id, out var targetRect))
                continue;

            // Region còn lại: di chuyển tới vị trí mới.
            region.root.localPosition = Vector3.Lerp(
                startPositions[id],
                RegionCenter(targetRect),
                eased
            );

            // Đồng thời kéo giãn mesh để lấp phần không gian trống.
            if (targetVisualScales.TryGetValue(id, out var targetScale))
            {
                region.visual.localScale = Vector3.Lerp(
                    startVisualScales[id],
                    targetScale,
                    eased
                );
            }

            if (targetVisualLocalPositions.TryGetValue(
                id, out var targetLocalPosition))
            {
                region.visual.localPosition = Vector3.Lerp(
                    startVisualLocalPositions[id],
                    targetLocalPosition,
                    eased
                );
            }
        }

        yield return null;
    }

    var destroyedIds = new List<int>();

    foreach (var pair in regionVisuals)
    {
        int id = pair.Key;
        var region = pair.Value;

        if (ContainsId(removedIds, id))
        {
            Destroy(region.root.gameObject);
            destroyedIds.Add(id);
            continue;
        }

        if (!targetRects.TryGetValue(id, out var targetRect))
            continue;

        // Chỉ chốt chính xác vị trí và kích thước đích.
        // Không reset kích thước mesh về kích thước cũ.
        region.root.localPosition = RegionCenter(targetRect);
        region.root.localScale = Vector3.one;

        if (targetVisualScales.TryGetValue(id, out var targetScale))
            region.visual.localScale = targetScale;

        if (targetVisualLocalPositions.TryGetValue(
            id, out var targetLocalPosition))
        {
            region.visual.localPosition = targetLocalPosition;
        }

        region.rect = targetRect;
    }

    foreach (int id in destroyedIds)
        regionVisuals.Remove(id);
}

    static Vector3 RegionCenter(Rect rect) => new Vector3(rect.center.x - .5f, rect.center.y - .5f, 0);

    public void SetSelected(bool selected)
    {
        StopAllCoroutines();
        if (isDragging) return;
        StartCoroutine(Jiggle(selected ? 1.08f : 1f, .16f));
    }

    public void SetDragging(bool dragging)
    {
        if (isDragging == dragging) return;
        isDragging = dragging;
        StopAllCoroutines();
        transform.localScale = dragging ? new Vector3(1.08f, .92f, 1f) : Vector3.one;
        if (!dragging) StartCoroutine(SettleDragRotation());
    }

    public void SetDragVelocity(Vector3 velocity)
    {
        if (!isDragging) return;
        transform.localRotation = Quaternion.Euler(
            Mathf.Clamp(-velocity.y * .015f, -10f, 10f),
            Mathf.Clamp(velocity.x * .015f, -10f, 10f),
            Mathf.Clamp(-velocity.x * .01f, -8f, 8f));
    }

    public void PlayPop(float duration = .08f)
    {
        StopAllCoroutines();
        StartCoroutine(Pop(duration));
    }

    IEnumerator SettleDragRotation()
    {
        var start = transform.localRotation;
        for (float time = 0; time < .16f; time += Time.deltaTime)
        {
            transform.localRotation = Quaternion.Slerp(start, Quaternion.identity, time / .16f);
            yield return null;
        }
        transform.localRotation = Quaternion.identity;
    }

    IEnumerator Pop(float duration)
    {
        var start = transform.localScale;
        for (float time = 0; time < Mathf.Max(.01f, duration); time += Time.deltaTime)
        {
            transform.localScale = Vector3.Lerp(start, new Vector3(1.16f, .82f, 1f), time / Mathf.Max(.01f, duration));
            yield return null;
        }
        transform.localScale = start;
    }

    IEnumerator Jiggle(float scale, float duration)
    {
        var start = transform.localScale;
        for (float time = 0; time < duration; time += Time.deltaTime)
        {
            var wobble = Mathf.Sin(time * 28f) * .025f;
            transform.localScale = Vector3.Lerp(start, Vector3.one * scale, time / duration) + new Vector3(wobble, -wobble, 0);
            yield return null;
        }
        if (!isDragging) transform.localScale = Vector3.one * scale;
    }

    void ClearRuntimeVisuals()
    {
        foreach (var pair in regionVisuals) if (pair.Value.root != null) Destroy(pair.Value.root.gameObject);
        regionVisuals.Clear();
    }

    void OnDestroy() => ClearRuntimeVisuals();

    static bool ContainsId(IReadOnlyCollection<int> ids, int id)
    {
        foreach (var value in ids)
            if (value == id) return true;
        return false;
    }

    static void CenterVisual(GameObject visual, Transform anchor)
    {
        var renderers = visual.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        var bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        visual.transform.position += anchor.position - bounds.center;
    }
}
