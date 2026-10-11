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
    readonly System.Collections.Generic.List<Transform> pieces = new System.Collections.Generic.List<Transform>();

    public static JellyBlockView Create(JellyBlockData data, Transform parent, Vector3 position, Vector2Int cell, int spawnSlot, GameObject regionPrefab = null, JellyMaterialPalette materialPalette = null)
    {
        var objectRoot = new GameObject(spawnSlot >= 0 ? "Spawn Jelly " + spawnSlot : "Jelly " + cell);
        objectRoot.transform.SetParent(parent);
        objectRoot.transform.position = position;
        var view = objectRoot.AddComponent<JellyBlockView>();
        view.Data = data;
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
            cube.transform.localScale = Vector3.Scale(prefabScale, new Vector3(region.rect.width * .92f, region.rect.height * .92f, .32f));
            CenterVisual(cube, regionRoot.transform);
            var material = materialPalette == null ? null : materialPalette.Get(region.color);
            if (material != null)
                foreach (var renderer in cube.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial = material;
            pieces.Add(regionRoot.transform);
        }
    }

    static void CenterVisual(GameObject visual, Transform anchor)
    {
        var renderers = visual.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        var bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        visual.transform.position += anchor.position - bounds.center;
    }

    public void SetSelected(bool selected)
    {
        StopAllCoroutines();
        StartCoroutine(Jiggle(selected ? 1.08f : 1f, .16f));
    }

    public void SetDragging(bool dragging)
    {
        transform.localScale = dragging ? new Vector3(1.08f, .92f, 1) : Vector3.one;
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
