using System.Collections.Generic;
using UnityEngine;

public class BoardGridView
{
    public void Build(IReadOnlyCollection<Vector2Int> validCells, Transform parent, GameObject cellPrefab, float visualScale)
    {
        Clear(parent);
        if (cellPrefab == null)
        {
            Debug.LogError("BoardGridView requires a cell prefab. Assign it to GameController > Cell Prefab.");
            return;
        }

        foreach (var coordinate in validCells)
        {
            var cell = Object.Instantiate(cellPrefab, parent);
            cell.name = "Cell " + coordinate;
            cell.transform.localPosition = new Vector3(coordinate.x, coordinate.y + 2.2f, .2f);
            cell.transform.localScale = Vector3.Scale(cell.transform.localScale, Vector3.one * visualScale);
        }
    }

    void Clear(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            if (parent.GetChild(i).name.StartsWith("Cell ")) Object.Destroy(parent.GetChild(i).gameObject);
    }
}
