using System;
using UnityEngine;
using UnityEngine.UI;

public class GameHud : MonoBehaviour
{
    Canvas canvas;
    [SerializeField] Text levelText;
    [SerializeField] Text objectiveText;
    [SerializeField] Text messageText;
    [SerializeField] GameObject completionPanel;

    public void ShowLevel(LevelData level, int index, int total, ObjectiveTracker objectives)
    {
        levelText.text = level.name + "  " + (index + 1) + "/" + total;
        objectiveText.text = "OBJECTIVES\n";
        foreach (var objective in level.objectives)
            objectiveText.text += objective.color.ToString().ToUpperInvariant() + ": " + Mathf.Min(objectives.GetProgress(objective.color), objective.required) + "/" + objective.required + "    ";
    }

    public void SetMessage(string message) => messageText.text = message;
    public void SetCompletionVisible(bool visible) => completionPanel.SetActive(visible);

    Text CreateText(Transform parent, string name, Vector2 min, Vector2 max, int size, TextAnchor anchor)
    {
        var text = new GameObject(name).AddComponent<Text>();
        text.transform.SetParent(parent, false);
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.color = Color.white;
        text.alignment = anchor;
        var rect = text.rectTransform;
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return text;
    }
}
