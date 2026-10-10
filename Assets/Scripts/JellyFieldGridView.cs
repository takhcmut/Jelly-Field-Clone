using System.Collections.Generic;
using UnityEngine;

public sealed class JellyFieldGridView
{
    sealed class CellVisual
    {
        public Vector2Int coordinate;
        public Renderer fill;
    }

    readonly List<CellVisual> cells = new List<CellVisual>();
    readonly Dictionary<Vector2Int, CellVisual> byCoordinate = new Dictionary<Vector2Int, CellVisual>();
    static Material emptyMaterial;
    static Material occupiedMaterial;
    static Material borderMaterial;

    public void Build(IReadOnlyCollection<Vector2Int> validCells, Transform parent)
    {
        Clear(parent);
        foreach (var coordinate in validCells)
        {
            var root = new GameObject("Cell " + coordinate);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(coordinate.x, coordinate.y + 2.2f, .2f);

            var fill = CreatePart(root.transform, "Fill", new Vector3(.92f, .92f, .04f), Vector3.zero, FillMaterial(false));
            CreatePart(root.transform, "Top Border", new Vector3(1f, .035f, .06f), new Vector3(0, .5f, -.04f), BorderMaterial());
            CreatePart(root.transform, "Bottom Border", new Vector3(1f, .035f, .06f), new Vector3(0, -.5f, -.04f), BorderMaterial());
            CreatePart(root.transform, "Left Border", new Vector3(.035f, .93f, .06f), new Vector3(-.5f, 0, -.04f), BorderMaterial());
            CreatePart(root.transform, "Right Border", new Vector3(.035f, .93f, .06f), new Vector3(.5f, 0, -.04f), BorderMaterial());

            var visual = new CellVisual { coordinate = coordinate, fill = fill };
            cells.Add(visual);
            byCoordinate[coordinate] = visual;
        }
    }

    public void Refresh(IReadOnlyDictionary<Vector2Int, JellyBlockData> occupiedCells)
    {
        foreach (var cell in cells)
            cell.fill.sharedMaterial = occupiedCells.ContainsKey(cell.coordinate) ? FillMaterial(true) : FillMaterial(false);
    }

    void Clear(Transform parent)
    {
        cells.Clear();
        byCoordinate.Clear();
        for (int i = parent.childCount - 1; i >= 0; i--)
            if (parent.GetChild(i).name.StartsWith("Cell ")) Object.Destroy(parent.GetChild(i).gameObject);
    }

    static Renderer CreatePart(Transform parent, string name, Vector3 scale, Vector3 position, Material material)
    {
        var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.GetComponent<Renderer>().sharedMaterial = material;
        var collider = part.GetComponent<Collider>();
        if (collider != null) Object.Destroy(collider);
        return part.GetComponent<Renderer>();
    }

    static Material FillMaterial(bool occupied)
    {
        if (occupied && occupiedMaterial != null) return occupiedMaterial;
        if (!occupied && emptyMaterial != null) return emptyMaterial;
        var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        material.color = occupied ? new Color(.09f, .13f, .17f) : new Color(.2f, .3f, .34f);
        if (occupied) occupiedMaterial = material; else emptyMaterial = material;
        return material;
    }

    static Material BorderMaterial()
    {
        if (borderMaterial != null) return borderMaterial;
        borderMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        borderMaterial.color = new Color(.48f, .68f, .7f);
        return borderMaterial;
    }
}
