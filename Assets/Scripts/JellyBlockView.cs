using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class JellyBlockView : MonoBehaviour
{
    public JellyBlockData Data { get; set; }
    readonly List<Transform> pieces = new List<Transform>();
    static readonly Dictionary<JellyColor, Material> materials = new Dictionary<JellyColor, Material>();

    public static JellyBlockView Create(JellyBlockData data, Transform parent, Vector3 position, Vector2Int cell, int spawnSlot, GameObject regionPrefab = null)
    {
        var objectRoot = new GameObject(spawnSlot >= 0 ? "Spawn Jelly " + spawnSlot : "Jelly " + cell);
        objectRoot.transform.SetParent(parent);
        objectRoot.transform.position = position;
        var view = objectRoot.AddComponent<JellyBlockView>();
        view.Data = data;
        view.Build(regionPrefab);
        return view;
    }

    void Build(GameObject regionPrefab)
    {
        Data.ApplyGeometry();
        foreach (var piece in pieces) if (piece != null) Destroy(piece.gameObject);
        pieces.Clear();
        for (int i = 0; i < Data.regions.Count; i++)
        {
            var region = Data.regions[i];
            var cube = regionPrefab == null ? GameObject.CreatePrimitive(PrimitiveType.Cube) : Instantiate(regionPrefab, transform);
            cube.name = "Region " + region.id;
            cube.transform.SetParent(transform, false);
            cube.transform.localPosition = new Vector3(region.rect.center.x - .5f, region.rect.center.y - .5f, 0);
            cube.transform.localScale = new Vector3(region.rect.width * .92f, region.rect.height * .92f, .32f);
            var material = MaterialFor(region.color);
            foreach (var renderer in cube.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial = material;
            pieces.Add(cube.transform);
        }
    }

    static Material MaterialFor(JellyColor color)
    {
        if (materials.TryGetValue(color, out var material)) return material;
        material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = color == JellyColor.Yellow ? new Color(1, .75f, .12f) : color == JellyColor.Blue ? new Color(.1f, .5f, 1) : color == JellyColor.Red ? new Color(1, .2f, .2f) : color == JellyColor.Green ? new Color(.2f, .85f, .35f) : new Color(1, .25f, .7f);
        materials[color] = material;
        return material;
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
