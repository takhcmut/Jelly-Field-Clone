
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

    [Header("Goal Panel")]
[SerializeField] RectTransform goalPanel;

[SerializeField] Vector2 iconSize = new Vector2(58f, 58f);
[SerializeField] float iconSpacing = 16f;
[SerializeField] int countFontSize = 24;

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

if (objectiveRow == null)
    return;

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

if (objectiveRow == null)
    return;

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
    if (goalPanel == null)
    {
        Debug.LogError(
            "GameHud: Please assign Goal Panel in the Inspector.",
            this
        );
        return;
    }

    // Configure the existing panel's vertical layout.
    var vertical = goalPanel.GetComponent<VerticalLayoutGroup>();

    if (vertical == null)
        vertical = goalPanel.gameObject.AddComponent<VerticalLayoutGroup>();

    // vertical.padding = new RectOffset(12, 12, 8, 8);
    // // vertical.spacing = 5f;
    // vertical.childAlignment = TextAnchor.UpperCenter;
    // vertical.childControlWidth = true;
    // // vertical.childControlHeight = true;
    // vertical.childForceExpandWidth = true;
    // vertical.childForceExpandHeight = false;

    // Give the existing GOAL title a preferred height if it exists.
    Transform titleTransform = goalPanel.Find("Goal Title");

    if (titleTransform != null)
    {
        var titleLayout =
            titleTransform.GetComponent<LayoutElement>();

        if (titleLayout == null)
            titleLayout = titleTransform.gameObject.AddComponent<LayoutElement>();

        titleLayout.preferredHeight = 28f;
    }

    // Find Objective Row if it already exists.
    if (objectiveRow == null)
    {
        Transform existingRow = goalPanel.Find("Objective Row");

        if (existingRow != null)
            objectiveRow = existingRow as RectTransform;
    }

    // Create only the row, not the panel or the title.
    if (objectiveRow == null)
    {
        var rowObject = new GameObject(
            "Objective Row",
            typeof(RectTransform),
            typeof(HorizontalLayoutGroup),
            typeof(LayoutElement)
        );

        objectiveRow = rowObject.GetComponent<RectTransform>();
        objectiveRow.SetParent(goalPanel, false);
    }

    var horizontal =
        objectiveRow.GetComponent<HorizontalLayoutGroup>();

    if (horizontal == null)
        horizontal = objectiveRow.gameObject.AddComponent<HorizontalLayoutGroup>();

    horizontal.spacing = iconSpacing;
    horizontal.childAlignment = TextAnchor.MiddleCenter;
    horizontal.childControlWidth = false;
    horizontal.childControlHeight = false;
    horizontal.childForceExpandWidth = false;
    horizontal.childForceExpandHeight = false;

    var rowLayout = objectiveRow.GetComponent<LayoutElement>();

    if (rowLayout == null)
        rowLayout = objectiveRow.gameObject.AddComponent<LayoutElement>();

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
