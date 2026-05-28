using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static partial class LeadTheWayObjectSetupTools
{
        [MenuItem("Tools/Lead The Way/UI/Setup Gameplay UI")]
        public static void SetupGameplayUI()
        {
            var canvas = FindOrCreateCanvas();
            var controlUI = SetupStableControlUI(canvas);
            SetupInteractionCounter(canvas);
            SetupLevelResultFlash(canvas);
    
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            Selection.activeGameObject = controlUI;
            EditorGUIUtility.PingObject(controlUI);
            EditorUtility.DisplayDialog("Setup Gameplay UI", "Gameplay UI is styled, parented under Canvas, and wired.", "OK");
        }

        private static GameObject SetupStableControlUI(Canvas canvas)
        {
            var selectionUI = UnityEngine.Object.FindAnyObjectByType<SelectionPanelsUI>(FindObjectsInactive.Include);
            if (selectionUI == null)
                selectionUI = CreateControlUIFromPrefab(canvas);
    
            if (selectionUI == null)
            {
                var controlObject = new GameObject("Control UI", typeof(RectTransform), typeof(SelectionPanelsUI), typeof(AudioSource));
                Undo.RegisterCreatedObjectUndo(controlObject, "Create Control UI");
                selectionUI = controlObject.GetComponent<SelectionPanelsUI>();
            }
    
            var root = selectionUI.gameObject;
            Undo.SetTransformParent(root.transform, canvas.transform, "Parent Control UI To Canvas");
            root.transform.SetAsLastSibling();
            root.name = "Control UI";
            root.SetActive(true);
    
            ConfigureRect(root.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(520f, 82f));
    
            var audioSource = GetOrAddComponent<AudioSource>(root);
            var objectPanel = EnsurePanel(root.transform, "Object Panel");
            var actionPanel = EnsurePanel(root.transform, "Action Panel");
            StyleMainPanel(objectPanel, true);
            StyleMainPanel(actionPanel, false);
    
            var objectTitle = EnsureText(objectPanel.transform, "Title", "SELECT OBJECT", 11, FontStyle.Bold, TextAnchor.UpperLeft);
            StyleTitle(objectTitle, "SELECT OBJECT");
    
            var objectContainer = EnsureHorizontalContainer(objectPanel.transform, "Object Button Container");
            ConfigureButtonContainer(objectContainer.transform);
    
            var objectTemplate = EnsureControlButton(objectContainer.transform, "Object Button Template", true);
            StyleControlButton(objectTemplate);
    
            var previousIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/ButtonSet/Textures/icons/128x128/arrow_left.png");
            var nextIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/ButtonSet/Textures/icons/128x128/arrow_right.png");
            var playIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/ButtonSet/Textures/icons/128x128/play.png");
    
            var previousButton = EnsureSmallIconButton(objectPanel.transform, "Previous Object Page", previousIcon, new Vector2(-74f, -8f));
            var nextButton = EnsureSmallIconButton(objectPanel.transform, "Next Object Page", nextIcon, new Vector2(-18f, -8f));
            var pageText = EnsureText(objectPanel.transform, "Object Page Text", "1/1", 13, FontStyle.Bold, TextAnchor.MiddleCenter);
            ConfigureRect(pageText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(-46f, -8f), new Vector2(38f, 16f));
            pageText.color = new Color(0.24f, 0.17f, 0.08f, 0.9f);
            pageText.raycastTarget = false;
    
            var actionTitle = EnsureText(actionPanel.transform, "Title", "ACTION", 11, FontStyle.Bold, TextAnchor.UpperLeft);
            StyleTitle(actionTitle, "ACTION");
    
            var actionContainer = EnsureHorizontalContainer(actionPanel.transform, "Action Button Container");
            ConfigureButtonContainer(actionContainer.transform);
    
            var actionTemplate = EnsureControlButton(actionContainer.transform, "Action Button Template", true);
            StyleControlButton(actionTemplate);
    
            var backButton = EnsureBackButton(actionPanel.transform);
    
            var serialized = new SerializedObject(selectionUI);
            SetObject(serialized, "objectPanel", objectPanel);
            SetObject(serialized, "actionPanel", actionPanel);
            SetObject(serialized, "objectPanelTitle", objectTitle);
            SetObject(serialized, "objectButtonContainer", objectContainer.transform);
            SetObject(serialized, "objectButtonTemplate", objectTemplate);
            SetObject(serialized, "objectPreviousPageButton", previousButton);
            SetObject(serialized, "objectNextPageButton", nextButton);
            SetObject(serialized, "objectPageText", pageText);
            SetObject(serialized, "previousPageIcon", previousIcon);
            SetObject(serialized, "nextPageIcon", nextIcon);
            SetObject(serialized, "actionPanelTitle", actionTitle);
            SetObject(serialized, "actionButtonContainer", actionContainer.transform);
            SetObject(serialized, "actionButtonTemplate", actionTemplate);
            SetObject(serialized, "backButton", backButton);
            SetObject(serialized, "defaultActionIcon", playIcon);
            SetObject(serialized, "audioSource", audioSource);
            SetObject(serialized, "interactionFlowManager", UnityEngine.Object.FindAnyObjectByType<InteractionFlowManager>());
            serialized.ApplyModifiedProperties();
    
            objectPanel.SetActive(true);
            actionPanel.SetActive(false);
    
            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(selectionUI);
            return root;
        }

        private static SelectionPanelsUI CreateControlUIFromPrefab(Canvas canvas)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ControlUIPrefabPath);
            if (prefab == null)
                return null;
    
            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
                return null;
    
            Undo.RegisterCreatedObjectUndo(instance, "Create Control UI From Prefab");
    
            var selectionUI = instance.GetComponentInChildren<SelectionPanelsUI>(true);
            if (selectionUI == null)
            {
                Undo.DestroyObjectImmediate(instance);
                return null;
            }
    
            Undo.SetTransformParent(selectionUI.transform, canvas.transform, "Parent Control UI To Canvas");
    
            if (instance != selectionUI.gameObject)
                Undo.DestroyObjectImmediate(instance);
    
            return selectionUI;
        }

        private static void SetupInteractionCounter(Canvas canvas)
        {
            var flowManager = UnityEngine.Object.FindAnyObjectByType<InteractionFlowManager>();
            if (flowManager == null)
                flowManager = Undo.AddComponent<InteractionFlowManager>(GetOrCreateGameSystems());
    
            var counterUI = UnityEngine.Object.FindAnyObjectByType<InteractionCounterUI>();
            var counterRoot = counterUI != null ? counterUI.gameObject : null;
    
            if (counterRoot == null)
            {
                counterRoot = new GameObject("Interaction Counter", typeof(RectTransform), typeof(Image), typeof(InteractionCounterUI));
                Undo.RegisterCreatedObjectUndo(counterRoot, "Create Interaction Counter");
                counterUI = counterRoot.GetComponent<InteractionCounterUI>();
            }
    
            Undo.SetTransformParent(counterRoot.transform, canvas.transform, "Parent Interaction Counter To Canvas");
            counterRoot.transform.SetAsLastSibling();
            counterRoot.SetActive(true);
    
            ConfigureRect(counterRoot.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(82f, 82f));
    
            var background = counterRoot.GetComponent<Image>();
            background.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PanelSoftPath);
            background.type = background.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            background.color = new Color(0.16f, 0.11f, 0.05f, 0.88f);
    
            var dialSprite = AssetDatabase.LoadAssetAtPath<Sprite>(MediumCountdownDialPath);
            var dialBackground = EnsureChild(counterRoot.transform, "Countdown Dial Background", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            ConfigureCountdownDialImage(dialBackground, dialSprite, new Color(1f, 0.93f, 0.72f, 0.2f), false);
    
            var dialFill = EnsureChild(counterRoot.transform, "Countdown Dial Fill", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            ConfigureCountdownDialImage(dialFill, dialSprite, new Color(1f, 0.68f, 0.03f, 1f), true);
    
            var staleLabelText = FindDirect(counterRoot.transform, "Label");
            if (staleLabelText != null)
                Undo.DestroyObjectImmediate(staleLabelText.gameObject);
    
            var countText = FindOrCreateText(counterRoot.transform, "Count");
            ConfigureCounterText(countText, flowManager.MaxInteractionCount.ToString(), 34, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(10f, -10f), new Vector2(62f, 62f), new Color(1f, 0.93f, 0.72f, 1f));
    
            var staleMaxText = FindDirect(counterRoot.transform, "Max");
            if (staleMaxText != null)
                Undo.DestroyObjectImmediate(staleMaxText.gameObject);
    
            Undo.RecordObject(counterUI, "Setup Interaction Counter");
            counterUI.Configure(flowManager, countText, dialFill);
            EditorUtility.SetDirty(counterUI);
            EditorUtility.SetDirty(counterRoot);
        }

        private static void SetupLevelResultFlash(Canvas canvas)
        {
            var flowManager = UnityEngine.Object.FindAnyObjectByType<InteractionFlowManager>();
            if (flowManager == null)
                flowManager = Undo.AddComponent<InteractionFlowManager>(GetOrCreateGameSystems());
    
            var flashUI = UnityEngine.Object.FindAnyObjectByType<LevelResultFlashUI>(FindObjectsInactive.Include);
            var flashRoot = flashUI != null ? flashUI.gameObject : null;
    
            if (flashRoot == null)
            {
                flashRoot = new GameObject("Level Result Flash", typeof(RectTransform), typeof(Image), typeof(LevelResultFlashUI));
                Undo.RegisterCreatedObjectUndo(flashRoot, "Create Level Result Flash");
                flashUI = flashRoot.GetComponent<LevelResultFlashUI>();
            }
    
            Undo.SetTransformParent(flashRoot.transform, canvas.transform, "Parent Level Result Flash To Canvas");
            flashRoot.transform.SetAsLastSibling();
            flashRoot.SetActive(true);
    
            ConfigureRect(flashRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
    
            var image = flashRoot.GetComponent<Image>();
            image.color = new Color(1f, 0f, 0f, 0f);
            image.raycastTarget = false;
            image.enabled = false;
    
            Undo.RecordObject(flashUI, "Setup Level Result Flash");
            flashUI.Configure(image);
    
            Undo.RecordObject(flowManager, "Wire Level Result Flash");
            flowManager.ConfigureResultFlash(flashUI);
    
            EditorUtility.SetDirty(flashUI);
            EditorUtility.SetDirty(flowManager);
            EditorUtility.SetDirty(flashRoot);
        }

        private static void StyleMainPanel(GameObject panel, bool isObjectPanel)
        {
            ConfigureRect(panel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
    
            var image = panel.GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PanelSoftPath);
            image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = new Color(0.96f, 0.92f, 0.78f, 0.97f);
            image.raycastTarget = true;
    
            panel.SetActive(isObjectPanel);
        }

        private static void ConfigureButtonContainer(Transform container)
        {
            ConfigureRect(container.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(-24f, -34f));
            var layout = container.GetComponent<HorizontalLayoutGroup>() ?? Undo.AddComponent<HorizontalLayoutGroup>(container.gameObject);
            layout.spacing = 10f;
            layout.padding = new RectOffset(8, 8, 6, 4);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        private static void StyleControlButton(Button button)
        {
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            ConfigureRect(button.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(54f, 54f));
    
            var image = button.GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonRoundPath);
            image.type = Image.Type.Simple;
            image.color = Color.white;
            image.preserveAspect = true;
    
            var layout = button.GetComponent<LayoutElement>() ?? Undo.AddComponent<LayoutElement>(button.gameObject);
            layout.preferredWidth = 54f;
            layout.preferredHeight = 54f;
    
            var icon = FindDeep(button.transform, "Icon")?.GetComponent<Image>();
            if (icon != null)
            {
                ConfigureRect(icon.rectTransform, new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20f, 20f));
                icon.color = new Color(0.13f, 0.11f, 0.09f, 1f);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
            }
    
            var number = FindDeep(button.transform, "Number")?.GetComponent<Text>();
            if (number != null)
            {
                ConfigureRect(number.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 3f), new Vector2(24f, 15f));
                number.font = GetUIFont();
                number.fontSize = 10;
                number.fontStyle = FontStyle.Bold;
                number.alignment = TextAnchor.MiddleCenter;
                number.color = new Color(1f, 0.93f, 0.72f, 1f);
                number.raycastTarget = false;
                EnsureNumberBackground(button.transform, number.transform.GetSiblingIndex());
            }
    
            var fallback = FindDeep(button.transform, "Fallback Icon")?.GetComponent<Text>();
            if (fallback != null)
            {
                ConfigureRect(fallback.rectTransform, new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34f, 26f));
                fallback.font = GetUIFont();
                fallback.fontSize = 14;
                fallback.fontStyle = FontStyle.Bold;
                fallback.color = new Color(0.13f, 0.11f, 0.09f, 1f);
                fallback.raycastTarget = false;
            }
    
            var label = FindDeep(button.transform, "Label")?.GetComponent<Text>();
            if (label != null)
                label.gameObject.SetActive(false);
    
            var numberTransform = FindDeep(button.transform, "Number");
            if (numberTransform != null)
                numberTransform.SetAsLastSibling();
    
            button.gameObject.SetActive(false);
        }

        private static void EnsureNumberBackground(Transform button, int numberSiblingIndex)
        {
            var background = FindDirect(button, "Number Background");
            if (background == null)
                background = EnsureChild(button, "Number Background", typeof(RectTransform), typeof(Image)).transform;
    
            background.SetSiblingIndex(Mathf.Max(0, numberSiblingIndex));
            var image = background.GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BadgeSoftPath);
            image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = new Color(0.16f, 0.11f, 0.05f, 0.92f);
            image.raycastTarget = false;
            ConfigureRect(background.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 3f), new Vector2(24f, 15f));
        }

        private static void StyleTitle(Text title, string value)
        {
            if (title == null)
                return;
    
            title.text = value;
            title.font = GetUIFont();
            title.fontSize = 11;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.UpperLeft;
            title.color = new Color(0.18f, 0.12f, 0.05f, 1f);
            title.raycastTarget = false;
            ConfigureRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(-20f, 18f));
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name)
                return root;
    
            foreach (Transform child in root)
            {
                var result = FindDeep(child, name);
                if (result != null)
                    return result;
            }
    
            return null;
        }

        private static Transform FindDirect(Transform root, string name)
        {
            foreach (Transform child in root)
            {
                if (child.name == name)
                    return child;
            }
    
            return null;
        }

        private static Font GetUIFont()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(PoppinsBoldPath);
            return font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static GameObject EnsurePanel(Transform parent, string name)
        {
            var panel = EnsureChild(parent, name, typeof(RectTransform), typeof(Image));
            ConfigureRect(panel.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
    
            var image = panel.GetComponent<Image>();
            image.color = new Color(0.94f, 0.9f, 0.76f, 0.96f);
            image.raycastTarget = true;
            return panel;
        }

        private static GameObject EnsureHorizontalContainer(Transform parent, string name)
        {
            var container = EnsureChild(parent, name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            var layout = container.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.padding = new RectOffset(8, 8, 6, 4);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return container;
        }

        private static Button EnsureControlButton(Transform parent, string name, bool startHidden)
        {
            var buttonObject = EnsureChild(parent, name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            ConfigureRect(buttonObject.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(58f, 58f));
    
            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(1f, 0.69f, 0.04f, 1f);
    
            var layout = buttonObject.GetComponent<LayoutElement>();
            layout.preferredWidth = 58f;
            layout.preferredHeight = 58f;
    
            var button = buttonObject.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
    
            var icon = EnsureChild(buttonObject.transform, "Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            ConfigureRect(icon.rectTransform, new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24f, 24f));
            icon.color = new Color(0.18f, 0.12f, 0.05f, 1f);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
    
            var fallback = EnsureText(buttonObject.transform, "Fallback Icon", "?", 15, FontStyle.Bold, TextAnchor.MiddleCenter);
            ConfigureRect(fallback.rectTransform, new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34f, 28f));
            fallback.color = new Color(0.18f, 0.12f, 0.05f, 1f);
            fallback.raycastTarget = false;
    
            var numberBackground = EnsureChild(buttonObject.transform, "Number Background", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            ConfigureRect(numberBackground.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(22f, 16f));
            numberBackground.color = new Color(0.16f, 0.11f, 0.05f, 0.92f);
            numberBackground.raycastTarget = false;
    
            var number = EnsureText(buttonObject.transform, "Number", "1", 11, FontStyle.Bold, TextAnchor.MiddleCenter);
            ConfigureRect(number.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(22f, 16f));
            number.color = new Color(1f, 0.93f, 0.72f, 1f);
            number.raycastTarget = false;
    
            var label = EnsureText(buttonObject.transform, "Label", "ACTION", 11, FontStyle.Bold, TextAnchor.MiddleCenter);
            ConfigureRect(label.rectTransform, new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(52f, 24f));
            label.color = new Color(0.18f, 0.12f, 0.05f, 1f);
            label.raycastTarget = false;
    
            buttonObject.SetActive(!startHidden);
            return button;
        }

        private static Button EnsureSmallIconButton(Transform parent, string name, Sprite iconSprite, Vector2 anchoredPosition)
        {
            var buttonObject = EnsureChild(parent, name, typeof(RectTransform), typeof(Image), typeof(Button));
            ConfigureRect(buttonObject.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), anchoredPosition, new Vector2(18f, 16f));
    
            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.16f, 0.11f, 0.05f, 0.88f);
    
            var icon = EnsureChild(buttonObject.transform, "Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            ConfigureRect(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10f, 10f));
            icon.sprite = iconSprite;
            icon.color = new Color(1f, 0.93f, 0.72f, 1f);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
    
            var button = buttonObject.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            return button;
        }

        private static Button EnsureBackButton(Transform parent)
        {
            var buttonObject = EnsureChild(parent, "Back Button", typeof(RectTransform), typeof(Image), typeof(Button));
            ConfigureRect(buttonObject.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-8f, -7f), new Vector2(54f, 20f));
    
            var image = buttonObject.GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SmallPanelPath);
            image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = new Color(0.16f, 0.11f, 0.05f, 0.88f);
    
            var label = EnsureText(buttonObject.transform, "Label", "BACK", 8, FontStyle.Bold, TextAnchor.MiddleCenter);
            ConfigureRect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            label.font = GetUIFont();
            label.fontSize = 9;
            label.color = new Color(1f, 0.93f, 0.72f, 1f);
            label.raycastTarget = false;
    
            var button = buttonObject.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            return button;
        }

        private static Text EnsureText(Transform parent, string name, string value, int fontSize, FontStyle fontStyle, TextAnchor alignment)
        {
            var textObject = EnsureChild(parent, name, typeof(RectTransform), typeof(Text));
            var text = textObject.GetComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = new Color(0.12f, 0.12f, 0.12f, 1f);
            return text;
        }

        private static GameObject EnsureChild(Transform parent, string name, params Type[] componentTypes)
        {
            var child = parent.Find(name);
            GameObject childObject;
            if (child == null)
            {
                childObject = new GameObject(name, componentTypes);
                Undo.RegisterCreatedObjectUndo(childObject, "Create UI Element");
                childObject.transform.SetParent(parent, false);
            }
            else
            {
                childObject = child.gameObject;
            }
    
            foreach (var componentType in componentTypes)
            {
                if (childObject.GetComponent(componentType) == null)
                    Undo.AddComponent(childObject, componentType);
            }
    
            return childObject;
        }

        private static void ConfigureRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            Undo.RecordObject(rect, "Configure RectTransform");
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static void SetObject(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property != null)
                property.objectReferenceValue = value;
        }

        private static Canvas FindOrCreateCanvas()
        {
            var canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                ConfigureCanvas(canvas);
                return canvas;
            }
    
            var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasObject, "Create Canvas");
    
            canvas = canvasObject.GetComponent<Canvas>();
            ConfigureCanvas(canvas);
    
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
    
            return canvas;
        }

        private static void ConfigureCanvas(Canvas canvas)
        {
            Undo.RecordObject(canvas, "Configure Canvas");
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;
            canvas.gameObject.SetActive(true);
        }

        private static Text FindOrCreateText(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null && existing.TryGetComponent<Text>(out var existingText))
                return existingText;
    
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            Undo.RegisterCreatedObjectUndo(textObject, "Create Counter Text");
            textObject.transform.SetParent(parent, false);
            return textObject.GetComponent<Text>();
        }

        private static void ConfigureCounterText(Text text, string value, int fontSize, FontStyle fontStyle, TextAnchor alignment, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            Undo.RecordObject(text, "Setup Counter Text");
            Undo.RecordObject(text.rectTransform, "Setup Counter Text");
    
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
    
            text.rectTransform.anchorMin = new Vector2(0f, 1f);
            text.rectTransform.anchorMax = new Vector2(0f, 1f);
            text.rectTransform.pivot = new Vector2(0f, 1f);
            text.rectTransform.anchoredPosition = anchoredPosition;
            text.rectTransform.sizeDelta = size;
        }

        private static void ConfigureCountdownDialImage(Image image, Sprite sprite, Color color, bool filled)
        {
            Undo.RecordObject(image, "Setup Countdown Dial");
            Undo.RecordObject(image.rectTransform, "Setup Countdown Dial");
    
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.type = filled && sprite != null ? Image.Type.Filled : Image.Type.Simple;
    
            if (filled)
            {
                image.fillMethod = Image.FillMethod.Radial360;
                image.fillOrigin = (int)Image.Origin360.Top;
                image.fillClockwise = false;
                image.fillAmount = 1f;
            }
    
            image.rectTransform.anchorMin = new Vector2(0f, 1f);
            image.rectTransform.anchorMax = new Vector2(0f, 1f);
            image.rectTransform.pivot = new Vector2(0f, 1f);
            image.rectTransform.anchoredPosition = new Vector2(10f, -10f);
            image.rectTransform.sizeDelta = new Vector2(62f, 62f);
        }
}