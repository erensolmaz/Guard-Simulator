using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class MainMenuCanvasCreator : EditorWindow
{
    private string gameSceneName = "Sandbox";
    private string menuTitle = "GUARD SIMULATOR";
    private Color backgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.85f);
    private Color accentColor = new Color(0.2f, 0.6f, 1f, 1f);
    private Color buttonColor = new Color(0.15f, 0.15f, 0.2f, 0.9f);
    private Color buttonHoverColor = new Color(0.25f, 0.65f, 1f, 1f);

    [MenuItem("Tools/Create Main Menu Canvas")]
    public static void ShowWindow()
    {
        GetWindow<MainMenuCanvasCreator>("Main Menu Creator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Main Menu Canvas Creator", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        menuTitle = EditorGUILayout.TextField("Menu Title", menuTitle);
        gameSceneName = EditorGUILayout.TextField("Game Scene Name", gameSceneName);
        
        EditorGUILayout.Space(5);
        backgroundColor = EditorGUILayout.ColorField("Background Color", backgroundColor);
        accentColor = EditorGUILayout.ColorField("Accent Color", accentColor);
        buttonColor = EditorGUILayout.ColorField("Button Color", buttonColor);
        buttonHoverColor = EditorGUILayout.ColorField("Button Hover Color", buttonHoverColor);

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Create Main Menu Canvas", GUILayout.Height(40)))
        {
            CreateMainMenuCanvas();
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox(
            "This will create a left-aligned main menu canvas with:\n" +
            "• Title text\n" +
            "• Play button\n" +
            "• Quit button\n" +
            "• MainMenuManager script",
            MessageType.Info
        );
    }

    private void CreateMainMenuCanvas()
    {
        GameObject existingCanvas = GameObject.Find("Main Menu Canvas");
        if (existingCanvas != null)
        {
            if (!EditorUtility.DisplayDialog("Canvas Exists", 
                "Main Menu Canvas already exists. Delete and recreate?", "Yes", "No"))
            {
                return;
            }
            DestroyImmediate(existingCanvas);
        }

        GameObject canvasObj = new GameObject("Main Menu Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject leftPanel = CreateLeftPanel(canvasObj.transform);
        CreateTitleText(leftPanel.transform);
        GameObject playButton = CreatePlayButton(leftPanel.transform);
        GameObject quitButton = CreateQuitButton(leftPanel.transform);

        SimpleMenuManager menuManager = canvasObj.AddComponent<SimpleMenuManager>();
        SerializedObject so = new SerializedObject(menuManager);
        so.FindProperty("playButton").objectReferenceValue = playButton.GetComponent<Button>();
        so.FindProperty("quitButton").objectReferenceValue = quitButton.GetComponent<Button>();
        so.FindProperty("gameSceneName").stringValue = gameSceneName;
        so.ApplyModifiedProperties();

        Selection.activeGameObject = canvasObj;
        EditorUtility.SetDirty(canvasObj);

        Debug.Log("Main Menu Canvas created successfully!");
    }

    private GameObject CreateLeftPanel(Transform parent)
    {
        GameObject panel = new GameObject("Left Panel");
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 0.5f);
        rect.anchoredPosition = new Vector2(0, 0);
        rect.sizeDelta = new Vector2(500, 0);

        Image image = panel.AddComponent<Image>();
        image.color = backgroundColor;

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 20;
        layout.padding = new RectOffset(40, 40, 100, 100);
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        return panel;
    }

    private void CreateTitleText(Transform parent)
    {
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(parent, false);

        RectTransform rect = titleObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 120);

        TextMeshProUGUI text = titleObj.AddComponent<TextMeshProUGUI>();
        text.text = menuTitle;
        text.fontSize = 48;
        text.fontStyle = FontStyles.Bold;
        text.color = accentColor;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = false;

        LayoutElement layout = titleObj.AddComponent<LayoutElement>();
        layout.preferredHeight = 120;
    }

    private GameObject CreatePlayButton(Transform parent)
    {
        GameObject buttonObj = new GameObject("Play Button");
        buttonObj.transform.SetParent(parent, false);

        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 80);

        Image image = buttonObj.AddComponent<Image>();
        image.color = buttonColor;

        Button button = buttonObj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = buttonHoverColor;
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.selectedColor = buttonHoverColor;
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        colors.colorMultiplier = 1f;
        button.colors = colors;

        buttonObj.AddComponent<MenuButtonAnimator>();

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "PLAY";
        text.fontSize = 36;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;

        LayoutElement layout = buttonObj.AddComponent<LayoutElement>();
        layout.preferredHeight = 80;

        return buttonObj;
    }

    private GameObject CreateQuitButton(Transform parent)
    {
        GameObject buttonObj = new GameObject("Quit Button");
        buttonObj.transform.SetParent(parent, false);

        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 80);

        Image image = buttonObj.AddComponent<Image>();
        image.color = buttonColor;

        Button button = buttonObj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.3f, 0.3f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.selectedColor = new Color(1f, 0.3f, 0.3f, 1f);
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        colors.colorMultiplier = 1f;
        button.colors = colors;

        buttonObj.AddComponent<MenuButtonAnimator>();

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "QUIT";
        text.fontSize = 36;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;

        LayoutElement layout = buttonObj.AddComponent<LayoutElement>();
        layout.preferredHeight = 80;

        return buttonObj;
    }
}
