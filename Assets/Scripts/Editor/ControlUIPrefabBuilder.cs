using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class ControlUIPrefabBuilder
{
    private const string PrefabFolder = "Assets/Prefabs/UI";
    private const string SpriteFolder = PrefabFolder + "/Sprites";
    private const string PrefabPath = PrefabFolder + "/ControlUI.prefab";
    private const string FreeButtonSetFolder = "Assets/Art/UI/FreeButtonSet";

    public static void CreateControlUIPrefab()
    {
        Directory.CreateDirectory(PrefabFolder);
        Directory.CreateDirectory(SpriteFolder);
        EnsureRoundedSprites();

        var root = CreateControlUIRoot();
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
    }

    [MenuItem("Tools/Lead The Way/Install Control UI In Current Scene")]
    public static void InstallControlUIInCurrentScene()
    {
        var existing = GameObject.Find("Control UI");
        if (existing != null)
            Undo.DestroyObjectImmediate(existing);

        Directory.CreateDirectory(SpriteFolder);
        EnsureRoundedSprites();

        var root = CreateControlUIRoot();
        Undo.RegisterCreatedObjectUndo(root, "Install Control UI");
        EnsureEventSystem();

        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeObject = root;
        EditorGUIUtility.PingObject(root);
    }

    public static void AssignDefaultDoorUIIcons()
    {
        var actionIcon = LoadFreeButtonSetSprite("Textures/icons/128x128/play.png");
        if (actionIcon == null)
        {
            EditorUtility.DisplayDialog("Door UI Icons", "Could not find the FreeButtonSet play icon.", "OK");
            return;
        }

        var changedCount = 0;
        foreach (var doorControl in Object.FindObjectsByType<DoorControlActions>(FindObjectsSortMode.None))
        {
            var serialized = new SerializedObject(doorControl);
            serialized.FindProperty("actionIcon").objectReferenceValue = actionIcon;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(doorControl);

            var selectable = doorControl.GetComponent<SelectableControlObject>();
            if (selectable != null)
            {
                var selectableObject = new SerializedObject(selectable);
                var actions = selectableObject.FindProperty("actions");
                if (actions != null && actions.arraySize > 0)
                {
                    actions.GetArrayElementAtIndex(0).FindPropertyRelative("icon").objectReferenceValue = actionIcon;
                    selectableObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(selectable);
                }
            }

            changedCount++;
        }

        if (changedCount > 0)
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Door UI Icons", $"Assigned icons to {changedCount} door control object(s).", "OK");
    }

    [MenuItem("Tools/Lead The Way/Assign Control UI Sounds")]
    public static void AssignControlUISounds()
    {
        var objectClick = LoadAudioClip("Assets/Sound/SoundEffects/object_click.wav");
        var actionClick = LoadAudioClip("Assets/Sound/SoundEffects/action_click.wav");
        var backClick = LoadAudioClip("Assets/Sound/SoundEffects/back_button_click.wav");

        var changedCount = 0;
        foreach (var ui in Object.FindObjectsByType<SelectionPanelsUI>(FindObjectsSortMode.None))
        {
            var audioSource = ui.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = Undo.AddComponent<AudioSource>(ui.gameObject);
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f;
            }

            var serialized = new SerializedObject(ui);
            serialized.FindProperty("objectClickSound").objectReferenceValue = objectClick;
            serialized.FindProperty("actionClickSound").objectReferenceValue = actionClick;
            serialized.FindProperty("backClickSound").objectReferenceValue = backClick;
            serialized.FindProperty("audioSource").objectReferenceValue = audioSource;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(ui);
            changedCount++;
        }

        if (changedCount > 0)
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Control UI Sounds", $"Assigned sounds to {changedCount} control UI object(s).", "OK");
    }

    [MenuItem("Tools/Lead The Way/Configure Door Audio")]
    public static void ConfigureDoorAudio()
    {
        var changedCount = 0;
        foreach (var door in Object.FindObjectsByType<DoorScript.Door>(FindObjectsSortMode.None))
        {
            Undo.RecordObject(door, "Configure Door Audio");
            door.soundVolume = 1.5f;
            door.spatialBlend = 0.2f;
            EditorUtility.SetDirty(door);

            var source = door.GetComponent<AudioSource>();
            if (source != null)
            {
                Undo.RecordObject(source, "Configure Door Audio Source");
                source.playOnAwake = false;
                source.volume = 1f;
                source.spatialBlend = 0.2f;
                EditorUtility.SetDirty(source);
            }

            changedCount++;
        }

        if (changedCount > 0)
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Door Audio", $"Configured audio on {changedCount} door object(s).", "OK");
    }

    private static GameObject CreateControlUIRoot()
    {
        var root = new GameObject("Control UI", typeof(RectTransform));
        var canvasObject = CreateCanvas(root.transform);
        var canvasTransform = canvasObject.GetComponent<RectTransform>();

        var objectPanel = CreatePanel("Object Panel", canvasTransform);
        var actionPanel = CreatePanel("Action Panel", canvasTransform);

        var objectTitle = CreateText("Title", objectPanel.transform, "SELECT OBJECT", 12, FontStyle.Bold, TextAnchor.MiddleLeft, UiWalnut());
        StretchTop(objectTitle.rectTransform, 18f, 10f, -18f, 18f);

        var objectContainer = CreateHorizontalContainer("Object Button Container", objectPanel.transform);
        var objectTemplate = CreateObjectButtonTemplate(objectContainer.transform);

        var actionTitle = CreateText("Title", actionPanel.transform, "DOOR", 12, FontStyle.Bold, TextAnchor.MiddleLeft, UiWalnut());
        StretchTop(actionTitle.rectTransform, 18f, 10f, -88f, 18f);

        var defaultActionIcon = LoadFreeButtonSetSprite("Textures/icons/128x128/play.png");
        var actionContainer = CreateHorizontalContainer("Action Button Container", actionPanel.transform);
        var actionTemplate = CreateActionButtonTemplate(actionContainer.transform);
        var backButton = CreateBackButton(actionPanel.transform);
        actionPanel.SetActive(false);

        var ui = root.AddComponent<SelectionPanelsUI>();
        var audioSource = root.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        var serialized = new SerializedObject(ui);
        serialized.FindProperty("objectPanel").objectReferenceValue = objectPanel;
        serialized.FindProperty("actionPanel").objectReferenceValue = actionPanel;
        serialized.FindProperty("objectPanelTitle").objectReferenceValue = objectTitle;
        serialized.FindProperty("objectButtonContainer").objectReferenceValue = objectContainer.transform;
        serialized.FindProperty("objectButtonTemplate").objectReferenceValue = objectTemplate;
        serialized.FindProperty("actionPanelTitle").objectReferenceValue = actionTitle;
        serialized.FindProperty("actionButtonContainer").objectReferenceValue = actionContainer.transform;
        serialized.FindProperty("actionButtonTemplate").objectReferenceValue = actionTemplate;
        serialized.FindProperty("backButton").objectReferenceValue = backButton;
        serialized.FindProperty("defaultActionIcon").objectReferenceValue = defaultActionIcon;
        serialized.FindProperty("objectClickSound").objectReferenceValue = LoadAudioClip("Assets/Sound/SoundEffects/object_click.wav");
        serialized.FindProperty("actionClickSound").objectReferenceValue = LoadAudioClip("Assets/Sound/SoundEffects/action_click.wav");
        serialized.FindProperty("backClickSound").objectReferenceValue = LoadAudioClip("Assets/Sound/SoundEffects/back_button_click.wav");
        serialized.FindProperty("audioSource").objectReferenceValue = audioSource;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }

    private static GameObject CreateCanvas(Transform parent)
    {
        var canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.transform.SetParent(parent, false);

        var rect = canvas.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var canvasComponent = canvas.GetComponent<Canvas>();
        canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasComponent.sortingOrder = 20;

        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private static GameObject CreatePanel(string name, Transform parent)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);

        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 24f);
        rect.sizeDelta = new Vector2(420f, 94f);

        var image = panel.GetComponent<Image>();
        image.sprite = LoadSprite("PanelSoft");
        image.type = Image.Type.Sliced;
        image.color = UiCream();

        var shadow = panel.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.12f, 0.08f, 0.03f, 0.28f);
        shadow.effectDistance = new Vector2(0f, -3f);

        return panel;
    }

    private static GameObject CreateHorizontalContainer(string name, Transform parent)
    {
        var container = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
        container.transform.SetParent(parent, false);

        var rect = container.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(18f, 12f);
        rect.offsetMax = new Vector2(-18f, -34f);

        var layout = container.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        return container;
    }

    private static Button CreateObjectButtonTemplate(Transform parent)
    {
        var button = CreateButton("Object Button Template", parent, new Vector2(58f, 56f));
        var icon = CreateImage("Icon", button.transform, null, UiWalnut());
        CenterChild(icon.rectTransform, new Vector2(23f, 23f), new Vector2(0f, -11f));
        icon.enabled = false;
        CreateText("Fallback Icon", button.transform, "D", 18, FontStyle.Bold, TextAnchor.MiddleCenter, UiWalnut());
        CenterChild(button.transform.Find("Fallback Icon").GetComponent<RectTransform>(), new Vector2(38f, 30f), new Vector2(0f, -8f));
        CreateNumberPill(button.transform);
        button.gameObject.AddComponent<UIButtonFeedback>();
        return button;
    }

    private static Button CreateActionButtonTemplate(Transform parent)
    {
        var button = CreateButton("Action Button Template", parent, new Vector2(58f, 56f));
        var icon = CreateImage("Icon", button.transform, null, UiWalnut());
        CenterChild(icon.rectTransform, new Vector2(22f, 22f), new Vector2(0f, -12f));
        icon.enabled = false;
        CreateText("Fallback Icon", button.transform, "O", 18, FontStyle.Bold, TextAnchor.MiddleCenter, UiWalnut());
        CenterChild(button.transform.Find("Fallback Icon").GetComponent<RectTransform>(), new Vector2(38f, 30f), new Vector2(0f, -8f));
        CreateNumberPill(button.transform);
        CreateText("Label", button.transform, "Open / Close", 13, FontStyle.Bold, TextAnchor.MiddleCenter, UiWalnut());
        Stretch(button.transform.Find("Label").GetComponent<RectTransform>(), 7f, 23f, -7f, -8f);
        button.gameObject.AddComponent<UIButtonFeedback>();
        return button;
    }

    private static Button CreateBackButton(Transform parent)
    {
        var button = CreateButton("Back Button", parent, new Vector2(68f, 26f));
        var rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-14f, -8f);

        var image = button.GetComponent<Image>();
        image.sprite = LoadSprite("TileSoft");
        image.type = Image.Type.Sliced;
        image.color = new Color(0.93f, 0.83f, 0.62f, 1f);

        var label = CreateText("Label", button.transform, "BACK", 10, FontStyle.Bold, TextAnchor.MiddleCenter, UiWalnut());
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = Vector2.zero;
        label.rectTransform.offsetMax = Vector2.zero;
        button.gameObject.AddComponent<UIButtonFeedback>();

        return button;
    }

    private static Button CreateButton(string name, Transform parent, Vector2 size)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);

        var rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = size;

        var image = buttonObject.GetComponent<Image>();
        image.sprite = LoadFreeButtonSetSprite(size.x > size.y ? "Textures/buttons/button_100.png" : "Textures/buttons/button_round_130.png") ?? LoadSprite("TileSoft");
        image.type = Image.Type.Sliced;
        image.color = Color.white;

        var shadow = buttonObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.14f, 0.08f, 0.03f, 0.18f);
        shadow.effectDistance = new Vector2(0f, -1.5f);

        var outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.38f, 0.24f, 0.08f, 0.28f);
        outline.effectDistance = new Vector2(1f, -1f);

        var button = buttonObject.GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.navigation = new Navigation { mode = Navigation.Mode.None };

        var layout = buttonObject.GetComponent<LayoutElement>();
        layout.preferredWidth = size.x;
        layout.preferredHeight = size.y;

        return button;
    }

    private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
    {
        var imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        var image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private static void CreateNumberPill(Transform parent)
    {
        CreateNumberPill(parent, new Vector2(0f, 3f), new Vector2(19f, 16f), 10);
    }

    private static void CreateNumberPill(Transform parent, Vector2 anchoredPosition, Vector2 size, int fontSize)
    {
        var pill = new GameObject("Number Pill", typeof(RectTransform), typeof(Image), typeof(Outline));
        pill.transform.SetParent(parent, false);
        var rect = pill.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        var image = pill.GetComponent<Image>();
        image.sprite = LoadSprite("BadgeSoft");
        image.type = Image.Type.Sliced;
        image.color = UiBadgeDark();
        var outline = pill.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.35f);
        outline.effectDistance = new Vector2(1f, -1f);

        var number = CreateText("Number", pill.transform, "1", fontSize, FontStyle.Bold, TextAnchor.MiddleCenter, UiBadgeLight());
        number.rectTransform.anchorMin = Vector2.zero;
        number.rectTransform.anchorMax = Vector2.one;
        number.rectTransform.offsetMin = Vector2.zero;
        number.rectTransform.offsetMax = Vector2.zero;
    }

    private static Text CreateText(string name, Transform parent, string value, int size, FontStyle style, TextAnchor alignment)
    {
        return CreateText(name, parent, value, size, style, alignment, UiWalnut());
    }

    private static Text CreateText(string name, Transform parent, string value, int size, FontStyle style, TextAnchor alignment, Color color)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);

        var text = textObject.GetComponent<Text>();
        text.text = value;
        text.font = LoadUIFont();
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;

        return text;
    }

    private static void CenterChild(RectTransform rect, Vector2 size, Vector2 anchoredPosition)
    {
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private static void StretchTop(RectTransform rect, float left, float top, float right, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(left, -top - height);
        rect.offsetMax = new Vector2(right, -top);
    }

    private static void StretchBottom(RectTransform rect, float left, float bottom, float right, float height)
    {
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(right, bottom + height);
    }

    private static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(right, top);
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;

        var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
    }

    private static Color UiGold()
    {
        return new Color(0.96f, 0.68f, 0.18f, 1f);
    }

    private static Color UiCream()
    {
        return new Color(0.98f, 0.94f, 0.80f, 0.98f);
    }

    private static Color UiTile()
    {
        return new Color(0.91f, 0.80f, 0.56f, 1f);
    }

    private static Color UiWalnut()
    {
        return new Color(0.24f, 0.17f, 0.08f, 1f);
    }

    private static Color UiBadgeDark()
    {
        return new Color(0.16f, 0.11f, 0.05f, 1f);
    }

    private static Color UiBadgeLight()
    {
        return new Color(1f, 0.93f, 0.72f, 1f);
    }

    private static Font LoadUIFont()
    {
        var poppins = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/UI/Fonts/Poppins-Bold.ttf");
        if (poppins != null)
            return poppins;

        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private static void EnsureRoundedSprites()
    {
        EnsureRoundedSpriteAsset("PanelSoft", 10);
        EnsureRoundedSpriteAsset("TileSoft", 6);
        EnsureRoundedSpriteAsset("BadgeSoft", 5);
    }

    private static Sprite LoadSprite(string name)
    {
        var path = $"{SpriteFolder}/{name}.asset";
        var sprites = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var asset in sprites)
        {
            if (asset is Sprite sprite)
                return sprite;
        }

        EnsureRoundedSpriteAsset(name, 6);
        sprites = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var asset in sprites)
        {
            if (asset is Sprite sprite)
                return sprite;
        }

        return null;
    }

    private static Sprite LoadFreeButtonSetSprite(string relativePath)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{FreeButtonSetFolder}/{relativePath}");
    }

    private static AudioClip LoadAudioClip(string path)
    {
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }

    private static void EnsureRoundedSpriteAsset(string name, int radius)
    {
        var path = $"{SpriteFolder}/{name}.asset";
        if (File.Exists(path))
            return;

        var texture = CreateRoundedTexture(radius, Color.white);
        texture.name = name + "Texture";
        AssetDatabase.CreateAsset(texture, path);

        var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        sprite.name = name;
        AssetDatabase.AddObjectToAsset(sprite, texture);
        AssetDatabase.SaveAssets();
    }

    private static Texture2D CreateRoundedTexture(int radius, Color color)
    {
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var alpha = GetRoundedRectAlpha(x, y, size, radius);
                texture.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * alpha));
            }
        }

        texture.Apply();
        return texture;
    }

    private static float GetRoundedRectAlpha(int x, int y, int size, int radius)
    {
        var px = Mathf.Min(x, size - 1 - x);
        var py = Mathf.Min(y, size - 1 - y);

        if (px >= radius || py >= radius)
            return 1f;

        var dx = radius - px;
        var dy = radius - py;
        return dx * dx + dy * dy <= radius * radius ? 1f : 0f;
    }
}
