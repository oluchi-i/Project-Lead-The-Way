using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
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
    private const string LevelSuccessSoundPath = "Assets/Sound/SoundEffects/level_success.mp3";
    private const string PlayerDeathSoundPath = "Assets/Sound/SoundEffects/death.mp3";

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

        var introCameraTransition = GetOrCreateIntroCameraTransition(boardManager, startTile, startDoor, false);
        var deathCinematicCamera = GetOrCreateDeathCinematicCamera();

        if (playerMover != null)
        {
            Undo.RecordObject(playerMover, "Wire Player Mover");
            playerMover.Configure(boardManager, playerObject);
            EditorUtility.SetDirty(playerMover);
            changedCount++;
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

    private static DeathCinematicCamera GetOrCreateDeathCinematicCamera()
    {
        var existing = Object.FindAnyObjectByType<DeathCinematicCamera>(FindObjectsInactive.Include);
        if (existing != null)
            return existing;

        var camera = Camera.main;
        if (camera == null)
            camera = Object.FindAnyObjectByType<Camera>(FindObjectsInactive.Include);

        if (camera == null)
            return null;

        var cinematicCamera = Undo.AddComponent<DeathCinematicCamera>(camera.gameObject);
        cinematicCamera.Configure(camera);
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
