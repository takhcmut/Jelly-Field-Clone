using System.Collections;
using UnityEngine;

 [System.Serializable]
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
    public JellyBlockData Data { get; set; }
    public Vector2Int Cell { get; private set; }
    readonly System.Collections.Generic.List<Transform> pieces = new System.Collections.Generic.List<Transform>();
    bool isDragging;

    public static JellyBlockView Create(JellyBlockData data, Transform parent, Vector3 position, Vector2Int cell, int spawnSlot, GameObject regionPrefab = null, JellyMaterialPalette materialPalette = null)
    {
        var objectRoot = new GameObject(spawnSlot >= 0 ? "Spawn Jelly " + spawnSlot : "Jelly " + cell);
        objectRoot.transform.SetParent(parent);
        objectRoot.transform.position = position;
        var view = objectRoot.AddComponent<JellyBlockView>();
        view.Data = data;
        view.Cell = cell;
        view.Build(regionPrefab, materialPalette);
        return view;
    }

    void Build(GameObject regionPrefab, JellyMaterialPalette materialPalette)
    {
        Data.ApplyGeometry();
        foreach (var piece in pieces) if (piece != null) Destroy(piece.gameObject);
        pieces.Clear();
        for (int i = 0; i < Data.regions.Count; i++)
        {
            var region = Data.regions[i];
            var regionRoot = new GameObject("Region " + region.id);
            regionRoot.transform.SetParent(transform, false);
            regionRoot.transform.localPosition = new Vector3(region.rect.center.x - .5f, region.rect.center.y - .5f, 0);

            var cube = regionPrefab == null ? GameObject.CreatePrimitive(PrimitiveType.Cube) : Instantiate(regionPrefab);
            cube.transform.SetParent(regionRoot.transform, false);
            cube.transform.localPosition = Vector3.zero;
            var prefabScale = cube.transform.localScale;
            cube.transform.localScale = Vector3.Scale(prefabScale, new Vector3(region.rect.width, region.rect.height, .32f));
            FitVisualToRegion(cube, regionRoot.transform, region.rect);
            var material = materialPalette == null ? null : materialPalette.Get(region.color);
            if (material != null)
                foreach (var renderer in cube.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial = material;
            pieces.Add(regionRoot.transform);
        }
    }

    static void CenterVisual(GameObject visual, Transform anchor)
    {
        if (!TryGetRendererBounds(visual, out var bounds)) return;
        visual.transform.position += anchor.position - bounds.center;
    }

    static void FitVisualToRegion(GameObject visual, Transform anchor, Rect region)
    {
        CenterVisual(visual, anchor);
        if (!TryGetRendererBounds(visual, out var bounds)) return;
        if (bounds.size.x <= .0001f || bounds.size.y <= .0001f) return;

        var fitScale = new Vector3(region.width / bounds.size.x, region.height / bounds.size.y, 1f);
        visual.transform.localScale = Vector3.Scale(visual.transform.localScale, fitScale);
        CenterVisual(visual, anchor);
    }

    static bool TryGetRendererBounds(GameObject visual, out Bounds bounds)
    {
        var renderers = visual.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            bounds = default;
            return false;
        }

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return true;
    }

    public void SetSelected(bool selected)
    {
        StopAllCoroutines();
        StartCoroutine(Jiggle(selected ? 1.08f : 1f, .16f));
    }

    public void SetDragging(bool dragging)
    {
        if (isDragging == dragging) return;
        isDragging = dragging;
        StopAllCoroutines();
        transform.localScale = dragging ? new Vector3(1.08f, .92f, 1) : Vector3.one;
        if (!dragging) StartCoroutine(SettleDragRotation());
    }

    public void SetDragVelocity(Vector3 velocity)
    {
        if (!isDragging) return;
        var sway = Mathf.Clamp(velocity.x * .015f, -12f, 12f);
        var pitch = Mathf.Clamp(-velocity.y * .015f, -12f, 12f);
        transform.localRotation = Quaternion.Euler(pitch, sway, -sway * .7f);
    }

    IEnumerator SettleDragRotation()
    {
        var start = transform.localRotation;
        for (float t = 0; t < .16f; t += Time.deltaTime)
        {
            transform.localRotation = Quaternion.Slerp(start, Quaternion.identity, t / .16f);
            yield return null;
        }
        transform.localRotation = Quaternion.identity;
    }

    public void PlayPop(float duration = .08f)
    {
        StopAllCoroutines();
        StartCoroutine(Pop(duration));
    }

    IEnumerator Pop(float duration)
    {
        var start = transform.localScale;
        if (duration <= 0f)
        {
            transform.localScale = start;
            yield break;
        }
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            transform.localScale = Vector3.Lerp(start, new Vector3(1.16f, .82f, 1), t / duration);
            yield return null;
        }
        transform.localScale = start;
    }

    IEnumerator Jiggle(float scale, float duration)
    {
        var start = transform.localScale;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            var wobble = Mathf.Sin(t * 28) * .025f;
            transform.localScale = Vector3.Lerp(start, Vector3.one * scale, t / duration) + new Vector3(wobble, -wobble, 0);
            yield return null;
        }
        transform.localScale = Vector3.one * scale;
    }
}
