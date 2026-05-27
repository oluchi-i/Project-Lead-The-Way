using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class SelectionPanelsUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject objectPanel;
    [SerializeField] private GameObject actionPanel;

    [Header("Object Panel")]
    [SerializeField] private Text objectPanelTitle;
    [SerializeField] private Transform objectButtonContainer;
    [SerializeField] private Button objectButtonTemplate;
    [SerializeField] private int objectsPerPage = 6;
    [SerializeField] private Button objectPreviousPageButton;
    [SerializeField] private Button objectNextPageButton;
    [SerializeField] private Text objectPageText;
    [SerializeField] private Sprite previousPageIcon;
    [SerializeField] private Sprite nextPageIcon;

    [Header("Action Panel")]
    [SerializeField] private Text actionPanelTitle;
    [SerializeField] private Transform actionButtonContainer;
    [SerializeField] private Button actionButtonTemplate;
    [SerializeField] private Button backButton;
    [SerializeField] private Sprite defaultActionIcon;

    [Header("Sounds")]
    [SerializeField] private AudioClip objectClickSound;
    [SerializeField] private AudioClip actionClickSound;
    [SerializeField] private AudioClip backClickSound;
    [SerializeField] private AudioSource audioSource;

    [Header("Scene Highlight")]
    [SerializeField] private bool showSceneHighlight = true;
    [SerializeField] private Color highlightColor = new Color(1f, 0.72f, 0.12f, 0.75f);
    [SerializeField, Range(1.01f, 1.2f), Tooltip("How far the selected object's outline expands from the original mesh.")]
    private float highlightScale = 1.12f;

    [Header("Scene Objects")]
    [SerializeField] private List<SelectableControlObject> objects = new List<SelectableControlObject>();

    private readonly List<GameObject> highlightOutlines = new List<GameObject>();
    private Material highlightMaterial;
    private int objectPage;
    private static readonly Color NavigationButtonColor = new Color(0.16f, 0.11f, 0.05f, 0.88f);
    private static readonly Color NavigationTextColor = new Color(1f, 0.93f, 0.72f, 1f);

    private void Awake()
    {
        if (backButton != null)
        {
            backButton.onClick.AddListener(GoBackToObjects);
            StyleBackButton();
        }

        EnsureObjectPaginationButtons();

        if (objectPreviousPageButton != null)
            objectPreviousPageButton.onClick.AddListener(ShowPreviousObjectPage);

        if (objectNextPageButton != null)
            objectNextPageButton.onClick.AddListener(ShowNextObjectPage);

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        HideTemplate(objectButtonTemplate);
        HideTemplate(actionButtonTemplate);
    }

    private void Start()
    {
        RefreshObjects();
        ShowObjects();
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (actionPanel != null && actionPanel.activeSelf && keyboard.bKey.wasPressedThisFrame)
        {
            GoBackToObjects();
            return;
        }

        var pressedNumber = GetPressedNumber();
        if (pressedNumber < 1)
            return;

        if (objectPanel != null && objectPanel.activeSelf)
            SelectObjectBySlot(pressedNumber);
        else if (actionPanel != null && actionPanel.activeSelf)
            InvokeActionBySlot(pressedNumber);
    }

    public void RefreshObjects()
    {
        objects = FindObjectsByType<SelectableControlObject>(FindObjectsSortMode.None)
            .OrderBy(item => item.slotNumber)
            .ToList();
    }

    public void ShowObjects()
    {
        if (!IsConfigured())
            return;

        currentObject = null;
        HideSceneHighlight();
        objectPanel.SetActive(true);
        actionPanel.SetActive(false);

        RenderObjectPage();
    }

    private void ShowActions(SelectableControlObject selectedObject)
    {
        currentObject = selectedObject;
        ShowSceneHighlight(selectedObject);
        objectPanel.SetActive(false);
        actionPanel.SetActive(true);
        actionPanelTitle.text = selectedObject.displayName;

        ClearContainer(actionButtonContainer, actionButtonTemplate);

        for (var i = 0; i < selectedObject.actions.Count; i++)
            CreateActionButton(selectedObject.actions[i], i + 1);
    }

    private void CreateObjectButton(SelectableControlObject item)
    {
        var button = Instantiate(objectButtonTemplate, objectButtonContainer);
        button.name = item.displayName + " Button";
        button.gameObject.SetActive(true);
        button.onClick.AddListener(() =>
        {
            PlaySound(objectClickSound);
            ShowActions(item);
            ClearFocus();
        });

        SetChildText(button.transform, "Number", item.slotNumber.ToString());
        SetChildImage(button.transform, "Icon", item.icon);
        SetChildText(button.transform, "Fallback Icon", GetFallbackIconText(item.displayName));
        SetChildActive(button.transform, "Fallback Icon", item.icon == null);
    }

    private void RenderObjectPage()
    {
        var pageSize = Mathf.Max(1, objectsPerPage);
        var maxPage = Mathf.Max(0, Mathf.CeilToInt(objects.Count / (float)pageSize) - 1);
        objectPage = Mathf.Clamp(objectPage, 0, maxPage);

        objectPanelTitle.text = "SELECT OBJECT";
        SetPageText(maxPage > 0 ? $"{objectPage + 1}/{maxPage + 1}" : string.Empty);

        ClearContainer(objectButtonContainer, objectButtonTemplate);

        var start = objectPage * pageSize;
        var end = Mathf.Min(start + pageSize, objects.Count);
        for (var i = start; i < end; i++)
            CreateObjectButton(objects[i]);

        SetPageButtonState(objectPreviousPageButton, objectPage > 0);
        SetPageButtonState(objectNextPageButton, objectPage < maxPage);
    }

    private void ShowPreviousObjectPage()
    {
        if (objectPage <= 0)
        {
            objectPage = 0;
            return;
        }

        objectPage--;

        PlaySound(backClickSound);
        RenderObjectPage();
        ClearFocus();
    }

    private void ShowNextObjectPage()
    {
        var pageSize = Mathf.Max(1, objectsPerPage);
        var maxPage = Mathf.Max(0, Mathf.CeilToInt(objects.Count / (float)pageSize) - 1);
        if (objectPage >= maxPage)
        {
            objectPage = maxPage;
            return;
        }

        objectPage++;

        PlaySound(objectClickSound);
        RenderObjectPage();
        ClearFocus();
    }

    private void SetPageButtonState(Button button, bool isVisible)
    {
        if (button == null)
            return;

        button.gameObject.SetActive(true);
        button.interactable = isVisible;

        var opacity = isVisible ? 1f : 0.32f;
        foreach (var graphic in button.GetComponentsInChildren<Graphic>())
        {
            var color = graphic.color;
            color.a = opacity;
            graphic.color = color;
        }
    }

    private void EnsureObjectPaginationButtons()
    {
        if (objectPanel == null)
            return;

        if (previousPageIcon == null)
            previousPageIcon = LoadEditorSprite("Assets/Art/UI/ButtonSet/Textures/icons/128x128/arrow_left.png");

        if (nextPageIcon == null)
            nextPageIcon = LoadEditorSprite("Assets/Art/UI/ButtonSet/Textures/icons/128x128/arrow_right.png");

        if (objectPreviousPageButton == null)
            objectPreviousPageButton = CreatePaginationButton("Previous Object Page", previousPageIcon, new Vector2(-74f, -8f), objectPanel.transform);

        if (objectNextPageButton == null)
            objectNextPageButton = CreatePaginationButton("Next Object Page", nextPageIcon, new Vector2(-18f, -8f), objectPanel.transform);

        if (objectPageText == null)
            objectPageText = CreatePaginationText("Object Page Text", new Vector2(-46f, -8f), objectPanel.transform);
    }

    private Button CreatePaginationButton(string name, Sprite icon, Vector2 anchoredPosition, Transform parent)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(18f, 16f);

        var image = buttonObject.GetComponent<Image>();
        image.color = NavigationButtonColor;

        var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(buttonObject.transform, false);
        var iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(10f, 10f);

        var iconImage = iconObject.GetComponent<Image>();
        iconImage.sprite = icon;
        iconImage.color = NavigationTextColor;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        var button = buttonObject.GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        return button;
    }

    private Text CreatePaginationText(string name, Vector2 anchoredPosition, Transform parent)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);

        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(34f, 16f);

        var text = textObject.GetComponent<Text>();
        text.font = objectPanelTitle != null ? objectPanelTitle.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 11;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.24f, 0.17f, 0.08f, 0.82f);
        text.raycastTarget = false;
        return text;
    }

    private void StyleBackButton()
    {
        var image = backButton.GetComponent<Image>();
        if (image != null)
            image.color = NavigationButtonColor;

        var label = backButton.GetComponentInChildren<Text>(true);
        if (label != null)
            label.color = NavigationTextColor;
    }

    private void SetPageText(string value)
    {
        if (objectPageText == null)
            return;

        objectPageText.text = value;
        objectPageText.gameObject.SetActive(!string.IsNullOrEmpty(value));
    }

    private Sprite LoadEditorSprite(string path)
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
#else
        return null;
