using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class JellyFieldUiController
{
    Canvas canvas;
    Text levelText;
    Text objectiveText;
    Text messageText;
    GameObject completionPanel;

    public void Build(Action retry, Action nextLevel)
    {
        canvas = new GameObject("Jelly Field UI").AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        canvas.gameObject.AddComponent<GraphicRaycaster>();

        levelText = CreateText(canvas.transform, "Level", new Vector2(.06f, .92f), new Vector2(.68f, .98f), 42, TextAnchor.MiddleLeft);
        objectiveText = CreateText(canvas.transform, "Objectives", new Vector2(.06f, .78f), new Vector2(.94f, .9f), 30, TextAnchor.UpperLeft);
        messageText = CreateText(canvas.transform, "Message", new Vector2(.06f, .04f), new Vector2(.94f, .12f), 30, TextAnchor.MiddleCenter);
        CreateButton(canvas.transform, "Retry", new Vector2(.7f, .92f), new Vector2(.94f, .98f), retry);

        completionPanel = new GameObject("Completion Panel");
        completionPanel.transform.SetParent(canvas.transform, false);
        var panelImage = completionPanel.AddComponent<Image>();
        panelImage.color = new Color(.03f, .06f, .09f, .96f);
        var panelRect = completionPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(.1f, .35f);
        panelRect.anchorMax = new Vector2(.9f, .65f);
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
        CreateText(completionPanel.transform, "Complete Text", new Vector2(.08f, .55f), new Vector2(.92f, .82f), 38, TextAnchor.MiddleCenter).text = "LEVEL COMPLETE";
        CreateButton(completionPanel.transform, "Next Level", new Vector2(.25f, .12f), new Vector2(.75f, .4f), nextLevel);
        completionPanel.SetActive(false);
    }

    public void ShowLevel(LevelData level, int index, int total, JellyFieldObjectiveController objectives)
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

    Button CreateButton(Transform parent, string label, Vector2 min, Vector2 max, Action action)
    {
        var buttonObject = new GameObject(label + " Button");
        buttonObject.transform.SetParent(parent, false);
        var image = buttonObject.AddComponent<Image>();
        image.color = new Color(.18f, .55f, .68f);
        var button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(() => action());
        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        CreateText(buttonObject.transform, label + " Label", Vector2.zero, Vector2.one, 28, TextAnchor.MiddleCenter);
        return button;
    }
}
