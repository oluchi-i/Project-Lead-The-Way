using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class LeadTheWayObjectSetupTools
{
    private const string PanelSoftPath = "Assets/Prefabs/UI/Sprites/PanelSoft.asset";
    private const string BadgeSoftPath = "Assets/Prefabs/UI/Sprites/BadgeSoft.asset";
    private const string PoppinsBoldPath = "Assets/Art/UI/Fonts/Poppins-Bold.ttf";
    private const string ButtonRoundPath = "Assets/Art/UI/ButtonSet/Textures/buttons/button_round_130.png";
    private const string SmallPanelPath = "Assets/Art/UI/ButtonSet/Textures/controls/universal_panel_20.png";

    [MenuItem("Tools/Lead The Way/Board/Create Board Manager")]
    public static void CreateBoardManager()
    {
        var existing = UnityEngine.Object.FindAnyObjectByType<BoardManager>();
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            EditorGUIUtility.PingObject(existing);
            EditorUtility.DisplayDialog("Create Board Manager", "A BoardManager already exists in the scene.", "OK");
            return;
        }

        var systems = GameObject.Find("Game Systems");
        if (systems == null)
        {
            systems = new GameObject("Game Systems");
            Undo.RegisterCreatedObjectUndo(systems, "Create Game Systems");
        }

        var boardManager = Undo.AddComponent<BoardManager>(systems);
        EditorSceneManager.MarkSceneDirty(systems.scene);
        Selection.activeGameObject = systems;
        EditorGUIUtility.PingObject(boardManager);
        EditorUtility.DisplayDialog("Create Board Manager", "Created BoardManager on Game Systems.", "OK");
    }

    [MenuItem("Tools/Lead The Way/Board/Wire Interaction Flow References")]
    public static void WireInteractionFlowReferences()
    {
        WireLevelFlowReferences();
    }

    [MenuItem("Tools/Lead The Way/Board/Wire Level Flow References")]
    public static void WireLevelFlowReferences()
    {
        var boardManager = UnityEngine.Object.FindAnyObjectByType<BoardManager>();
        if (boardManager == null)
        {
            EditorUtility.DisplayDialog("Wire Level Flow", "Create a BoardManager first: Tools > Lead The Way > Board > Create Board Manager.", "OK");
            return;
        }

        var flowManager = UnityEngine.Object.FindAnyObjectByType<InteractionFlowManager>();
        if (flowManager == null)
            flowManager = Undo.AddComponent<InteractionFlowManager>(GetOrCreateGameSystems());

        var playerMover = UnityEngine.Object.FindAnyObjectByType<BoardPlayerMover>();
        var playerObject = playerMover != null ? playerMover.GetComponent<BoardObject>() : FindFirstSceneBoardObjectOfType(BoardObjectType.Player);
        var startTile = flowManager.StartTile != null ? flowManager.StartTile : FindNamedSceneBoardObject("start", item => item.ObjectType != BoardObjectType.Door);
        var startDoor = flowManager.StartDoor != null ? flowManager.StartDoor : FindNamedSceneBoardObject("start", item => item.ObjectType == BoardObjectType.Door);
        var destinationDoor = flowManager.DestinationDoor != null ? flowManager.DestinationDoor : FindDestinationDoorCandidate(startDoor);

        Undo.RecordObject(flowManager, "Wire Level Flow");
        flowManager.Configure(boardManager, playerMover, playerObject, destinationDoor);
        flowManager.ConfigureLevelFlow(startTile, startDoor, destinationDoor);
        EditorUtility.SetDirty(flowManager);
        EditorSceneManager.MarkSceneDirty(flowManager.gameObject.scene);

        Selection.activeGameObject = flowManager.gameObject;
        EditorGUIUtility.PingObject(flowManager);

        var missing = new List<string>();
        if (playerMover == null)
            missing.Add("Player Mover");
        if (playerObject == null)
            missing.Add("Player Object");
        if (startTile == null)
            missing.Add("Start Tile");
        if (startDoor == null)
            missing.Add("Start Door");
        if (destinationDoor == null)
            missing.Add("Destination Door");

        if (missing.Count > 0)
        {
            EditorUtility.DisplayDialog(
                "Wire Level Flow",
                $"Wired what could be found, but still missing: {string.Join(", ", missing)}.\n\nUse the setup commands for the missing scene objects, then run this again.",
                "OK");
            return;
        }

        EditorUtility.DisplayDialog("Wire Level Flow", "Wired BoardManager, Player, StartTile, StartDoor, and DestinationDoor.", "OK");
    }

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

    [MenuItem("Tools/Lead The Way/Board/Setup Selected Board Objects")]
    public static void SetupSelectedBoardObjectsAuto()
    {
        SetupSelectedBoardObjects(null);
    }

    [MenuItem("Tools/Lead The Way/Board/Setup Selected Player Progression")]
    public static void SetupSelectedPlayerProgression()
    {
        var targets = GetSelectedSceneObjects();
        if (targets.Count == 0)
        {
            EditorUtility.DisplayDialog("Setup Player Progression", "Select the Player GameObject in the Hierarchy first.", "OK");
            return;
        }

        var boardManager = UnityEngine.Object.FindAnyObjectByType<BoardManager>();
        if (boardManager == null)
        {
            EditorUtility.DisplayDialog("Setup Player Progression", "Create a BoardManager first: Tools > Lead The Way > Board > Create Board Manager.", "OK");
            return;
        }

        if (UnityEngine.Object.FindAnyObjectByType<InteractionFlowManager>() == null)
            Undo.AddComponent<InteractionFlowManager>(GetOrCreateGameSystems());

        var flowManager = UnityEngine.Object.FindAnyObjectByType<InteractionFlowManager>();

        foreach (var target in targets)
        {
            var boardObject = GetOrAddComponent<BoardObject>(target);
            var playerMover = GetOrAddComponent<BoardPlayerMover>(target);
            var playerMovement = target.GetComponent<PlayerMovement>();
            var body = target.GetComponent<Rigidbody>();

            Undo.RecordObject(boardObject, "Setup Player Progression");
            Undo.RecordObject(playerMover, "Setup Player Progression");

            boardObject.Configure(BoardObjectType.Player, true, true, true);
            boardObject.SyncTileFromTransform(boardManager.WorldOrigin, boardManager.TileSize);
            playerMover.Configure(boardManager, boardObject);

            if (playerMovement != null)
            {
                Undo.RecordObject(playerMovement, "Setup Player Progression");
                playerMovement.ConfigureForBoardMovement();
                playerMovement.enabled = true;
                EditorUtility.SetDirty(playerMovement);
            }

            if (body != null)
            {
                Undo.RecordObject(body, "Setup Player Progression");
                body.isKinematic = true;
                body.useGravity = false;
                EditorUtility.SetDirty(body);
            }

            EditorUtility.SetDirty(boardObject);
            EditorUtility.SetDirty(playerMover);

            if (flowManager != null)
            {
                Undo.RecordObject(flowManager, "Setup Player Progression");
                var destinationDoor = flowManager.DestinationDoor != null ? flowManager.DestinationDoor : FindDestinationDoorCandidate(flowManager.StartDoor);
                flowManager.Configure(boardManager, playerMover, boardObject, destinationDoor);
                flowManager.ConfigureLevelFlow(flowManager.StartTile, flowManager.StartDoor, destinationDoor);
                EditorUtility.SetDirty(flowManager);
            }
        }

        boardManager.RebuildRegistry();
        EditorSceneManager.MarkSceneDirty(targets[0].scene);
        EditorUtility.DisplayDialog("Setup Player Progression", $"Configured {targets.Count} player object(s) for interaction-driven board movement.", "OK");
    }

    [MenuItem("Tools/Lead The Way/Board/Setup Selected Destination Tile")]
    public static void SetupSelectedDestinationTile()
    {
        SetupSelectedBoardObjects(BoardObjectType.Goal);
    }

    [MenuItem("Tools/Lead The Way/Board/Setup Selected Start Tile")]
    public static void SetupSelectedStartTile()
    {
        var targets = GetSelectedSceneObjects();
        if (targets.Count == 0)
        {
            EditorUtility.DisplayDialog("Setup Start Tile", "Select the StartTile GameObject in the Hierarchy first.", "OK");
            return;
        }

        var boardManager = UnityEngine.Object.FindAnyObjectByType<BoardManager>();
        var origin = boardManager != null ? boardManager.WorldOrigin : Vector3.zero;
        var tileSize = boardManager != null ? boardManager.TileSize : 1f;
        var flowManager = GetOrCreateInteractionFlowManager();
        BoardObject firstStartTile = null;

        foreach (var target in targets)
        {
            var boardObject = GetOrAddComponent<BoardObject>(target);
            Undo.RecordObject(boardObject, "Setup Start Tile");
            boardObject.Configure(BoardObjectType.Other, true, false, false);
            boardObject.SyncTileFromTransform(origin, tileSize);
            EditorUtility.SetDirty(boardObject);

            if (firstStartTile == null)
                firstStartTile = boardObject;
        }

        if (flowManager != null && firstStartTile != null)
        {
            Undo.RecordObject(flowManager, "Wire Start Tile");
            flowManager.ConfigureLevelFlow(firstStartTile, flowManager.StartDoor, flowManager.DestinationDoor);
            EditorUtility.SetDirty(flowManager);
        }

        if (boardManager != null)
            boardManager.RebuildRegistry();

        EditorSceneManager.MarkSceneDirty(targets[0].scene);
        EditorUtility.DisplayDialog("Setup Start Tile", $"Configured {targets.Count} start tile object(s).", "OK");
    }

    [MenuItem("Tools/Lead The Way/Board/Set Selected Door As Start Door")]
    public static void SetSelectedDoorAsStartDoor()
    {
        var door = SetupSelectedDoorReference(true);
        if (door != null)
            EditorUtility.DisplayDialog("Set Start Door", $"Set {door.name} as the level start door. It will not appear in the control UI.", "OK");
    }

    [MenuItem("Tools/Lead The Way/Board/Set Selected Door As Destination Door")]
    public static void SetSelectedDoorAsDestinationDoor()
    {
        var door = SetupSelectedDoorReference(false);
        if (door != null)
            EditorUtility.DisplayDialog("Set Destination Door", $"Set {door.name} as the level destination door.", "OK");
    }

    [MenuItem("Tools/Lead The Way/Board/Sync Selected Tiles From Transforms")]
    public static void SyncSelectedBoardTilesFromTransforms()
    {
        var selectedBoardObjects = GetSelectedBoardObjects();
        if (selectedBoardObjects.Count == 0)
        {
            EditorUtility.DisplayDialog("Sync Board Tiles", "Select one or more GameObjects with BoardObject components first.", "OK");
            return;
        }

        var boardManager = UnityEngine.Object.FindAnyObjectByType<BoardManager>();
        var origin = boardManager != null ? boardManager.WorldOrigin : Vector3.zero;
        var tileSize = boardManager != null ? boardManager.TileSize : 1f;

        foreach (var boardObject in selectedBoardObjects)
        {
            Undo.RecordObject(boardObject, "Sync Board Tile");
            boardObject.SyncTileFromTransform(origin, tileSize);
            EditorUtility.SetDirty(boardObject);
        }

        if (boardManager != null)
            boardManager.RebuildRegistry();

        EditorSceneManager.MarkSceneDirty(selectedBoardObjects[0].gameObject.scene);
        EditorUtility.DisplayDialog("Sync Board Tiles", $"Synced {selectedBoardObjects.Count} BoardObject tile position(s) from their transforms.", "OK");
    }

    [MenuItem("Tools/Lead The Way/Board/Report Registered Board Objects")]
    public static void ReportRegisteredBoardObjects()
    {
        var boardManager = UnityEngine.Object.FindAnyObjectByType<BoardManager>();
        if (boardManager == null)
        {
            EditorUtility.DisplayDialog("Board Report", "No BoardManager exists in the active scene yet.", "OK");
            return;
        }

        boardManager.RebuildRegistry();

        var report = new List<string>();
        foreach (var boardObject in BoardManager.FindSceneBoardObjects())
        {
            if (EditorUtility.IsPersistent(boardObject))
                continue;

            report.Add($"{boardObject.name}: {boardObject.ObjectType}, anchor {boardObject.TilePosition}, footprint [{string.Join(", ", boardObject.GetOccupiedTiles())}], active={boardObject.gameObject.activeInHierarchy}, scene={boardObject.gameObject.scene.name}, blocks={boardObject.BlocksMovement}, movable={boardObject.Movable}");
        }

        report.Sort(StringComparer.Ordinal);
        Debug.Log($"Lead The Way Board Report: Registered {report.Count} BoardObject(s).");
        foreach (var line in report)
            Debug.Log("Lead The Way Board Object: " + line);

        EditorUtility.DisplayDialog("Board Report", $"Registered {report.Count} BoardObject(s). Check the Console for details.", "OK");
    }

    private static void SetupSelectedBoardObjects(BoardObjectType? forcedType)
    {
        var targets = GetSelectedSceneObjects();
        if (targets.Count == 0)
        {
            EditorUtility.DisplayDialog("Setup Board Objects", "Select one or more scene objects in the Hierarchy first.", "OK");
            return;
        }

        var boardManager = UnityEngine.Object.FindAnyObjectByType<BoardManager>();
        var origin = boardManager != null ? boardManager.WorldOrigin : Vector3.zero;
        var tileSize = boardManager != null ? boardManager.TileSize : 1f;

        foreach (var target in targets)
        {
            var boardObject = GetOrAddComponent<BoardObject>(target);
            var type = forcedType ?? GuessBoardObjectType(target.name);
            var settings = GetBoardDefaults(type);

            Undo.RecordObject(boardObject, "Setup Board Object");
            boardObject.Configure(type, settings.occupiesTile, settings.blocksMovement, settings.movable);
            boardObject.SyncTileFromTransform(origin, tileSize);
            EditorUtility.SetDirty(boardObject);
        }

        if (boardManager != null)
            boardManager.RebuildRegistry();

        EditorSceneManager.MarkSceneDirty(targets[0].scene);
        EditorUtility.DisplayDialog("Setup Board Objects", $"Configured {targets.Count} BoardObject(s).", "OK");
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

    private static List<BoardObject> GetSelectedBoardObjects()
    {
        var selectedBoardObjects = new List<BoardObject>();
        foreach (var selected in Selection.gameObjects)
        {
            if (selected == null || EditorUtility.IsPersistent(selected))
                continue;

            if (selected.TryGetComponent<BoardObject>(out var boardObject) && !selectedBoardObjects.Contains(boardObject))
                selectedBoardObjects.Add(boardObject);
        }

        return selectedBoardObjects;
    }

    private static T GetOrAddComponent<T>(GameObject target) where T : Component
    {
        if (target.TryGetComponent<T>(out var existing))
            return existing;

        return Undo.AddComponent<T>(target);
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

    private static InteractionFlowManager GetOrCreateInteractionFlowManager()
    {
        var flowManager = UnityEngine.Object.FindAnyObjectByType<InteractionFlowManager>();
        if (flowManager != null)
            return flowManager;

        return Undo.AddComponent<InteractionFlowManager>(GetOrCreateGameSystems());
    }

    private static BoardObject SetupSelectedDoorReference(bool isStartDoor)
    {
        var targets = GetSelectedSceneObjects();
        if (targets.Count == 0)
        {
            EditorUtility.DisplayDialog(
                isStartDoor ? "Set Start Door" : "Set Destination Door",
                "Select a door GameObject in the Hierarchy first.",
                "OK");
            return null;
        }

        var target = targets[0];
        var boardManager = UnityEngine.Object.FindAnyObjectByType<BoardManager>();
        var origin = boardManager != null ? boardManager.WorldOrigin : Vector3.zero;
        var tileSize = boardManager != null ? boardManager.TileSize : 1f;
        var flowManager = GetOrCreateInteractionFlowManager();
        var boardObject = GetOrAddComponent<BoardObject>(target);

        Undo.RecordObject(boardObject, isStartDoor ? "Setup Start Door" : "Setup Destination Door");
        boardObject.Configure(BoardObjectType.Door, true, true, false);
        boardObject.SyncTileFromTransform(origin, tileSize);
        EditorUtility.SetDirty(boardObject);

        if (isStartDoor)
        {
            RemoveComponentIfExists<ControlObjectConnector>(target);
            RemoveComponentIfExists<SelectableControlObject>(target);
        }

        if (flowManager != null)
        {
            Undo.RecordObject(flowManager, isStartDoor ? "Wire Start Door" : "Wire Destination Door");
            var startTile = flowManager.StartTile != null ? flowManager.StartTile : FindNamedSceneBoardObject("start", item => item.ObjectType != BoardObjectType.Door);
            var startDoor = isStartDoor ? boardObject : flowManager.StartDoor;
            var destinationDoor = isStartDoor ? flowManager.DestinationDoor : boardObject;

            flowManager.ConfigureLevelFlow(startTile, startDoor, destinationDoor);
            flowManager.Configure(boardManager, UnityEngine.Object.FindAnyObjectByType<BoardPlayerMover>(), FindFirstSceneBoardObjectOfType(BoardObjectType.Player), destinationDoor);
            EditorUtility.SetDirty(flowManager);
        }

        if (boardManager != null)
            boardManager.RebuildRegistry();

        EditorSceneManager.MarkSceneDirty(target.scene);
        Selection.activeGameObject = target;
        EditorGUIUtility.PingObject(target);
        return boardObject;
    }

    private static void RemoveComponentIfExists<T>(GameObject target) where T : Component
    {
        if (target == null || !target.TryGetComponent<T>(out var component))
            return;

        Undo.DestroyObjectImmediate(component);
    }

    private static BoardObject FindFirstSceneBoardObjectOfType(BoardObjectType type)
    {
        foreach (var boardObject in BoardManager.FindSceneBoardObjects())
        {
            if (boardObject == null || EditorUtility.IsPersistent(boardObject) || !boardObject.gameObject.activeInHierarchy)
                continue;

            if (boardObject.ObjectType == type)
                return boardObject;
        }

        return null;
    }

    private static BoardObject FindNamedSceneBoardObject(string namePart, Predicate<BoardObject> predicate)
    {
        foreach (var boardObject in BoardManager.FindSceneBoardObjects())
        {
            if (boardObject == null || EditorUtility.IsPersistent(boardObject) || !boardObject.gameObject.activeInHierarchy)
                continue;

            if (!predicate(boardObject))
                continue;

            if (boardObject.name.IndexOf(namePart, StringComparison.OrdinalIgnoreCase) >= 0)
                return boardObject;
        }

        return null;
    }

    private static BoardObject FindDestinationDoorCandidate(BoardObject startDoor)
    {
        var namedDoor = FindNamedSceneBoardObject("destination", item => item.ObjectType == BoardObjectType.Door)
            ?? FindNamedSceneBoardObject("exit", item => item.ObjectType == BoardObjectType.Door);

        if (namedDoor != null)
            return namedDoor;

        foreach (var boardObject in BoardManager.FindSceneBoardObjects())
        {
            if (boardObject == null || EditorUtility.IsPersistent(boardObject) || !boardObject.gameObject.activeInHierarchy)
                continue;

            if (boardObject.ObjectType == BoardObjectType.Door && boardObject != startDoor)
                return boardObject;
        }

        return null;
    }

    private static GameObject SetupStableControlUI(Canvas canvas)
    {
        var selectionUI = UnityEngine.Object.FindAnyObjectByType<SelectionPanelsUI>(FindObjectsInactive.Include);
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

        ConfigureRect(counterRoot.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(168f, 54f));

        var background = counterRoot.GetComponent<Image>();
        background.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PanelSoftPath);
        background.type = background.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        background.color = new Color(0.16f, 0.11f, 0.05f, 0.88f);

        var labelText = FindOrCreateText(counterRoot.transform, "Label");
        ConfigureCounterText(labelText, "ACTION COUNT", 10, FontStyle.Bold, TextAnchor.UpperLeft, new Vector2(12f, -7f), new Vector2(144f, 16f), new Color(1f, 0.82f, 0.24f, 1f));

        var countText = FindOrCreateText(counterRoot.transform, "Count");
        ConfigureCounterText(countText, $"0/{flowManager.MaxInteractionCount}", 26, FontStyle.Bold, TextAnchor.LowerLeft, new Vector2(12f, -18f), new Vector2(144f, 30f), new Color(1f, 0.93f, 0.72f, 1f));

        Undo.RecordObject(counterUI, "Setup Interaction Counter");
        counterUI.Configure(flowManager, countText);
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

    private static BoardObjectType GuessBoardObjectType(string objectName)
    {
        var normalizedName = objectName.ToLowerInvariant();
        if (normalizedName.Contains("player"))
            return BoardObjectType.Player;
        if (normalizedName.Contains("box") || normalizedName.Contains("crate"))
            return BoardObjectType.Box;
        if (normalizedName.Contains("door"))
            return BoardObjectType.Door;
        if (normalizedName.Contains("spike") || normalizedName.Contains("hazard") || normalizedName.Contains("hazzard"))
            return BoardObjectType.Spike;
        if (normalizedName.Contains("button"))
            return BoardObjectType.Button;
        if (normalizedName.Contains("lever"))
            return BoardObjectType.Lever;
        if (normalizedName.Contains("goal") || normalizedName.Contains("exit") || normalizedName.Contains("destination"))
            return BoardObjectType.Goal;
        if (normalizedName.Contains("wall"))
            return BoardObjectType.Wall;

        return BoardObjectType.Other;
    }

    private static (bool occupiesTile, bool blocksMovement, bool movable) GetBoardDefaults(BoardObjectType type)
    {
        switch (type)
        {
            case BoardObjectType.Player:
                return (true, true, true);
            case BoardObjectType.Box:
                return (true, true, true);
            case BoardObjectType.Door:
                return (true, true, false);
            case BoardObjectType.Spike:
            case BoardObjectType.Button:
            case BoardObjectType.Lever:
            case BoardObjectType.Goal:
                return (true, false, false);
            case BoardObjectType.Wall:
                return (true, true, false);
            default:
                return (true, false, false);
        }
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
}
