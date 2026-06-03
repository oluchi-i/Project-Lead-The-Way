using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class LeadTheWayMapPathTools
{
    private const string RootName = "World Map Path System";
    private const string ButtonRoundSpritePath = "Assets/Art/UI/ButtonSet/Textures/buttons/button_round_130.png";
    private const string ArrowLeftIconPath = "Assets/Art/UI/ButtonSet/Textures/icons/128x128/arrow_left.png";
    private const string ArrowRightIconPath = "Assets/Art/UI/ButtonSet/Textures/icons/128x128/arrow_right.png";
    private const string PanelSoftPath = "Assets/Art/UI/Sprites/PanelSoft.asset";
    private const string WorldMapPlayerPrefabPath = "Assets/Prefabs/Player/Player.prefab";
    private const float MiniatureMapFieldOfView = 36f;
    private const float WorldMapPlayerVisualScale = 1f;
    private const float CameraLookAtHeightOffset = 2f;

    [MenuItem("Tools/Lead The Way/Map Path/Create Camera And Player Path Setup")]
    public static void CreateCameraAndPlayerPathSetup()
    {
        var root = GameObject.Find(RootName);
        if (root == null)
        {
            root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Create World Map Path System");
        }

        var cameraPath = EnsurePath(root.transform, "Camera Path", new Vector3(-4f, 6f, -8f), new Vector3(0f, 7f, -6f), new Vector3(4f, 6f, -8f));
        var playerPath = EnsurePath(root.transform, "Player Path", new Vector3(-4f, 0f, 0f), new Vector3(0f, 0f, 1.4f), new Vector3(4f, 0f, 0f));

        Selection.objects = new Object[] { cameraPath.gameObject, playerPath.gameObject };
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorUtility.DisplayDialog(
            "Map Path Setup",
            "Created Camera Path and Player Path with 2 invisible checkpoints and 1 invisible control point each.",
            "OK");
    }

    [MenuItem("Tools/Lead The Way/Map Path/Setup Checkpoint Navigator UI")]
    public static void SetupCheckpointNavigatorUI()
    {
        var canvas = EnsureWorldMapCanvas();
        var navigatorObject = FindOrCreateChild(canvas.transform, "Map Checkpoint Navigator");
        Undo.RegisterCompleteObjectUndo(navigatorObject, "Setup Checkpoint Navigator UI");
        navigatorObject.SetActive(true);

        var rect = EnsureRectTransform(navigatorObject);
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 28f);
        rect.sizeDelta = new Vector2(390f, 76f);

        var panelImage = EnsureComponent<Image>(navigatorObject);
        panelImage.sprite = LoadSprite(PanelSoftPath);
        panelImage.type = panelImage.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        panelImage.color = new Color(0.20f, 0.16f, 0.12f, 0.88f);

        var previousButton = EnsureIconButton(navigatorObject.transform, "Previous Checkpoint", ArrowLeftIconPath, new Vector2(-150f, 0f));
        var nextButton = EnsureIconButton(navigatorObject.transform, "Next Checkpoint", ArrowRightIconPath, new Vector2(150f, 0f));
        var startButton = EnsureStartButton(navigatorObject.transform);
        var playButton = EnsurePlayButton(navigatorObject.transform);
        var titleBackground = EnsureLevelTitleBackground(canvas.transform);
        var label = EnsureLevelTitle(titleBackground.transform);
        var legacyLabel = navigatorObject.transform.Find("Checkpoint Label");
        if (legacyLabel != null)
            legacyLabel.gameObject.SetActive(false);

        previousButton.gameObject.SetActive(false);
        nextButton.gameObject.SetActive(false);
        playButton.gameObject.SetActive(false);
        titleBackground.gameObject.SetActive(false);
        startButton.gameObject.SetActive(true);

        var navigator = EnsureComponent<MapCheckpointNavigatorUI>(navigatorObject);
        var cameraPath = FindMapPath("Camera Path");
        var playerPath = FindMapPath("Player Path");
        var playerFollower = EnsureWorldMapPlayerFollower(playerPath);
        var cameraFollower = EnsureWorldMapCameraFollower(cameraPath, playerFollower != null ? EnsureCameraLookTarget(playerFollower.transform) : null);
        navigator.Configure(
            cameraPath,
            playerPath,
            cameraFollower,
            playerFollower,
            previousButton,
            nextButton,
            startButton,
            playButton,
            label,
            panelImage,
            titleBackground);

        EnsureEventSystem();
        EditorUtility.SetDirty(navigatorObject);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = navigatorObject;
        EditorUtility.DisplayDialog(
            "Checkpoint Navigator UI",
            BuildNavigatorSetupMessage(canvas, navigatorObject, cameraPath, playerPath, cameraFollower, playerFollower),
            "OK");
    }

    [MenuItem("Tools/Lead The Way/Map Path/Apply Miniature Camera Zoom")]
    public static void ApplyMiniatureCameraZoom()
    {
        var camera = FindWorldMapCamera();
        if (camera == null)
        {
            Debug.LogWarning("Lead The Way Map Path: No scene camera was found for miniature zoom.");
            return;
        }

        camera.gameObject.name = "World Map Camera";
        camera.gameObject.tag = "MainCamera";
        camera.enabled = true;
        ApplyMiniatureMapCameraSettings(camera);
        DisableOtherSceneCameras(camera);
        EnsureSingleAudioListener(camera);

        EditorUtility.SetDirty(camera);
        EditorUtility.SetDirty(camera.gameObject);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = camera.gameObject;
        Debug.Log($"Lead The Way Map Path: Applied miniature camera view to {camera.gameObject.name}. Field Of View: {MiniatureMapFieldOfView}");
    }

    [MenuItem("Tools/Lead The Way/Map Path/Snap Camera To First Checkpoint")]
    public static void SnapCameraToFirstCheckpoint()
    {
        var cameraPath = FindMapPath("Camera Path");
        if (cameraPath == null || !cameraPath.TryGetCheckpointProgress(0, out var cameraProgress))
        {
            Debug.LogWarning("Lead The Way Map Path: Camera Path checkpoint 0 was not found.");
            return;
        }

        var camera = FindWorldMapCamera();
        if (camera == null)
        {
            Debug.LogWarning("Lead The Way Map Path: No scene camera was found.");
            return;
        }

        var player = GameObject.Find("World Map Player");
        if (player == null)
        {
            Debug.LogWarning("Lead The Way Map Path: World Map Player was not found.");
            return;
        }

        Undo.RecordObject(camera.transform, "Snap World Map Camera To First Checkpoint");
        camera.transform.position = cameraPath.GetPoint(cameraProgress);
        LookAt(camera.transform, player.transform.position + Vector3.up * CameraLookAtHeightOffset);

        camera.gameObject.name = "World Map Camera";
        camera.gameObject.tag = "MainCamera";
        camera.enabled = true;
        ApplyMiniatureMapCameraSettings(camera);
        DisableOtherSceneCameras(camera);
        EnsureSingleAudioListener(camera);

        EditorUtility.SetDirty(camera.transform);
        EditorUtility.SetDirty(camera);
        EditorUtility.SetDirty(camera.gameObject);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = camera.gameObject;
        Debug.Log("Lead The Way Map Path: Snapped World Map Camera to camera checkpoint 0 and aimed it at World Map Player.");
    }

    [MenuItem("Tools/Lead The Way/Map Path/Setup World Map Player Visual")]
    public static void SetupWorldMapPlayerVisual()
    {
        var playerPath = FindMapPath("Player Path");
        var playerFollower = EnsureWorldMapPlayerFollower(playerPath);
        if (playerFollower == null)
        {
            Debug.LogWarning("Lead The Way Map Path: Player Path is missing, so the world map player visual could not be set up.");
            return;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = playerFollower.gameObject;
        Debug.Log("Lead The Way Map Path: Set up World Map Player visual and walking animation.");
    }


    [MenuItem("Tools/Lead The Way/Map Path/Add Checkpoint To Selected Path")]
    public static void AddCheckpointToSelectedPath()
    {
        var path = GetSelectedPath();
        if (path == null)
            return;

        var checkpointCount = path.CheckpointCount;
        var position = path.transform.childCount > 0
            ? path.transform.GetChild(path.transform.childCount - 1).position + Vector3.right * 2f
            : path.transform.position;

        var checkpoint = CreateCheckpoint(path.transform, checkpointCount + 1, position);
        Selection.activeGameObject = checkpoint.gameObject;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    [MenuItem("Tools/Lead The Way/Map Path/Add Control Point After Selected")]
    public static void AddControlPointAfterSelected()
    {
        var selected = Selection.activeTransform;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("Map Path", "Select a checkpoint or control point first.", "OK");
            return;
        }

        var parentPath = selected.GetComponentInParent<MapPath>();
        if (parentPath == null || selected == parentPath.transform)
        {
            EditorUtility.DisplayDialog("Map Path", "Select a child checkpoint/control point under a MapPath.", "OK");
            return;
        }

        var insertIndex = selected.GetSiblingIndex() + 1;
        var position = selected.position + Vector3.forward;
        var controlPoint = CreateControlPoint(parentPath.transform, "Control Point", position);
        controlPoint.transform.SetSiblingIndex(insertIndex);

        Selection.activeGameObject = controlPoint.gameObject;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    [MenuItem("Tools/Lead The Way/Map Path/Add Checkpoint To Selected Path", true)]
    private static bool CanAddCheckpointToSelectedPath()
    {
        return GetSelectedPath(false) != null;
    }

    [MenuItem("Tools/Lead The Way/Map Path/Add Control Point After Selected", true)]
    private static bool CanAddControlPointAfterSelected()
    {
        var selected = Selection.activeTransform;
        return selected != null && selected.GetComponentInParent<MapPath>() != null && selected.GetComponent<MapPath>() == null;
    }

    private static MapPath GetSelectedPath(bool showDialog = true)
    {
        var path = Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponent<MapPath>()
            : null;

        if (path == null && showDialog)
            EditorUtility.DisplayDialog("Map Path", "Select a GameObject with a MapPath component.", "OK");

        return path;
    }

    private static MapPath EnsurePath(Transform root, string pathName, Vector3 checkpointA, Vector3 controlPoint, Vector3 checkpointB)
    {
        var pathTransform = root.Find(pathName);
        GameObject pathObject;
        if (pathTransform == null)
        {
            pathObject = new GameObject(pathName);
            Undo.RegisterCreatedObjectUndo(pathObject, "Create " + pathName);
            pathObject.transform.SetParent(root);
        }
        else
        {
            pathObject = pathTransform.gameObject;
        }

        var path = pathObject.GetComponent<MapPath>();
        if (path == null)
            path = Undo.AddComponent<MapPath>(pathObject);

        if (HasAnyMapPathPoint(pathObject.transform))
            return path;

        EnsurePoint(pathObject.transform, "Checkpoint 01", checkpointA, true, "checkpoint-01");
        EnsurePoint(pathObject.transform, "Control Point 01A", controlPoint, false, "control-point-01a");
        EnsurePoint(pathObject.transform, "Checkpoint 02", checkpointB, true, "checkpoint-02");
        return path;
    }

    private static bool HasAnyMapPathPoint(Transform parent)
    {
        for (var i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child.GetComponent<MapCheckpoint>() != null || child.GetComponent<MapPathControlPoint>() != null)
                return true;
        }

        return false;
    }

    private static void EnsurePoint(Transform parent, string name, Vector3 position, bool checkpoint, string id)
    {
        var child = parent.Find(name);
        GameObject pointObject;
        if (child == null)
        {
            pointObject = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(pointObject, "Create " + name);
            pointObject.transform.SetParent(parent);
        }
        else
        {
            pointObject = child.gameObject;
        }

        if (child == null)
            pointObject.transform.position = position;

        RemoveVisualComponents(pointObject);

        if (checkpoint)
        {
            var component = pointObject.GetComponent<MapCheckpoint>();
            if (component == null)
            {
                component = Undo.AddComponent<MapCheckpoint>(pointObject);
                component.Configure(id, LabelFromCheckpointId(id));
            }

            var control = pointObject.GetComponent<MapPathControlPoint>();
            if (control != null)
                Undo.DestroyObjectImmediate(control);
        }
        else
        {
            var component = pointObject.GetComponent<MapPathControlPoint>();
            if (component == null)
            {
                component = Undo.AddComponent<MapPathControlPoint>(pointObject);
                component.Configure(id);
            }

            var mapCheckpoint = pointObject.GetComponent<MapCheckpoint>();
            if (mapCheckpoint != null)
                Undo.DestroyObjectImmediate(mapCheckpoint);
        }

        EditorUtility.SetDirty(pointObject);
    }

    private static MapCheckpoint CreateCheckpoint(Transform parent, int number, Vector3 position)
    {
        var pointObject = new GameObject("Checkpoint " + number.ToString("00"));
        Undo.RegisterCreatedObjectUndo(pointObject, "Create Map Checkpoint");
        pointObject.transform.SetParent(parent);
        pointObject.transform.position = position;
        var checkpoint = Undo.AddComponent<MapCheckpoint>(pointObject);
        checkpoint.Configure("checkpoint-" + number.ToString("00"), number == 1 ? "Start" : "Level " + (number - 1));
        return checkpoint;
    }

    private static MapPathControlPoint CreateControlPoint(Transform parent, string name, Vector3 position)
    {
        var pointObject = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(pointObject, "Create Map Control Point");
        pointObject.transform.SetParent(parent);
        pointObject.transform.position = position;
        var controlPoint = Undo.AddComponent<MapPathControlPoint>(pointObject);
        controlPoint.Configure("control-point");
        return controlPoint;
    }

    private static void RemoveVisualComponents(GameObject pointObject)
    {
        foreach (var renderer in pointObject.GetComponents<Renderer>())
            Undo.DestroyObjectImmediate(renderer);
        foreach (var filter in pointObject.GetComponents<MeshFilter>())
            Undo.DestroyObjectImmediate(filter);
        foreach (var collider in pointObject.GetComponents<Collider>())
            Undo.DestroyObjectImmediate(collider);
    }

    private static string LabelFromCheckpointId(string id)
    {
        return id == "checkpoint-01" ? "Start" : "Level 1";
    }

    private static Canvas EnsureWorldMapCanvas()
    {
        var canvasObject = GameObject.Find("World Map Canvas");
        Canvas canvas;
        if (canvasObject == null)
        {
            canvasObject = new GameObject("World Map Canvas");
            Undo.RegisterCreatedObjectUndo(canvasObject, "Create World Map Canvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }
        else
        {
            canvas = canvasObject.GetComponent<Canvas>();
            if (canvas == null)
                canvas = Undo.AddComponent<Canvas>(canvasObject);

            if (canvasObject.GetComponent<GraphicRaycaster>() == null)
                Undo.AddComponent<GraphicRaycaster>(canvasObject);
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.enabled = true;
        canvas.sortingOrder = 110;

        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = Undo.AddComponent<CanvasScaler>(canvas.gameObject);

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static string BuildNavigatorSetupMessage(
        Canvas canvas,
        GameObject navigatorObject,
        MapPath cameraPath,
        MapPath playerPath,
        MapPathFollower cameraFollower,
        MapPathFollower playerFollower)
    {
        return "Created/repaired real scene UI objects:\n\n" +
            $"- {canvas.name}\n" +
            $"- {navigatorObject.name}\n\n" +
            "Checkpoint/control point objects were not modified.\n\n" +
            "Wiring:\n" +
            $"- Camera Path: {(cameraPath != null ? cameraPath.name : "missing")}\n" +
            $"- Player Path: {(playerPath != null ? playerPath.name : "missing")}\n" +
            $"- Camera Follower: {(cameraFollower != null ? cameraFollower.name : "missing; assign manually")}\n" +
            $"- Player Follower: {(playerFollower != null ? playerFollower.name : "missing; assign manually")}";
    }

    private static MapPathFollower EnsureWorldMapPlayerFollower(MapPath playerPath)
    {
        if (playerPath == null)
            return null;

        var playerObject = GameObject.Find("World Map Player");
        if (playerObject == null)
        {
            playerObject = new GameObject("World Map Player");
            Undo.RegisterCreatedObjectUndo(playerObject, "Create World Map Player");
        }

        var follower = playerObject.GetComponent<MapPathFollower>();
        if (follower == null)
            follower = Undo.AddComponent<MapPathFollower>(playerObject);

        Undo.RecordObject(follower, "Wire World Map Player Follower");
        var movementAnimation = EnsureWorldMapPlayerVisual(playerObject);
        follower.Configure(playerPath);
        follower.ConfigureLookTarget(null);
        follower.ConfigureMovementAnimation(movementAnimation);
        follower.ConfigureMovementStyle(true, Vector3.back);
        follower.JumpToCheckpoint(0);
        EditorUtility.SetDirty(follower);
        EditorUtility.SetDirty(playerObject);
        return follower;
    }

    private static PlayerMovement EnsureWorldMapPlayerVisual(GameObject playerObject)
    {
        var visualTransform = playerObject.transform.Find("Player Visual");
        GameObject visualObject;
        if (visualTransform != null)
        {
            visualObject = visualTransform.gameObject;
        }
        else
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WorldMapPlayerPrefabPath);
            if (prefab != null)
            {
                visualObject = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                Undo.RegisterCreatedObjectUndo(visualObject, "Create World Map Player Visual");
            }
            else
            {
                visualObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                Undo.RegisterCreatedObjectUndo(visualObject, "Create World Map Player Visual");
            }

            visualObject.name = "Player Visual";
            visualObject.transform.SetParent(playerObject.transform, false);
        }

        Undo.RecordObject(visualObject.transform, "Configure World Map Player Visual");
        visualObject.transform.localPosition = Vector3.zero;
        visualObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        visualObject.transform.localScale = Vector3.one * WorldMapPlayerVisualScale;
        visualObject.SetActive(true);

        DisableVisualPhysics(visualObject);

        var movementAnimation = visualObject.GetComponentInChildren<PlayerMovement>(true);
        if (movementAnimation != null)
        {
            Undo.RecordObject(movementAnimation, "Configure World Map Player Animation");
            movementAnimation.ConfigureForBoardMovement();
            EditorUtility.SetDirty(movementAnimation);
        }

        EditorUtility.SetDirty(visualObject);
        return movementAnimation;
    }

    private static Transform EnsureCameraLookTarget(Transform player)
    {
        var target = player.Find("Camera Look Target");
        GameObject targetObject;
        if (target == null)
        {
            targetObject = new GameObject("Camera Look Target");
            Undo.RegisterCreatedObjectUndo(targetObject, "Create Camera Look Target");
            targetObject.transform.SetParent(player, false);
        }
        else
        {
            targetObject = target.gameObject;
        }

        Undo.RecordObject(targetObject.transform, "Configure Camera Look Target");
        targetObject.transform.localPosition = Vector3.up * CameraLookAtHeightOffset;
        targetObject.transform.localRotation = Quaternion.identity;
        targetObject.transform.localScale = Vector3.one;
        targetObject.hideFlags = HideFlags.None;
        EditorUtility.SetDirty(targetObject.transform);
        return targetObject.transform;
    }

    private static void DisableVisualPhysics(GameObject root)
    {
        foreach (var collider in root.GetComponentsInChildren<Collider>(true))
        {
            Undo.RecordObject(collider, "Disable World Map Player Collider");
            collider.enabled = false;
            EditorUtility.SetDirty(collider);
        }

        foreach (var rigidbody in root.GetComponentsInChildren<Rigidbody>(true))
        {
            Undo.RecordObject(rigidbody, "Disable World Map Player Physics");
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;
            EditorUtility.SetDirty(rigidbody);
        }
    }

    private static MapPathFollower EnsureWorldMapCameraFollower(MapPath cameraPath, Transform lookTarget)
    {
        if (cameraPath == null)
            return null;

        var camera = FindWorldMapCamera();
        if (camera == null)
        {
            var cameraObject = new GameObject("World Map Camera");
            Undo.RegisterCreatedObjectUndo(cameraObject, "Create World Map Camera");
            camera = cameraObject.AddComponent<Camera>();
        }

        camera.gameObject.name = "World Map Camera";
        camera.gameObject.tag = "MainCamera";
        camera.enabled = true;
        ApplyMiniatureMapCameraSettings(camera);

        var follower = camera.GetComponent<MapPathFollower>();
        if (follower == null)
            follower = Undo.AddComponent<MapPathFollower>(camera.gameObject);

        Undo.RecordObject(follower, "Wire World Map Camera Follower");
        follower.Configure(cameraPath);
        follower.ConfigureLookTarget(lookTarget);
        follower.JumpToCheckpoint(0);
        EditorUtility.SetDirty(follower);

        DisableOtherSceneCameras(camera);
        EnsureSingleAudioListener(camera);
        EditorUtility.SetDirty(camera);
        EditorUtility.SetDirty(camera.gameObject);
        return follower;
    }

    private static void ApplyMiniatureMapCameraSettings(Camera camera)
    {
        Undo.RecordObject(camera, "Apply Miniature Map Camera Settings");
        camera.orthographic = false;
        camera.fieldOfView = MiniatureMapFieldOfView;
        camera.nearClipPlane = 0.02f;
        camera.farClipPlane = 120f;
        camera.depth = 100f;
        camera.rect = new Rect(0f, 0f, 1f, 1f);
        camera.targetTexture = null;
        camera.targetDisplay = 0;
        camera.cullingMask = ~0;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.46f, 0.68f, 0.84f);
    }

    private static void LookAt(Transform source, Vector3 targetPosition)
    {
        var direction = targetPosition - source.position;
        if (direction.sqrMagnitude <= 0.0001f)
            return;

        source.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private static Camera FindWorldMapCamera()
    {
        var named = GameObject.Find("World Map Camera");
        if (named != null && named.TryGetComponent<Camera>(out var namedCamera))
            return namedCamera;

        if (Camera.main != null)
            return Camera.main;

        var cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include);
        return cameras.Length > 0 ? cameras[0] : null;
    }

    private static void DisableOtherSceneCameras(Camera activeCamera)
    {
        foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
        {
            if (camera == activeCamera)
                continue;

            Undo.RecordObject(camera, "Disable Non-Map Camera");
            camera.enabled = false;
            if (camera.CompareTag("MainCamera"))
                camera.gameObject.tag = "Untagged";
            EditorUtility.SetDirty(camera);
        }
    }

    private static void EnsureSingleAudioListener(Camera activeCamera)
    {
        var activeListener = activeCamera.GetComponent<AudioListener>();
        if (activeListener == null)
            activeListener = Undo.AddComponent<AudioListener>(activeCamera.gameObject);

        activeListener.enabled = true;
        foreach (var listener in Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include))
        {
            if (listener == activeListener)
                continue;

            Undo.RecordObject(listener, "Disable Extra Audio Listener");
            listener.enabled = false;
            EditorUtility.SetDirty(listener);
        }
    }

    private static void EnsureEventSystem()
    {
        var eventSystem = Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include);
        if (eventSystem == null)
        {
            var eventSystemObject = new GameObject("EventSystem");
            Undo.RegisterCreatedObjectUndo(eventSystemObject, "Create EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
        }

        eventSystem.gameObject.SetActive(true);

        foreach (var legacyModule in eventSystem.GetComponents<StandaloneInputModule>())
            Undo.DestroyObjectImmediate(legacyModule);

        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            Undo.AddComponent<InputSystemUIInputModule>(eventSystem.gameObject);

        EditorUtility.SetDirty(eventSystem.gameObject);
    }

    private static GameObject FindOrCreateChild(Transform parent, string name)
    {
        var child = parent.Find(name);
        if (child != null)
            return child.gameObject;

        var childObject = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(childObject, "Create " + name);
        childObject.transform.SetParent(parent, false);
        return childObject;
    }

    private static RectTransform EnsureRectTransform(GameObject target)
    {
        var rect = target.GetComponent<RectTransform>();
        return rect != null ? rect : Undo.AddComponent<RectTransform>(target);
    }

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        var component = target.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(target);
    }

    private static Button EnsureIconButton(Transform parent, string name, string iconPath, Vector2 anchoredPosition)
    {
        var buttonObject = FindOrCreateChild(parent, name);
        var rect = EnsureRectTransform(buttonObject);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(56f, 56f);

        var image = EnsureComponent<Image>(buttonObject);
        image.sprite = LoadSprite(ButtonRoundSpritePath);
        image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = Color.white;

        var button = EnsureComponent<Button>(buttonObject);
        button.targetGraphic = image;

        var textTransform = buttonObject.transform.Find("Text");
        if (textTransform != null)
            textTransform.gameObject.SetActive(false);

        var iconObject = FindOrCreateChild(buttonObject.transform, "Icon");
        var iconRect = EnsureRectTransform(iconObject);
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(25f, 25f);

        var icon = EnsureComponent<Image>(iconObject);
        icon.sprite = LoadSprite(iconPath);
        icon.type = Image.Type.Simple;
        icon.preserveAspect = true;
        icon.color = new Color(0.13f, 0.11f, 0.09f, 1f);
        icon.raycastTarget = false;
        return button;
    }

    private static Text EnsureLabel(Transform parent, string name)
    {
        var labelObject = FindOrCreateChild(parent, name);
        var rect = EnsureRectTransform(labelObject);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(270f, 50f);

        var label = EnsureComponent<Text>(labelObject);
        label.text = "-";
        label.font = GetDefaultFont();
        label.fontStyle = FontStyle.Bold;
        label.fontSize = 20;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(1f, 0.91f, 0.68f, 1f);
        return label;
    }

    private static Image EnsureLevelTitleBackground(Transform canvasTransform)
    {
        var backgroundObject = FindOrCreateChild(canvasTransform, "Map Level Title Background");
        var rect = EnsureRectTransform(backgroundObject);
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -18f);
        rect.sizeDelta = new Vector2(300f, 58f);

        var image = EnsureComponent<Image>(backgroundObject);
        image.sprite = LoadSprite(PanelSoftPath);
        image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = new Color(0.20f, 0.16f, 0.12f, 0.88f);
        image.raycastTarget = false;

        var legacyText = backgroundObject.GetComponent<Text>();
        if (legacyText != null)
            Undo.DestroyObjectImmediate(legacyText);

        var oldTitle = canvasTransform.Find("Map Level Title");
        if (oldTitle != null && oldTitle != backgroundObject.transform)
            oldTitle.gameObject.SetActive(false);

        return image;
    }

    private static Text EnsureLevelTitle(Transform titleBackground)
    {
        var label = EnsureLabel(titleBackground, "Level Text");
        var rect = label.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.offsetMin = new Vector2(16f, 0f);
        rect.offsetMax = new Vector2(-16f, 0f);

        label.fontSize = 24;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(1f, 0.91f, 0.58f, 1f);
        label.raycastTarget = false;
        return label;
    }

    private static Button EnsureStartButton(Transform parent)
    {
        var buttonObject = FindOrCreateChild(parent, "Start Button");
        var rect = EnsureRectTransform(buttonObject);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(178f, 48f);

        var image = EnsureComponent<Image>(buttonObject);
        image.sprite = LoadSprite(PanelSoftPath);
        image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = new Color(1f, 0.67f, 0.06f, 1f);

        var button = EnsureComponent<Button>(buttonObject);
        button.targetGraphic = image;

        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.86f, 0.32f, 1f);
        colors.pressedColor = new Color(0.86f, 0.45f, 0.02f, 1f);
        colors.selectedColor = colors.normalColor;
        colors.disabledColor = new Color(0.48f, 0.42f, 0.34f, 0.55f);
        button.colors = colors;

        var textObject = FindOrCreateChild(buttonObject.transform, "Text");
        var textRect = EnsureRectTransform(textObject);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var text = EnsureComponent<Text>(textObject);
        text.text = "START";
        text.font = GetDefaultFont();
        text.fontStyle = FontStyle.Bold;
        text.fontSize = 22;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.18f, 0.12f, 0.06f, 1f);
        text.raycastTarget = false;

        buttonObject.SetActive(true);
        return button;
    }

    private static Button EnsurePlayButton(Transform parent)
    {
        var buttonObject = FindOrCreateChild(parent, "Play Level");
        var rect = EnsureRectTransform(buttonObject);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(180f, 52f);

        var image = EnsureComponent<Image>(buttonObject);
        image.sprite = LoadSprite(PanelSoftPath);
        image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = new Color(1f, 0.67f, 0.06f, 1f);

        var button = EnsureComponent<Button>(buttonObject);
        button.targetGraphic = image;

        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.88f, 0.34f, 1f);
        colors.pressedColor = new Color(0.9f, 0.52f, 0.03f, 1f);
        colors.selectedColor = colors.normalColor;
        colors.disabledColor = new Color(0.48f, 0.42f, 0.34f, 0.55f);
        button.colors = colors;

        var iconObject = buttonObject.transform.Find("Icon");
        if (iconObject != null)
            iconObject.gameObject.SetActive(false);

        var textObject = FindOrCreateChild(buttonObject.transform, "Text");
        var textRect = EnsureRectTransform(textObject);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var text = EnsureComponent<Text>(textObject);
        text.text = "PLAY";
        text.font = GetDefaultFont();
        text.fontStyle = FontStyle.Bold;
        text.fontSize = 20;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.18f, 0.12f, 0.06f, 1f);
        text.raycastTarget = false;
        return button;
    }

    private static Sprite LoadSprite(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Font GetDefaultFont()
    {
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
            ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private static MapPath FindMapPath(string pathName)
    {
        foreach (var path in Object.FindObjectsByType<MapPath>(FindObjectsInactive.Include))
        {
            if (path.name == pathName)
                return path;
        }

        return null;
    }

}
