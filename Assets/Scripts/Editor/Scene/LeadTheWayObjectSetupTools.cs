using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class LeadTheWayObjectSetupTools
{
    private static readonly string[] ObjectIconSearchFolders =
    {
        "Assets/Art/Objects",
        "Assets/Art/Board/Doors/Icons"
    };
    private const string ArrowRightIconPath = "Assets/Art/UI/ButtonSet/Textures/icons/128x128/arrow_right.png";
    private const string ArrowLeftIconPath = "Assets/Art/UI/ButtonSet/Textures/icons/128x128/arrow_left.png";
    private const string ArrowUpIconPath = "Assets/Art/UI/ButtonSet/Textures/icons/128x128/arrow_up.png";
    private const string ArrowDownIconPath = "Assets/Art/UI/ButtonSet/Textures/icons/128x128/arrow_down.png";
    private const string BadgeSoftPath = "Assets/Art/UI/Sprites/BadgeSoft.asset";
    private const string PanelSoftPath = "Assets/Art/UI/Sprites/PanelSoft.asset";
    private const string TileSoftPath = "Assets/Art/UI/Sprites/TileSoft.asset";
    private const string PauseIconPath = "Assets/Art/UI/ButtonSet/Textures/icons/128x128/pause.png";
    private const string PlayIconPath = "Assets/Art/UI/ButtonSet/Textures/icons/128x128/play.png";
    private const string RepeatIconPath = "Assets/Art/UI/ButtonSet/Textures/icons/128x128/repeat.png";
    private const string CloseIconPath = "Assets/Art/UI/ButtonSet/Textures/icons/128x128/x.png";
    private const string PoppinsBoldPath = "Assets/Art/UI/Fonts/Poppins-Bold.ttf";
    private const string LevelSuccessSoundPath = "Assets/Sound/SoundEffects/level_success.mp3";
    private const string PlayerDeathSoundPath = "Assets/Sound/SoundEffects/death.mp3";
    private const string TeleportEffectPath = "Assets/Lana Studio/Hyper Casual FX/Prefabs/Area/Area_fire_red.prefab";

    [MenuItem("Tools/Lead The Way/Scene/Wire Current Scene References")]
    public static void WireCurrentSceneReferences()
    {
        var boardManager = Object.FindAnyObjectByType<BoardManager>();
        var flowManager = Object.FindAnyObjectByType<InteractionFlowManager>();
        var playerMover = Object.FindAnyObjectByType<BoardPlayerMover>();
        var resultFlash = Object.FindAnyObjectByType<LevelResultFlashUI>(FindObjectsInactive.Include);
        var boardObjects = BoardManager.FindSceneBoardObjects();
        var changedCount = 0;

        changedCount += EnsureUniqueBoardObjectIds(boardObjects);

        var playerObject = playerMover != null
            ? playerMover.GetComponent<BoardObject>()
            : FindBoardObject(boardObjects, item => item.ObjectType == BoardObjectType.Player);
        var playerDeathAnimation = playerMover != null
            ? playerMover.GetComponentInChildren<PlayerMovement>(true)
            : playerObject != null
                ? playerObject.GetComponentInChildren<PlayerMovement>(true)
                : null;
        var startTile = IsUsableBoardObject(flowManager != null ? flowManager.StartTile : null)
            ? flowManager.StartTile
            : FindBoardObject(boardObjects, item => IsNamed(item, "start") && item.ObjectType != BoardObjectType.Door);
        var startDoor = IsUsableDoor(flowManager != null ? flowManager.StartDoor : null)
            ? flowManager.StartDoor
            : FindBoardObject(boardObjects, item => IsNamed(item, "start") && item.ObjectType == BoardObjectType.Door);
        var destinationDoor = IsUsableDoor(flowManager != null ? flowManager.DestinationDoor : null)
            ? flowManager.DestinationDoor
            : null;

        if (destinationDoor == null)
        {
            destinationDoor = FindBoardObject(boardObjects, item => IsNamed(item, "destination") && item.ObjectType == BoardObjectType.Door && item != startDoor)
                ?? FindBoardObject(boardObjects, item => IsNamed(item, "exit") && item.ObjectType == BoardObjectType.Door && item != startDoor);
        }

        if (startDoor == null && startTile != null)
            startDoor = FindDoorAtOrNearTile(boardObjects, startTile.TilePosition, destinationDoor);

        if (destinationDoor == null)
            destinationDoor = FindBoardObject(boardObjects, item => item.ObjectType == BoardObjectType.Door && item != startDoor);

        var introCameraTransition = Object.FindAnyObjectByType<IntroCameraTransition>(FindObjectsInactive.Include);
        var deathCinematicCamera = GetOrCreateDeathCinematicCamera(boardManager);

        if (playerMover != null)
        {
            if (playerObject == null)
                playerObject = GetOrAddComponent<BoardObject>(playerMover.gameObject);

            Undo.RecordObject(playerMover, "Wire Player Mover");
            playerMover.Configure(boardManager, playerObject);
            EditorUtility.SetDirty(playerMover);
            changedCount++;
            changedCount += RepairPlayerForBoardGameplay(playerMover, boardManager, false);
        }

        if (flowManager != null)
        {
            Undo.RecordObject(flowManager, "Wire Interaction Flow");
            flowManager.Configure(boardManager, playerMover, playerObject, destinationDoor);
            flowManager.ConfigureLevelFlow(startTile, startDoor, destinationDoor);
            flowManager.ConfigureResultFlash(resultFlash);
            flowManager.ConfigurePlayerDeathAnimation(playerDeathAnimation);
            flowManager.ConfigureIntroCameraTransition(introCameraTransition);
            flowManager.ConfigureDeathCinematicCamera(deathCinematicCamera);
            flowManager.ConfigureTeleportEffect(AssetDatabase.LoadAssetAtPath<GameObject>(TeleportEffectPath));
            EditorUtility.SetDirty(flowManager);
            changedCount++;
        }

        foreach (var mover in Object.FindObjectsByType<GridTileMover>(FindObjectsInactive.Exclude))
        {
            var boardObject = mover.GetComponent<BoardObject>();
            Undo.RecordObject(mover, "Wire Grid Tile Mover");
            mover.Configure(boardManager, boardObject, flowManager, true);
            EditorUtility.SetDirty(mover);
            changedCount++;
        }

        changedCount += WireControlUIs(flowManager);
        changedCount += WireInteractionCounters(flowManager);
        changedCount += WireResultFlashAudio(resultFlash);
        changedCount += WireButtonPressAudioSources();
        if (introCameraTransition != null)
            changedCount++;
        if (deathCinematicCamera != null)
            changedCount++;

        if (boardManager != null)
        {
            boardManager.RebuildRegistry();
            EditorUtility.SetDirty(boardManager);
        }

        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (activeScene.IsValid())
            EditorSceneManager.MarkSceneDirty(activeScene);

        EditorUtility.DisplayDialog(
            "Wire Current Scene References",
            $"Finished wiring scene references.\n\nUpdated {changedCount} component(s)/asset reference(s).\n\nIf Console shows missing-reference warnings after this, that object likely needs a deliberate scene/prefab setup decision.",
            "OK");
    }

    [MenuItem("Tools/Lead The Way/Scene/Repair Player For Board Gameplay")]
    public static void RepairPlayerForBoardGameplay()
    {
        var boardManager = Object.FindAnyObjectByType<BoardManager>();
        var playerMover = Object.FindAnyObjectByType<BoardPlayerMover>(FindObjectsInactive.Include);
        if (playerMover == null)
        {
            EditorUtility.DisplayDialog("Repair Player For Board Gameplay", "Could not find a BoardPlayerMover in the current scene.", "OK");
            return;
        }

        var changedCount = RepairPlayerForBoardGameplay(playerMover, boardManager, true);
        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (activeScene.IsValid())
            EditorSceneManager.MarkSceneDirty(activeScene);

        EditorUtility.DisplayDialog(
            "Repair Player For Board Gameplay",
            $"Repaired player board wiring and visual physics safety.\n\nUpdated {changedCount} component(s).",
            "OK");
    }

    [MenuItem("Tools/Lead The Way/Scene/Setup Intro Camera Poses")]
    public static void SetupIntroCameraPoses()
    {
        var boardManager = Object.FindAnyObjectByType<BoardManager>();
        var flowManager = Object.FindAnyObjectByType<InteractionFlowManager>();
        var boardObjects = BoardManager.FindSceneBoardObjects(true);
        var startTile = IsUsableBoardObject(flowManager != null ? flowManager.StartTile : null)
            ? flowManager.StartTile
            : FindBoardObject(boardObjects, item => IsNamed(item, "start") && item.ObjectType != BoardObjectType.Door);
        var startDoor = IsUsableDoor(flowManager != null ? flowManager.StartDoor : null)
            ? flowManager.StartDoor
            : FindBoardObject(boardObjects, item => IsNamed(item, "start") && item.ObjectType == BoardObjectType.Door);

        var introCameraTransition = GetOrCreateIntroCameraTransition(boardManager, startTile, startDoor, true);
        if (flowManager != null)
        {
            Undo.RecordObject(flowManager, "Wire Intro Camera Transition");
            flowManager.ConfigureIntroCameraTransition(introCameraTransition);
            EditorUtility.SetDirty(flowManager);
        }

        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (activeScene.IsValid())
            EditorSceneManager.MarkSceneDirty(activeScene);

        EditorUtility.DisplayDialog(
            "Setup Intro Camera Poses",
            introCameraTransition != null
                ? "Created or updated intro camera poses.\n\nGameplay Camera Pose captured the current camera transform.\nMove Start Camera Pose in the Scene view to tune the opening shot."
                : "Could not setup intro camera poses because the scene has no Camera.",
            "OK");
    }

    [MenuItem("Tools/Lead The Way/Scene/Setup Ending Camera Pose")]
    public static void SetupEndingCameraPose()
    {
        var camera = Camera.main;
        if (camera == null)
            camera = Object.FindAnyObjectByType<Camera>(FindObjectsInactive.Include);

        if (camera == null)
        {
            EditorUtility.DisplayDialog("Setup Ending Camera Pose", "Could not find a scene Camera to capture.", "OK");
            return;
        }

        var posesRoot = FindOrCreateChild(GetOrCreateGameSystems().transform, "Intro Camera Poses");
        var endingPose = FindOrCreateChild(posesRoot, "Ending Camera Pose");
        var capturedPose = IsDefaultTransform(endingPose);
        if (capturedPose)
            CopyCameraPose(camera, endingPose);

        var introCameraTransition = Object.FindAnyObjectByType<IntroCameraTransition>(FindObjectsInactive.Include);
        if (introCameraTransition == null)
            introCameraTransition = Undo.AddComponent<IntroCameraTransition>(camera.gameObject);

        var startPose = introCameraTransition.StartCameraPose != null
            ? introCameraTransition.StartCameraPose
            : posesRoot.Find("Start Camera Pose");
        var gameplayPose = introCameraTransition.GameplayCameraPose != null
            ? introCameraTransition.GameplayCameraPose
            : posesRoot.Find("Gameplay Camera Pose");

        Undo.RecordObject(introCameraTransition, "Wire Ending Camera Pose");
        introCameraTransition.Configure(camera, startPose, gameplayPose);
        introCameraTransition.ConfigureEndingPose(endingPose, camera.fieldOfView);

        var flowManager = Object.FindAnyObjectByType<InteractionFlowManager>(FindObjectsInactive.Include);
        if (flowManager != null)
        {
            Undo.RecordObject(flowManager, "Wire Ending Camera Pose");
            flowManager.ConfigureIntroCameraTransition(introCameraTransition);
            EditorUtility.SetDirty(flowManager);
        }

        EditorUtility.SetDirty(endingPose);
        EditorUtility.SetDirty(introCameraTransition);
        EditorUtility.SetDirty(camera.gameObject);

        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (activeScene.IsValid())
            EditorSceneManager.MarkSceneDirty(activeScene);

        EditorUtility.DisplayDialog(
            "Setup Ending Camera Pose",
            capturedPose
                ? "Captured the current scene Camera as Ending Camera Pose.\n\nStart Camera Pose and Gameplay Camera Pose were not moved."
                : "Wired the existing Ending Camera Pose without moving it.\n\nStart Camera Pose and Gameplay Camera Pose were not moved.",
            "OK");
    }

    [MenuItem("Tools/Lead The Way/UI/Setup Pause Menu")]
    public static void SetupPauseMenu()
    {
        var canvas = EnsureGameplayCanvas();
        EnsureEventSystem();
        var pauseUI = FindOrCreateRectChild(canvas.transform, "Pause UI");
        StretchToParent(pauseUI);

        var pauseButton = EnsurePauseButton(pauseUI);
        var overlay = EnsurePauseOverlay(pauseUI);
        var panel = EnsurePausePanel(overlay);
        var closeButton = EnsurePanelIconButton(panel, "Close Button", CloseIconPath, new Vector2(-24f, -24f), TextAnchor.UpperRight);
        var title = EnsureText(panel, "Title", "PAUSED", 32, new Color(1f, 0.86f, 0.28f, 1f), TextAnchor.MiddleCenter);
        ConfigureRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(260f, 44f));

        var subtitle = EnsureText(panel, "Subtitle", "TAKE A BREATH", 13, new Color(1f, 0.94f, 0.78f, 0.82f), TextAnchor.MiddleCenter);
        ConfigureRect(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(260f, 26f));

        var resumeButton = EnsurePanelTextButton(panel, "Resume Button", "RESUME", PlayIconPath, new Vector2(0f, -145f));
        var restartButton = EnsurePanelTextButton(panel, "Restart Button", "RESTART", RepeatIconPath, new Vector2(0f, -210f));

        var canvasGroup = overlay.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = Undo.AddComponent<CanvasGroup>(overlay.gameObject);

        var pauseMenu = pauseUI.GetComponent<PauseMenuUI>();
        if (pauseMenu == null)
            pauseMenu = Undo.AddComponent<PauseMenuUI>(pauseUI.gameObject);

        Undo.RecordObject(pauseMenu, "Wire Pause Menu");
        pauseMenu.Configure(pauseButton, resumeButton, restartButton, closeButton, overlay.gameObject, canvasGroup);

        overlay.gameObject.SetActive(false);
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        EditorUtility.SetDirty(pauseMenu);
        EditorUtility.SetDirty(canvasGroup);
        EditorUtility.SetDirty(canvas);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);

        EditorUtility.DisplayDialog("Setup Pause Menu", "Created/repaired the top-right pause button and pause panel under the scene Canvas.", "OK");
    }

    [MenuItem("Tools/Lead The Way/Board/Setup Selected Movable Objects")]
    public static void SetupSelectedMovableObjects()
    {
        var targets = GetSelectedSceneObjects();
        if (targets.Count == 0)
        {
            EditorUtility.DisplayDialog("Setup Movable Objects", "Select one or more scene objects in the Hierarchy first.", "OK");
            return;
        }

        var boardManager = Object.FindAnyObjectByType<BoardManager>();
        if (boardManager == null)
        {
            EditorUtility.DisplayDialog("Setup Movable Objects", "Add a BoardManager to the scene first.", "OK");
            return;
        }

        var flowManager = GetOrCreateInteractionFlowManager();

        foreach (var target in targets)
        {
            var boardObject = GetOrAddComponent<BoardObject>(target);
            var mover = GetOrAddComponent<GridTileMover>(target);
            var selectable = GetOrAddComponent<SelectableControlObject>(target);

            Undo.RecordObject(boardObject, "Setup Movable Object");
            Undo.RecordObject(mover, "Setup Movable Object");
            Undo.RecordObject(selectable, "Setup Movable Object");

            boardObject.Configure(BoardObjectType.MovableObject, true, true, true);
            boardObject.SyncTileFromTransform(boardManager.WorldOrigin, boardManager.TileSize);
            mover.Configure(boardManager, boardObject, flowManager, true);

            var displayName = ObjectNames.NicifyVariableName(target.name);
            var icon = selectable.Icon != null ? selectable.Icon : LoadObjectIcon(target.name);
            selectable.Configure(displayName, icon, CreateMovableObjectActions(mover));

            EditorUtility.SetDirty(boardObject);
            EditorUtility.SetDirty(mover);
            EditorUtility.SetDirty(selectable);
        }

        boardManager.RebuildRegistry();
        EditorSceneManager.MarkSceneDirty(targets[0].scene);
        EditorUtility.DisplayDialog("Setup Movable Objects", $"Configured {targets.Count} movable object(s).", "OK");
    }

    [MenuItem("Tools/Lead The Way/Board/Setup Selected Blocking Objects")]
    public static void SetupSelectedBlockingObjects()
    {
        var targets = GetSelectedSceneObjects();
        if (targets.Count == 0)
        {
            EditorUtility.DisplayDialog("Setup Blocking Objects", "Select one or more scene objects in the Hierarchy first.", "OK");
            return;
        }

        var boardManager = Object.FindAnyObjectByType<BoardManager>();
        if (boardManager == null)
        {
            EditorUtility.DisplayDialog("Setup Blocking Objects", "Add a BoardManager to the scene first.", "OK");
            return;
        }

        foreach (var target in targets)
        {
            var boardObject = GetOrAddComponent<BoardObject>(target);

            Undo.RecordObject(boardObject, "Setup Blocking Object");
            RemoveComponentIfPresent<GridTileMover>(target, "Remove Movable Object Mover");
            RemoveComponentIfPresent<SelectableControlObject>(target, "Remove Selectable Control Object");

            boardObject.Configure(BoardObjectType.Obstacle, true, true, false);
            boardObject.SyncTileFromTransform(boardManager.WorldOrigin, boardManager.TileSize);

            EditorUtility.SetDirty(boardObject);
        }

        boardManager.RebuildRegistry();
        EditorUtility.SetDirty(boardManager);
        EditorSceneManager.MarkSceneDirty(targets[0].scene);
        EditorUtility.DisplayDialog("Setup Blocking Objects", $"Configured {targets.Count} blocking object(s).", "OK");
    }

    private static List<GameObject> GetSelectedSceneObjects()
    {
        var targets = new List<GameObject>();
        foreach (var selected in Selection.gameObjects)
        {
            if (selected == null || EditorUtility.IsPersistent(selected))
                continue;

            if (!targets.Contains(selected))
                targets.Add(selected);
        }

        return targets;
    }

    private static T GetOrAddComponent<T>(GameObject target) where T : Component
    {
        if (target.TryGetComponent<T>(out var existing))
            return existing;

        return Undo.AddComponent<T>(target);
    }

    private static int RepairPlayerForBoardGameplay(BoardPlayerMover playerMover, BoardManager boardManager, bool configureMover)
    {
        if (playerMover == null)
            return 0;

        var changedCount = 0;
        var boardObject = GetOrAddComponent<BoardObject>(playerMover.gameObject);
        Undo.RecordObject(boardObject, "Repair Player Board Object");
        boardObject.Configure(BoardObjectType.Player, true, true, true);
        EditorUtility.SetDirty(boardObject);
        changedCount++;

        if (configureMover)
        {
            Undo.RecordObject(playerMover, "Repair Player Mover");
            playerMover.Configure(boardManager, boardObject);
            EditorUtility.SetDirty(playerMover);
            changedCount++;
        }

        var movement = playerMover.GetComponentInChildren<PlayerMovement>(true);
        if (movement != null)
        {
            var serializedMovement = new SerializedObject(movement);
            var usePhysicsDeath = serializedMovement.FindProperty("usePhysicsDeath");
            if (usePhysicsDeath != null)
                usePhysicsDeath.boolValue = true;

            var explosionForce = serializedMovement.FindProperty("explosionForce");
            if (explosionForce != null && (explosionForce.floatValue <= 0f || explosionForce.floatValue > 2.2f))
                explosionForce.floatValue = 1.15f;

            var upwardForce = serializedMovement.FindProperty("upwardForce");
            if (upwardForce != null && (upwardForce.floatValue < 0f || upwardForce.floatValue > 0.35f))
                upwardForce.floatValue = 0.08f;

            serializedMovement.ApplyModifiedProperties();
            EditorUtility.SetDirty(movement);
            changedCount++;
        }

        foreach (var body in playerMover.GetComponentsInChildren<Rigidbody>(true))
        {
            Undo.RecordObject(body, "Repair Player Physics");
            body.isKinematic = true;
            body.useGravity = false;
            EditorUtility.SetDirty(body);
            changedCount++;
        }

        foreach (var meshCollider in playerMover.GetComponentsInChildren<MeshCollider>(true))
        {
            Undo.RecordObject(meshCollider, "Repair Player Mesh Colliders");
            meshCollider.convex = true;
            meshCollider.enabled = false;
            EditorUtility.SetDirty(meshCollider);
            changedCount++;
        }

        return changedCount;
    }

    private static void RemoveComponentIfPresent<T>(GameObject target, string undoName) where T : Component
    {
        if (!target.TryGetComponent<T>(out var component))
            return;

        Undo.DestroyObjectImmediate(component);
    }

    private static InteractionFlowManager GetOrCreateInteractionFlowManager()
    {
        var flowManager = Object.FindAnyObjectByType<InteractionFlowManager>();
        if (flowManager != null)
            return flowManager;

        return Undo.AddComponent<InteractionFlowManager>(GetOrCreateGameSystems());
    }

    private static DeathCinematicCamera GetOrCreateDeathCinematicCamera(BoardManager boardManager)
    {
        var existing = Object.FindAnyObjectByType<DeathCinematicCamera>(FindObjectsInactive.Include);
        if (existing != null)
        {
            Undo.RecordObject(existing, "Wire Death Cinematic Camera");
            existing.Configure(existing.GetComponent<Camera>() != null ? existing.GetComponent<Camera>() : Camera.main, boardManager);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        var camera = Camera.main;
        if (camera == null)
            camera = Object.FindAnyObjectByType<Camera>(FindObjectsInactive.Include);

        if (camera == null)
            return null;

        var cinematicCamera = Undo.AddComponent<DeathCinematicCamera>(camera.gameObject);
        cinematicCamera.Configure(camera, boardManager);
        EditorUtility.SetDirty(camera.gameObject);
        EditorUtility.SetDirty(cinematicCamera);
        return cinematicCamera;
    }

    private static IntroCameraTransition GetOrCreateIntroCameraTransition(BoardManager boardManager, BoardObject startTile, BoardObject startDoor, bool captureGameplayPoseFromCamera)
    {
        var camera = Camera.main;
        if (camera == null)
            camera = Object.FindAnyObjectByType<Camera>(FindObjectsInactive.Include);

        if (camera == null)
            return null;

        var introCameraTransition = Object.FindAnyObjectByType<IntroCameraTransition>(FindObjectsInactive.Include);
        if (introCameraTransition == null)
            introCameraTransition = Undo.AddComponent<IntroCameraTransition>(camera.gameObject);

        var root = FindOrCreateChild(GetOrCreateGameSystems().transform, "Intro Camera Poses");
        var startPose = FindOrCreateChild(root, "Start Camera Pose");
        var gameplayPose = FindOrCreateChild(root, "Gameplay Camera Pose");

        if (captureGameplayPoseFromCamera || IsDefaultTransform(gameplayPose))
            CopyCameraPose(camera, gameplayPose);

        if (IsDefaultTransform(startPose))
            ConfigureStartCameraPose(camera, startPose, boardManager, startTile, startDoor);

        introCameraTransition.Configure(camera, startPose, gameplayPose);
        introCameraTransition.ConfigureFieldOfView(camera.fieldOfView, camera.fieldOfView);

        EditorUtility.SetDirty(startPose);
        EditorUtility.SetDirty(gameplayPose);
        EditorUtility.SetDirty(introCameraTransition);
        EditorUtility.SetDirty(camera.gameObject);
        return introCameraTransition;
    }

    private static Transform FindOrCreateChild(Transform parent, string childName)
    {
        var existing = parent != null ? parent.Find(childName) : null;
        if (existing != null)
            return existing;

        var childObject = new GameObject(childName);
        Undo.RegisterCreatedObjectUndo(childObject, $"Create {childName}");
        if (parent != null)
            childObject.transform.SetParent(parent, false);

        return childObject.transform;
    }

    private static void CopyCameraPose(Camera camera, Transform pose)
    {
        Undo.RecordObject(pose, "Capture Camera Pose");
        pose.position = camera.transform.position;
        pose.rotation = camera.transform.rotation;
        pose.localScale = Vector3.one;
    }

    private static void ConfigureStartCameraPose(Camera camera, Transform pose, BoardManager boardManager, BoardObject startTile, BoardObject startDoor)
    {
        Undo.RecordObject(pose, "Configure Start Camera Pose");

        if (boardManager == null || startDoor == null)
        {
            pose.position = camera.transform.position;
            pose.rotation = camera.transform.rotation;
            pose.localScale = Vector3.one;
            return;
        }

        var doorTarget = startDoor.transform.position + Vector3.up * 1.2f;
        var viewDirection = Vector3.back;
        if (startTile != null)
        {
            var startTileWorld = boardManager.TileToWorld(startTile.TilePosition, startDoor.transform.position.y);
            viewDirection = startTileWorld - startDoor.transform.position;
            viewDirection.y = 0f;
        }

        if (viewDirection.sqrMagnitude < 0.0001f)
            viewDirection = -startDoor.transform.forward;

        viewDirection.Normalize();
        pose.position = doorTarget + viewDirection * 4f + Vector3.up * 1.8f;
        pose.rotation = Quaternion.LookRotation(doorTarget - pose.position, Vector3.up);
        pose.localScale = Vector3.one;
    }

    private static bool IsDefaultTransform(Transform transform)
    {
        return transform != null
            && transform.localPosition == Vector3.zero
            && transform.localRotation == Quaternion.identity
            && transform.localScale == Vector3.one;
    }

    private static GameObject GetOrCreateGameSystems()
    {
        var systems = GameObject.Find("Game Systems");
        if (systems != null)
            return systems;

        systems = new GameObject("Game Systems");
        Undo.RegisterCreatedObjectUndo(systems, "Create Game Systems");
        return systems;
    }

    private static Canvas EnsureGameplayCanvas()
    {
        var canvasObject = GameObject.Find("Canvas");
        Canvas canvas;
        if (canvasObject == null)
        {
            canvasObject = new GameObject("Canvas");
            Undo.RegisterCreatedObjectUndo(canvasObject, "Create Canvas");
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
        canvas.sortingOrder = 100;

        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = Undo.AddComponent<CanvasScaler>(canvas.gameObject);

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
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

    private static RectTransform FindOrCreateRectChild(Transform parent, string childName)
    {
        var existing = parent != null ? parent.Find(childName) : null;
        if (existing != null)
        {
            if (existing.TryGetComponent<RectTransform>(out var existingRect))
                return existingRect;

            return Undo.AddComponent<RectTransform>(existing.gameObject);
        }

        var childObject = new GameObject(childName, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(childObject, $"Create {childName}");
        childObject.transform.SetParent(parent, false);
        return childObject.GetComponent<RectTransform>();
    }

    private static void StretchToParent(RectTransform rectTransform)
    {
        ConfigureRect(rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static Button EnsurePauseButton(RectTransform parent)
    {
        var buttonRect = FindOrCreateRectChild(parent, "Pause Button");
        ConfigureRect(buttonRect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(52f, 52f));

        var image = EnsureImage(buttonRect.gameObject);
        image.sprite = LoadSprite(TileSoftPath);
        image.type = Image.Type.Sliced;
        image.color = new Color(1f, 0.72f, 0.04f, 1f);

        var button = EnsureButton(buttonRect.gameObject, image);
        EnsureBorder(buttonRect.gameObject, new Color(0.18f, 0.12f, 0.04f, 0.72f), new Vector2(1.2f, -1.2f));
        EnsureShadow(buttonRect.gameObject, new Color(0f, 0f, 0f, 0.38f), new Vector2(0f, -3f));
        EnsureFeedback(buttonRect.gameObject);

        var icon = FindOrCreateRectChild(buttonRect, "Icon");
        ConfigureRect(icon, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24f, 24f));
        var iconImage = EnsureImage(icon.gameObject);
        iconImage.sprite = LoadSprite(PauseIconPath);
        iconImage.color = new Color(0.13f, 0.09f, 0.04f, 1f);
        iconImage.raycastTarget = false;

        return button;
    }

    private static RectTransform EnsurePauseOverlay(RectTransform parent)
    {
        var overlay = FindOrCreateRectChild(parent, "Pause Overlay");
        StretchToParent(overlay);
        var image = EnsureImage(overlay.gameObject);
        image.sprite = null;
        image.color = new Color(0.04f, 0.035f, 0.025f, 0.68f);

        var canvasGroup = overlay.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = Undo.AddComponent<CanvasGroup>(overlay.gameObject);

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        return overlay;
    }

    private static RectTransform EnsurePausePanel(RectTransform overlay)
    {
        var panel = FindOrCreateRectChild(overlay, "Pause Panel");
        ConfigureRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(390f, 310f));
        var image = EnsureImage(panel.gameObject);
        image.sprite = LoadSprite(PanelSoftPath);
        image.type = Image.Type.Sliced;
        image.color = new Color(0.18f, 0.23f, 0.17f, 0.96f);

        var accent = panel.Find("Accent");
        if (accent != null)
            accent.gameObject.SetActive(false);

        return panel;
    }

    private static Button EnsurePanelIconButton(RectTransform parent, string name, string iconPath, Vector2 anchoredPosition, TextAnchor anchor)
    {
        var rect = FindOrCreateRectChild(parent, name);
        var anchorVector = anchor == TextAnchor.UpperRight ? new Vector2(1f, 1f) : new Vector2(0.5f, 0.5f);
        ConfigureRect(rect, anchorVector, anchorVector, anchorVector, anchoredPosition, new Vector2(34f, 34f));

        var image = EnsureImage(rect.gameObject);
        image.sprite = LoadSprite(TileSoftPath);
        image.type = Image.Type.Sliced;
        image.color = new Color(1f, 0.72f, 0.04f, 1f);

        var button = EnsureButton(rect.gameObject, image);
        EnsureBorder(rect.gameObject, new Color(0.18f, 0.12f, 0.04f, 0.72f), new Vector2(1f, -1f));
        EnsureShadow(rect.gameObject, new Color(0f, 0f, 0f, 0.34f), new Vector2(0f, -2.5f));
        EnsureFeedback(rect.gameObject);

        var icon = FindOrCreateRectChild(rect, "Icon");
        ConfigureRect(icon, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(17f, 17f));
        var iconImage = EnsureImage(icon.gameObject);
        iconImage.sprite = LoadSprite(iconPath);
        iconImage.color = new Color(0.13f, 0.09f, 0.04f, 1f);
        iconImage.raycastTarget = false;
        return button;
    }

    private static Button EnsurePanelTextButton(RectTransform parent, string name, string text, string iconPath, Vector2 anchoredPosition)
    {
        var rect = FindOrCreateRectChild(parent, name);
        ConfigureRect(rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), anchoredPosition, new Vector2(230f, 48f));

        var image = EnsureImage(rect.gameObject);
        image.sprite = LoadSprite(TileSoftPath);
        image.type = Image.Type.Sliced;
        image.color = new Color(1f, 0.72f, 0.04f, 1f);

        var button = EnsureButton(rect.gameObject, image);
        EnsureBorder(rect.gameObject, new Color(0.18f, 0.12f, 0.04f, 0.6f), new Vector2(1f, -1f));
        EnsureShadow(rect.gameObject, new Color(0f, 0f, 0f, 0.3f), new Vector2(0f, -3f));
        EnsureFeedback(rect.gameObject);

        var icon = FindOrCreateRectChild(rect, "Icon");
        ConfigureRect(icon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(42f, 0f), new Vector2(20f, 20f));
        var iconImage = EnsureImage(icon.gameObject);
        iconImage.sprite = LoadSprite(iconPath);
        iconImage.color = new Color(0.13f, 0.09f, 0.04f, 1f);
        iconImage.raycastTarget = false;

        var label = EnsureText(rect, "Label", text, 18, new Color(0.13f, 0.09f, 0.04f, 1f), TextAnchor.MiddleCenter);
        ConfigureRect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(14f, 0f), Vector2.zero);
        return button;
    }

    private static Text EnsureText(RectTransform parent, string name, string value, int fontSize, Color color, TextAnchor alignment)
    {
        var rect = FindOrCreateRectChild(parent, name);
        var text = rect.GetComponent<Text>();
        if (text == null)
            text = Undo.AddComponent<Text>(rect.gameObject);

        text.text = value;
        text.font = LoadFont();
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }

    private static void ConfigureRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        Undo.RecordObject(rect, "Configure UI Rect");
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static Image EnsureImage(GameObject target)
    {
        var image = target.GetComponent<Image>();
        if (image == null)
            image = Undo.AddComponent<Image>(target);

        return image;
    }

    private static Button EnsureButton(GameObject target, Graphic targetGraphic)
    {
        var button = target.GetComponent<Button>();
        if (button == null)
            button = Undo.AddComponent<Button>(target);

        button.targetGraphic = targetGraphic;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = new ColorBlock
        {
            normalColor = Color.white,
            highlightedColor = new Color(1f, 0.96f, 0.72f, 1f),
            pressedColor = new Color(0.95f, 0.53f, 0.02f, 1f),
            selectedColor = Color.white,
            disabledColor = new Color(1f, 1f, 1f, 0.36f),
            colorMultiplier = 1f,
            fadeDuration = 0.08f
        };
        return button;
    }

    private static void EnsureFeedback(GameObject target)
    {
        if (target.GetComponent<UIButtonFeedback>() == null)
            Undo.AddComponent<UIButtonFeedback>(target);
    }

    private static void EnsureBorder(GameObject target, Color color, Vector2 distance)
    {
        var outline = target.GetComponent<Outline>();
        if (outline == null)
            outline = Undo.AddComponent<Outline>(target);

        outline.effectColor = color;
        outline.effectDistance = distance;
        outline.useGraphicAlpha = true;
    }

    private static void EnsureShadow(GameObject target, Color color, Vector2 distance)
    {
        var shadows = target.GetComponents<Shadow>();
        Shadow shadow = null;
        foreach (var item in shadows)
        {
            if (item is not Outline)
            {
                shadow = item;
                break;
            }
        }

        if (shadow == null)
            shadow = Undo.AddComponent<Shadow>(target);

        shadow.effectColor = color;
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
    }

    private static Sprite LoadSprite(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Font LoadFont()
    {
        var font = AssetDatabase.LoadAssetAtPath<Font>(PoppinsBoldPath);
        return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private static List<ControlAction> CreateMovableObjectActions(GridTileMover mover)
    {
        return new List<ControlAction>
        {
            CreateAction("X +1", ArrowRightIconPath, mover.MovePositiveX),
            CreateAction("X -1", ArrowLeftIconPath, mover.MoveNegativeX),
            CreateAction("Z -1", ArrowUpIconPath, mover.MoveNegativeZ),
            CreateAction("Z +1", ArrowDownIconPath, mover.MovePositiveZ)
        };
    }

    private static ControlAction CreateAction(string label, string iconPath, UnityAction callback)
    {
        var action = new ControlAction
        {
            label = label,
            icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath)
        };

        UnityEventTools.AddPersistentListener(action.onSelected, callback);
        return action;
    }

    private static Sprite LoadObjectIcon(string objectName)
    {
        var safeName = string.IsNullOrWhiteSpace(objectName) ? "ObjectIcon" : objectName.Trim();
        foreach (var invalidCharacter in System.IO.Path.GetInvalidFileNameChars())
            safeName = safeName.Replace(invalidCharacter, '_');

        safeName = safeName.Replace(' ', '_');
        var guids = AssetDatabase.FindAssets($"{safeName} t:Sprite", ObjectIconSearchFolders);
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) != safeName)
                continue;

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
                return sprite;
        }

        return null;
    }

    private static int EnsureUniqueBoardObjectIds(List<BoardObject> boardObjects)
    {
        var changedCount = 0;
        var usedIds = new HashSet<string>();
        foreach (var boardObject in boardObjects)
        {
            if (boardObject == null)
                continue;

            Undo.RecordObject(boardObject, "Ensure Unique Board Object Id");
            if (boardObject.EnsureUniqueObjectId(usedIds))
            {
                EditorUtility.SetDirty(boardObject);
                changedCount++;
            }
        }

        return changedCount;
    }

    private static int WireControlUIs(InteractionFlowManager flowManager)
    {
        var changedCount = 0;
        var navigationSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BadgeSoftPath);
        foreach (var controlUI in Object.FindObjectsByType<SelectionPanelsUI>(FindObjectsInactive.Include))
        {
            var canvasGroup = controlUI.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = Undo.AddComponent<CanvasGroup>(controlUI.gameObject);

            var audioSource = controlUI.GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = Undo.AddComponent<AudioSource>(controlUI.gameObject);

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;

            var serializedUI = new SerializedObject(controlUI);
            SetObject(serializedUI, "canvasGroup", canvasGroup);
            SetObject(serializedUI, "audioSource", audioSource);
            SetObject(serializedUI, "interactionFlowManager", flowManager);
            SetObjectIfPresent(serializedUI, "objectPreviousPageButton", FindChildComponent<Button>(controlUI.transform, "Previous Object Page"));
            SetObjectIfPresent(serializedUI, "objectNextPageButton", FindChildComponent<Button>(controlUI.transform, "Next Object Page"));
            SetObjectIfPresent(serializedUI, "objectPageText", FindChildComponent<Text>(controlUI.transform, "Object Page Text"));
            SetObjectIfPresent(serializedUI, "navigationButtonSprite", navigationSprite);
            serializedUI.ApplyModifiedProperties();

            EditorUtility.SetDirty(canvasGroup);
            EditorUtility.SetDirty(audioSource);
            EditorUtility.SetDirty(controlUI);
            changedCount++;
        }

        return changedCount;
    }

    private static int WireInteractionCounters(InteractionFlowManager flowManager)
    {
        var changedCount = 0;
        foreach (var counter in Object.FindObjectsByType<InteractionCounterUI>(FindObjectsInactive.Include))
        {
            var canvasGroup = counter.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = Undo.AddComponent<CanvasGroup>(counter.gameObject);

            var serializedCounter = new SerializedObject(counter);
            SetObject(serializedCounter, "interactionFlowManager", flowManager);
            SetObject(serializedCounter, "canvasGroup", canvasGroup);
            SetObjectIfPresent(serializedCounter, "countText", FindChildComponent<Text>(counter.transform, "Count") ?? counter.GetComponentInChildren<Text>(true));
            SetObjectIfPresent(serializedCounter, "remainingFillImage", FindFilledImage(counter.transform));
            serializedCounter.ApplyModifiedProperties();

            EditorUtility.SetDirty(canvasGroup);
            EditorUtility.SetDirty(counter);
            changedCount++;
        }

        return changedCount;
    }

    private static int WireResultFlashAudio(LevelResultFlashUI resultFlash)
    {
        if (resultFlash == null)
            return 0;

        var audioSource = resultFlash.GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = Undo.AddComponent<AudioSource>(resultFlash.gameObject);

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        var serializedFlash = new SerializedObject(resultFlash);
        SetObject(serializedFlash, "audioSource", audioSource);
        SetObject(serializedFlash, "successSound", AssetDatabase.LoadAssetAtPath<AudioClip>(LevelSuccessSoundPath));
        SetObject(serializedFlash, "failureSound", AssetDatabase.LoadAssetAtPath<AudioClip>(PlayerDeathSoundPath));
        serializedFlash.ApplyModifiedProperties();

        EditorUtility.SetDirty(audioSource);
        EditorUtility.SetDirty(resultFlash);
        return 1;
    }

    private static int WireButtonPressAudioSources()
    {
        var changedCount = 0;
        foreach (var buttonPress in Object.FindObjectsByType<ButtonPress>(FindObjectsInactive.Include))
        {
            var audioSource = buttonPress.GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = Undo.AddComponent<AudioSource>(buttonPress.gameObject);

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.2f;

            var serializedButton = new SerializedObject(buttonPress);
            SetObject(serializedButton, "audioSource", audioSource);
            serializedButton.ApplyModifiedProperties();

            EditorUtility.SetDirty(audioSource);
            EditorUtility.SetDirty(buttonPress);
            changedCount++;
        }

        return changedCount;
    }

    private static BoardObject FindBoardObject(List<BoardObject> boardObjects, System.Predicate<BoardObject> predicate)
    {
        foreach (var boardObject in boardObjects)
        {
            if (boardObject != null && boardObject.gameObject.activeInHierarchy && predicate(boardObject))
                return boardObject;
        }

        return null;
    }

    private static BoardObject FindDoorAtOrNearTile(List<BoardObject> boardObjects, Vector2Int tile, BoardObject excludedDoor)
    {
        BoardObject adjacentDoor = null;

        foreach (var boardObject in boardObjects)
        {
            if (!IsUsableDoor(boardObject) || boardObject == excludedDoor)
                continue;

            var distance = GetNearestManhattanDistance(boardObject, tile);
            if (distance == 0)
                return boardObject;

            if (distance == 1 && adjacentDoor == null)
                adjacentDoor = boardObject;
        }

        return adjacentDoor;
    }

    private static int GetNearestManhattanDistance(BoardObject boardObject, Vector2Int tile)
    {
        var nearestDistance = int.MaxValue;
        foreach (var occupiedTile in boardObject.GetOccupiedTiles())
        {
            var distance = Mathf.Abs(occupiedTile.x - tile.x) + Mathf.Abs(occupiedTile.y - tile.y);
            if (distance < nearestDistance)
                nearestDistance = distance;
        }

        return nearestDistance;
    }

    private static bool IsUsableBoardObject(BoardObject boardObject)
    {
        return boardObject != null && boardObject.gameObject.activeInHierarchy;
    }

    private static bool IsUsableDoor(BoardObject boardObject)
    {
        return IsUsableBoardObject(boardObject) && boardObject.ObjectType == BoardObjectType.Door;
    }

    private static bool IsNamed(Object target, string namePart)
    {
        return target != null && target.name.IndexOf(namePart, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static void SetObject(SerializedObject serializedObject, string propertyName, Object value)
    {
        var property = serializedObject.FindProperty(propertyName);
        if (property != null)
            property.objectReferenceValue = value;
    }

    private static void SetObjectIfPresent(SerializedObject serializedObject, string propertyName, Object value)
    {
        if (value != null)
            SetObject(serializedObject, propertyName, value);
    }

    private static T FindChildComponent<T>(Transform root, string childName) where T : Component
    {
        var child = FindChild(root, childName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private static Transform FindChild(Transform root, string childName)
    {
        if (root == null)
            return null;

        if (root.name == childName)
            return root;

        foreach (Transform child in root)
        {
            var match = FindChild(child, childName);
            if (match != null)
                return match;
        }

        return null;
    }

    private static Image FindFilledImage(Transform root)
    {
        if (root == null)
            return null;

        foreach (var image in root.GetComponentsInChildren<Image>(true))
        {
            if (image.type == Image.Type.Filled)
                return image;
        }

        return null;
    }
}
