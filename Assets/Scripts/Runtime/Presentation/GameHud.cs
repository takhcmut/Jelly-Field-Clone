
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameHud : MonoBehaviour
{
    [Header("Existing HUD")]
    [SerializeField] Text levelText;
    [SerializeField] Text objectiveText;
    [SerializeField] Text messageText;
    [SerializeField] GameObject completionPanel;

    [Header("Jelly Goal Sprites")]
    [SerializeField] Sprite yellowSprite;
    [SerializeField] Sprite blueSprite;
    [SerializeField] Sprite purpleSprite;
    [SerializeField] Sprite greenSprite;
    [SerializeField] Sprite pinkSprite;

    [Header("Goal UI Settings")]
    [SerializeField] Vector2 panelSize = new Vector2(420f, 110f);
    [SerializeField] Vector2 iconSize = new Vector2(58f, 58f);
    [SerializeField] float iconSpacing = 16f;
    [SerializeField] int countFontSize = 24;
    [SerializeField] Color panelColor =
        new Color(0.08f, 0.11f, 0.16f, 0.88f);

    RectTransform goalPanel;
    RectTransform objectiveRow;

    readonly Dictionary<JellyColor, Text> objectiveCounts =
        new Dictionary<JellyColor, Text>();

    public void ShowLevel(
        LevelData level,
        int index,
        int total,
        ObjectiveTracker objectives)
    {
        if (levelText != null)
            levelText.text =
                level.name + "  " + (index + 1) + "/" + total;

        EnsureGoalUI();
        RebuildObjectives(level, objectives);

        if (objectiveText != null)
            objectiveText.gameObject.SetActive(false);
    }

    public void RefreshObjectives(
        LevelData level,
        ObjectiveTracker objectives)
    {
        if (level == null || objectives == null)
            return;

        EnsureGoalUI();

        foreach (var objective in level.objectives)
        {
            if (!objectiveCounts.TryGetValue(
                    objective.color, out var countText))
                continue;

            int collected = Mathf.Min(
                objectives.GetProgress(objective.color),
                objective.required
            );

            int remaining = Mathf.Max(
                0, objective.required - collected
            );

            countText.text = remaining.ToString();
        }
    }

    void EnsureGoalUI()
    {
        if (goalPanel != null)
            return;

        Canvas canvas = levelText != null
            ? levelText.GetComponentInParent<Canvas>()
            : GetComponentInParent<Canvas>();

        if (canvas == null)
        {
            Debug.LogError(
                "GameHud: Cannot find a Canvas for the GOAL UI.",
                this
            );
            return;
        }

        // Main panel
        var panelObject = new GameObject(
            "Goal Panel",
            typeof(RectTransform),
            typeof(Image),
            typeof(VerticalLayoutGroup)
        );

        goalPanel = panelObject.GetComponent<RectTransform>();
        goalPanel.SetParent(canvas.transform, false);
        goalPanel.anchorMin = new Vector2(0.5f, 1f);
        goalPanel.anchorMax = new Vector2(0.5f, 1f);
        goalPanel.pivot = new Vector2(0.5f, 1f);
        goalPanel.anchoredPosition = new Vector2(0f, -20f);
        goalPanel.sizeDelta = panelSize;

        var background = panelObject.GetComponent<Image>();
        background.color = panelColor;
        background.raycastTarget = false;

        var vertical = panelObject.GetComponent<VerticalLayoutGroup>();
        vertical.padding = new RectOffset(12, 12, 8, 8);
        vertical.spacing = 5f;
        vertical.childAlignment = TextAnchor.UpperCenter;
        vertical.childControlWidth = true;
        vertical.childControlHeight = true;
        vertical.childForceExpandWidth = true;
        vertical.childForceExpandHeight = false;

        // GOAL title
        var title = CreateText(
            goalPanel,
            "Goal Title",
            Vector2.zero,
            Vector2.one,
            22,
            TextAnchor.MiddleCenter
        );

        title.text = "GOAL";
        title.fontStyle = FontStyle.Bold;
        title.raycastTarget = false;

        var titleLayout =
            title.gameObject.AddComponent<LayoutElement>();

        titleLayout.preferredHeight = 28f;

        // Horizontal row of jelly objectives
        var rowObject = new GameObject(
            "Objective Row",
            typeof(RectTransform),
            typeof(HorizontalLayoutGroup),
            typeof(LayoutElement)
        );

        objectiveRow = rowObject.GetComponent<RectTransform>();
        objectiveRow.SetParent(goalPanel, false);

        var horizontal =
            rowObject.GetComponent<HorizontalLayoutGroup>();

        horizontal.spacing = iconSpacing;
        horizontal.childAlignment = TextAnchor.MiddleCenter;
        horizontal.childControlWidth = false;
        horizontal.childControlHeight = false;
        horizontal.childForceExpandWidth = false;
        horizontal.childForceExpandHeight = false;

        var rowLayout = rowObject.GetComponent<LayoutElement>();
        rowLayout.preferredHeight = iconSize.y + 4f;
        rowLayout.flexibleWidth = 1f;
    }

    void RebuildObjectives(
        LevelData level,
        ObjectiveTracker objectives)
    {
        objectiveCounts.Clear();

        for (int i = objectiveRow.childCount - 1; i >= 0; i--)
            Destroy(objectiveRow.GetChild(i).gameObject);

        foreach (var objective in level.objectives)
        {
            Sprite sprite = GetSprite(objective.color);

            if (sprite == null)
            {
                Debug.LogWarning(
                    "Missing jelly sprite for " + objective.color,
                    this
                );
            }

            // One icon per objective color
            var itemObject = new GameObject(
                objective.color + " Goal",
                typeof(RectTransform),
                typeof(LayoutElement)
            );

            var itemRect = itemObject.GetComponent<RectTransform>();
            itemRect.SetParent(objectiveRow, false);
            itemRect.sizeDelta = iconSize;

            var itemLayout = itemObject.GetComponent<LayoutElement>();
            itemLayout.preferredWidth = iconSize.x;
            itemLayout.preferredHeight = iconSize.y;

            // Jelly sprite
            var iconObject = new GameObject(
                "Jelly Icon",
                typeof(RectTransform),
                typeof(Image)
            );

            var iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.SetParent(itemRect, false);
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            var image = iconObject.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;

            // Count displayed over the center of the jelly
            var count = CreateText(
                itemRect,
                "Remaining Count",
                Vector2.zero,
                Vector2.one,
                countFontSize,
                TextAnchor.MiddleCenter
            );

            count.fontStyle = FontStyle.Bold;
            count.color = Color.white;
            count.raycastTarget = false;

            var outline = count.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            int collected = objectives == null
                ? 0
                : Mathf.Min(
                    objectives.GetProgress(objective.color),
                    objective.required
                );

            int remaining = Mathf.Max(
                0, objective.required - collected
            );

            count.text = remaining.ToString();
            objectiveCounts[objective.color] = count;
        }
    }

    Sprite GetSprite(JellyColor color)
    {
        switch (color)
        {
            case JellyColor.Yellow: return yellowSprite;
            case JellyColor.Blue:   return blueSprite;
            case JellyColor.Purple: return purpleSprite;
            case JellyColor.Green:  return greenSprite;
            case JellyColor.Pink:   return pinkSprite;
            default:                return null;
        }
    }

    public void SetMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;
    }

    public void SetCompletionVisible(bool visible)
    {
        if (completionPanel != null)
            completionPanel.SetActive(visible);
    }

    Text CreateText(
        Transform parent,
        string name,
        Vector2 min,
        Vector2 max,
        int size,
        TextAnchor anchor)
    {
        var text = new GameObject(name).AddComponent<Text>();
        text.transform.SetParent(parent, false);

        text.font = Resources.GetBuiltinResource<Font>(
            "LegacyRuntime.ttf"
        );

        text.fontSize = size;
        text.color = Color.white;
        text.alignment = anchor;

        var rect = text.rectTransform;
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return text;
    }
}