#endif
    }

    private void CreateActionButton(ControlAction action, int slotNumber)
    {
        var button = Instantiate(actionButtonTemplate, actionButtonContainer);
        button.name = action.label + " Button";
        button.gameObject.SetActive(true);
        button.onClick.AddListener(() =>
        {
            PlaySound(actionClickSound);
            action.Invoke();
            ClearFocus();
        });

        var icon = action.icon != null ? action.icon : defaultActionIcon;

        SetChildText(button.transform, "Number", slotNumber.ToString());
        SetChildText(button.transform, "Label", action.label);
        SetChildImage(button.transform, "Icon", icon);
        SetChildText(button.transform, "Fallback Icon", GetFallbackIconText(action.label));
        SetChildActive(button.transform, "Fallback Icon", icon == null);
        SetChildActive(button.transform, "Label", false);
    }

    private SelectableControlObject currentObject;

    private void SelectObjectBySlot(int slotNumber)
    {
        var selected = objects.FirstOrDefault(item => item.slotNumber == slotNumber);
        if (selected != null)
        {
            ShowActions(selected);
            ClearFocus();
            PlaySound(objectClickSound);
        }
    }

    private void InvokeActionBySlot(int slotNumber)
    {
        if (currentObject == null)
            return;

        var index = slotNumber - 1;
        if (index >= 0 && index < currentObject.actions.Count)
        {
            PlaySound(actionClickSound);
            currentObject.actions[index].Invoke();
            ClearFocus();
        }
    }

    private void GoBackToObjects()
    {
        PlaySound(backClickSound);
        ShowObjects();
        ClearFocus();
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    private void ShowSceneHighlight(SelectableControlObject selectedObject)
    {
        if (!showSceneHighlight || selectedObject == null)
            return;

        HideSceneHighlight();

        var meshFilters = selectedObject.GetComponentsInChildren<MeshFilter>();
        foreach (var meshFilter in meshFilters)
            CreateOutline(meshFilter);
    }

    private void HideSceneHighlight()
    {
        for (var i = highlightOutlines.Count - 1; i >= 0; i--)
        {
            if (highlightOutlines[i] != null)
                Destroy(highlightOutlines[i]);
        }

        highlightOutlines.Clear();
    }

    private void CreateOutline(MeshFilter sourceMesh)
    {
        if (sourceMesh == null || sourceMesh.sharedMesh == null)
            return;

        var sourceRenderer = sourceMesh.GetComponent<MeshRenderer>();
        if (sourceRenderer == null || !sourceRenderer.enabled)
            return;

        if (highlightMaterial == null)
            highlightMaterial = CreateHighlightMaterial();

        var outline = new GameObject(sourceMesh.name + " Selection Outline", typeof(MeshFilter), typeof(MeshRenderer));
        outline.transform.SetParent(sourceMesh.transform, false);
        outline.transform.localPosition = Vector3.zero;
        outline.transform.localRotation = Quaternion.identity;
        outline.transform.localScale = Vector3.one * highlightScale;

        outline.GetComponent<MeshFilter>().sharedMesh = sourceMesh.sharedMesh;

        var renderer = outline.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = highlightMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        highlightOutlines.Add(outline);
    }

    private Material CreateHighlightMaterial()
    {
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        var material = new Material(shader);
        material.color = highlightColor;
        material.SetColor("_BaseColor", highlightColor);
        material.SetColor("_Color", highlightColor);
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_Cull", (float)CullMode.Front);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)RenderQueue.Transparent;
        return material;
    }

    private void OnDestroy()
    {
        HideSceneHighlight();

        if (highlightMaterial != null)
            Destroy(highlightMaterial);
    }

    private void ClearFocus()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private int GetPressedNumber()
    {
        var keyboard = Keyboard.current;

        if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) return 1;
        if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) return 2;
        if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) return 3;
        if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame) return 4;
        if (keyboard.digit5Key.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame) return 5;
        if (keyboard.digit6Key.wasPressedThisFrame || keyboard.numpad6Key.wasPressedThisFrame) return 6;
        if (keyboard.digit7Key.wasPressedThisFrame || keyboard.numpad7Key.wasPressedThisFrame) return 7;
        if (keyboard.digit8Key.wasPressedThisFrame || keyboard.numpad8Key.wasPressedThisFrame) return 8;
        if (keyboard.digit9Key.wasPressedThisFrame || keyboard.numpad9Key.wasPressedThisFrame) return 9;

        return -1;
    }

    private void SetChildText(Transform root, string childName, string value)
    {
        var child = FindDeep(root, childName);
        if (child == null)
            return;

        var text = child.GetComponent<Text>();
        if (text != null)
            text.text = value;
    }

    private void SetChildImage(Transform root, string childName, Sprite sprite)
    {
        var child = FindDeep(root, childName);
        if (child == null)
            return;

        var image = child.GetComponent<Image>();
        if (image == null)
            return;

        image.sprite = sprite;
        image.enabled = sprite != null;
    }

    private void SetChildActive(Transform root, string childName, bool active)
    {
        var child = FindDeep(root, childName);
        if (child != null)
            child.gameObject.SetActive(active);
    }

    private Transform FindDeep(Transform root, string childName)
    {
        if (root.name == childName)
            return root;

        foreach (Transform child in root)
        {
            var match = FindDeep(child, childName);
            if (match != null)
                return match;
        }

        return null;
    }

    private string GetFallbackIconText(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "?" : value.Substring(0, 1).ToUpperInvariant();
    }

    private void ClearContainer(Transform container, Button template)
    {
        for (var i = container.childCount - 1; i >= 0; i--)
        {
            var child = container.GetChild(i);
            if (template != null && child == template.transform)
                continue;

            Destroy(child.gameObject);
        }
    }

    private void HideTemplate(Button template)
    {
        if (template != null)
            template.gameObject.SetActive(false);
    }

    private bool IsConfigured()
    {
        return objectPanel != null
            && actionPanel != null
            && objectPanelTitle != null
            && objectButtonContainer != null
            && objectButtonTemplate != null
            && actionPanelTitle != null
            && actionButtonContainer != null
            && actionButtonTemplate != null
            && backButton != null;
    }
}
