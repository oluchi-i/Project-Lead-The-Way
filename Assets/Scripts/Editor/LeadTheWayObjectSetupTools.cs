using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class LeadTheWayObjectSetupTools
{
    private const string ObjectIconFolder = "Assets/Art/UI/ObjectIcons";
    private const string ArrowRightIconPath = "Assets/Art/UI/ButtonSet/Textures/icons/128x128/arrow_right.png";
    private const string ArrowLeftIconPath = "Assets/Art/UI/ButtonSet/Textures/icons/128x128/arrow_left.png";
    private const string ArrowUpIconPath = "Assets/Art/UI/ButtonSet/Textures/icons/128x128/arrow_up.png";
    private const string ArrowDownIconPath = "Assets/Art/UI/ButtonSet/Textures/icons/128x128/arrow_down.png";
    private const string BadgeSoftPath = "Assets/Art/UI/Sprites/BadgeSoft.asset";

    [MenuItem("Tools/Lead The Way/Optimize/Wire Current Scene References")]
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
        var startTile = FindBoardObject(boardObjects, item => IsNamed(item, "start") && item.ObjectType != BoardObjectType.Door);
        var startDoor = FindBoardObject(boardObjects, item => IsNamed(item, "start") && item.ObjectType == BoardObjectType.Door);
        var destinationDoor = FindBoardObject(boardObjects, item => IsNamed(item, "destination") && item.ObjectType == BoardObjectType.Door)
            ?? FindBoardObject(boardObjects, item => IsNamed(item, "exit") && item.ObjectType == BoardObjectType.Door)
            ?? FindBoardObject(boardObjects, item => item.ObjectType == BoardObjectType.Door && item != startDoor);

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
        changedCount += WireButtonPressAudioSources();

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

    public static void WireControlUIAudioSource()
    {
        var controlUI = Object.FindAnyObjectByType<SelectionPanelsUI>(FindObjectsInactive.Include);
        if (controlUI == null)
        {
            EditorUtility.DisplayDialog("Wire Control UI Audio Source", "No SelectionPanelsUI was found in the open scene.", "OK");
            return;
        }

        var audioSource = controlUI.GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = Undo.AddComponent<AudioSource>(controlUI.gameObject);

        var serializedUI = new SerializedObject(controlUI);
        serializedUI.FindProperty("audioSource").objectReferenceValue = audioSource;
        serializedUI.ApplyModifiedProperties();

        EditorUtility.SetDirty(controlUI);
        EditorUtility.SetDirty(audioSource);
        EditorSceneManager.MarkSceneDirty(controlUI.gameObject.scene);

        Selection.activeGameObject = controlUI.gameObject;
        EditorGUIUtility.PingObject(controlUI.gameObject);
        EditorUtility.DisplayDialog("Wire Control UI Audio Source", $"Wired {audioSource.name}'s AudioSource to SelectionPanelsUI.", "OK");
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

    private static InteractionFlowManager GetOrCreateInteractionFlowManager()
    {
        var flowManager = Object.FindAnyObjectByType<InteractionFlowManager>();
        if (flowManager != null)
            return flowManager;

        return Undo.AddComponent<InteractionFlowManager>(GetOrCreateGameSystems());
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
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{ObjectIconFolder}/{safeName}.png");
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
            var audioSource = controlUI.GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = Undo.AddComponent<AudioSource>(controlUI.gameObject);

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;

            var serializedUI = new SerializedObject(controlUI);
            SetObject(serializedUI, "audioSource", audioSource);
            SetObject(serializedUI, "interactionFlowManager", flowManager);
            SetObjectIfPresent(serializedUI, "objectPreviousPageButton", FindChildComponent<Button>(controlUI.transform, "Previous Object Page"));
            SetObjectIfPresent(serializedUI, "objectNextPageButton", FindChildComponent<Button>(controlUI.transform, "Next Object Page"));
            SetObjectIfPresent(serializedUI, "objectPageText", FindChildComponent<Text>(controlUI.transform, "Object Page Text"));
            SetObjectIfPresent(serializedUI, "navigationButtonSprite", navigationSprite);
            serializedUI.ApplyModifiedProperties();

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
            var serializedCounter = new SerializedObject(counter);
            SetObject(serializedCounter, "interactionFlowManager", flowManager);
            SetObjectIfPresent(serializedCounter, "countText", FindChildComponent<Text>(counter.transform, "Count") ?? counter.GetComponentInChildren<Text>(true));
            SetObjectIfPresent(serializedCounter, "remainingFillImage", FindFilledImage(counter.transform));
            serializedCounter.ApplyModifiedProperties();

            EditorUtility.SetDirty(counter);
            changedCount++;
        }

        return changedCount;
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
