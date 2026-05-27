using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class LeadTheWayObjectSetupTools
{
    private const string ControlUIPrefabPath = "Assets/Prefabs/UI/ControlUI.prefab";
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

    [MenuItem("Tools/Lead The Way/Board/Create Interaction Flow Manager")]
    public static void CreateInteractionFlowManager()
    {
        var existing = UnityEngine.Object.FindAnyObjectByType<InteractionFlowManager>();
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            EditorGUIUtility.PingObject(existing);
            EditorUtility.DisplayDialog("Create Interaction Flow Manager", "An InteractionFlowManager already exists in the scene.", "OK");
            return;
        }

        var systems = GetOrCreateGameSystems();
        var flowManager = Undo.AddComponent<InteractionFlowManager>(systems);
        EditorSceneManager.MarkSceneDirty(systems.scene);
        Selection.activeGameObject = systems;
        EditorGUIUtility.PingObject(flowManager);
        EditorUtility.DisplayDialog("Create Interaction Flow Manager", "Created InteractionFlowManager on Game Systems.", "OK");
    }

    [MenuItem("Tools/Lead The Way/Board/Wire Interaction Flow References")]
    public static void WireInteractionFlowReferences()
    {
        var boardManager = UnityEngine.Object.FindAnyObjectByType<BoardManager>();
        if (boardManager == null)
        {
            EditorUtility.DisplayDialog("Wire Interaction Flow", "Create a BoardManager first: Tools > Lead The Way > Board > Create Board Manager.", "OK");
            return;
        }

        var flowManager = UnityEngine.Object.FindAnyObjectByType<InteractionFlowManager>();
        if (flowManager == null)
            flowManager = Undo.AddComponent<InteractionFlowManager>(GetOrCreateGameSystems());

        var playerMover = UnityEngine.Object.FindAnyObjectByType<BoardPlayerMover>();
        var playerObject = playerMover != null ? playerMover.GetComponent<BoardObject>() : FindFirstSceneBoardObjectOfType(BoardObjectType.Player);
        var targetDoor = FindFirstSceneBoardObjectOfType(BoardObjectType.Door);

        Undo.RecordObject(flowManager, "Wire Interaction Flow");
        flowManager.Configure(boardManager, playerMover, playerObject, targetDoor);
        EditorUtility.SetDirty(flowManager);
        EditorSceneManager.MarkSceneDirty(flowManager.gameObject.scene);

        Selection.activeGameObject = flowManager.gameObject;
        EditorGUIUtility.PingObject(flowManager);

        var missing = new List<string>();
        if (playerMover == null)
            missing.Add("Player Mover");
        if (playerObject == null)
            missing.Add("Player Object");
        if (targetDoor == null)
            missing.Add("Target Door");

        if (missing.Count > 0)
        {
            EditorUtility.DisplayDialog(
                "Wire Interaction Flow",
                $"Wired what could be found, but still missing: {string.Join(", ", missing)}.\n\nIf the player is missing, select the Player and run Tools > Lead The Way > Board > Setup Selected Player Progression.",
                "OK");
            return;
        }

        EditorUtility.DisplayDialog("Wire Interaction Flow", "Wired BoardManager, PlayerMover, PlayerObject, and TargetDoor.", "OK");
    }

    [MenuItem("Tools/Lead The Way/UI/Setup Interaction Counter")]
    public static void SetupInteractionCounter()
    {
        var canvas = FindOrCreateCanvas();
        SetupInteractionCounter(canvas);
        EditorUtility.DisplayDialog("Setup Interaction Counter", "Created and wired the top-left interaction counter.", "OK");
    }

    [MenuItem("Tools/Lead The Way/UI/Setup Gameplay UI")]
    public static void SetupGameplayUI()
    {
        var canvas = FindOrCreateCanvas();
        var controlUI = SetupStableControlUI(canvas);
        SetupInteractionCounter(canvas);

        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Selection.activeGameObject = controlUI;
        EditorGUIUtility.PingObject(controlUI);
        EditorUtility.DisplayDialog("Setup Gameplay UI", "Gameplay UI is styled, parented under Canvas, and wired.", "OK");
    }

    [MenuItem("Tools/Lead The Way/UI/Wire Control UI References")]
    public static void WireControlUIReferences()
    {
        var canvas = FindOrCreateCanvas();
        var root = SetupStableControlUI(canvas);
        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);
        EditorUtility.DisplayDialog("Wire Control UI", "Control UI panels, templates, pagination, and references are wired.", "OK");
    }

    [MenuItem("Tools/Lead The Way/UI/Repair Control UI Visibility")]
    public static void RepairControlUIVisibility()
    {
        var selectionUI = UnityEngine.Object.FindAnyObjectByType<SelectionPanelsUI>();
        if (selectionUI == null)
        {
            EditorUtility.DisplayDialog("Repair Control UI", "No SelectionPanelsUI exists yet. Run Tools > Lead The Way > UI > Wire Control UI References first.", "OK");
            return;
        }

        var canvas = FindOrCreateCanvas();
        var root = selectionUI.gameObject;
        Undo.SetTransformParent(root.transform, canvas.transform, "Parent Control UI To Canvas");
        root.transform.SetAsLastSibling();
        root.SetActive(true);

        var rootRect = root.GetComponent<RectTransform>();
        ConfigureRect(rootRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(520f, 96f));

        var serialized = new SerializedObject(selectionUI);
        var objectPanel = serialized.FindProperty("objectPanel")?.objectReferenceValue as GameObject;
        var actionPanel = serialized.FindProperty("actionPanel")?.objectReferenceValue as GameObject;
        if (objectPanel != null)
            objectPanel.SetActive(true);
        if (actionPanel != null)
            actionPanel.SetActive(false);

        EditorUtility.SetDirty(canvas);
        EditorUtility.SetDirty(root);
        EditorUtility.SetDirty(selectionUI);
        EditorSceneManager.MarkSceneDirty(root.scene);

        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);
        EditorUtility.DisplayDialog("Repair Control UI", "Control UI is now parented under a Screen Space Overlay Canvas and positioned bottom-center.", "OK");
    }

    [MenuItem("Tools/Lead The Way/Board/Setup Selected Board Objects/Auto Guess")]
    public static void SetupSelectedBoardObjectsAuto()
    {
        SetupSelectedBoardObjects(null);
    }

    [MenuItem("Tools/Lead The Way/Board/Setup Selected Board Objects/As Player")]
    public static void SetupSelectedBoardObjectsAsPlayer()
    {
        SetupSelectedBoardObjects(BoardObjectType.Player);
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
                flowManager.Configure(boardManager, playerMover, boardObject, FindFirstSceneBoardObjectOfType(BoardObjectType.Door));
                EditorUtility.SetDirty(flowManager);
            }
        }

        boardManager.RebuildRegistry();
        EditorSceneManager.MarkSceneDirty(targets[0].scene);
        EditorUtility.DisplayDialog("Setup Player Progression", $"Configured {targets.Count} player object(s) for interaction-driven board movement.", "OK");
    }

    [MenuItem("Tools/Lead The Way/Board/Setup Selected Board Objects/As Box")]
    public static void SetupSelectedBoardObjectsAsBox()
    {
        SetupSelectedBoardObjects(BoardObjectType.Box);
    }

    [MenuItem("Tools/Lead The Way/Board/Setup Selected Board Objects/As Door")]
    public static void SetupSelectedBoardObjectsAsDoor()
    {
        SetupSelectedBoardObjects(BoardObjectType.Door);
    }

    [MenuItem("Tools/Lead The Way/Board/Setup Selected Board Objects/As Spike")]
    public static void SetupSelectedBoardObjectsAsSpike()
    {
        SetupSelectedBoardObjects(BoardObjectType.Spike);
    }

    [MenuItem("Tools/Lead The Way/Board/Setup Selected Board Objects/As Button")]
    public static void SetupSelectedBoardObjectsAsButton()
    {
        SetupSelectedBoardObjects(BoardObjectType.Button);
    }

    [MenuItem("Tools/Lead The Way/Board/Setup Selected Board Objects/As Lever")]
    public static void SetupSelectedBoardObjectsAsLever()
    {
        SetupSelectedBoardObjects(BoardObjectType.Lever);
    }

    [MenuItem("Tools/Lead The Way/Board/Setup Selected Board Objects/As Wall")]
    public static void SetupSelectedBoardObjectsAsWall()
    {
        SetupSelectedBoardObjects(BoardObjectType.Wall);
    }

    [MenuItem("Tools/Lead The Way/Board/Setup Selected Board Objects/As Goal")]
    public static void SetupSelectedBoardObjectsAsGoal()
    {
        SetupSelectedBoardObjects(BoardObjectType.Goal);
    }

    [MenuItem("Tools/Lead The Way/Board/Setup Selected Destination Tile")]
    public static void SetupSelectedDestinationTile()
    {
        SetupSelectedBoardObjects(BoardObjectType.Goal);
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

    [MenuItem("Tools/Lead The Way/Board/Setup Selected Board Movers As Boxes")]
    public static void SetupSelectedBoardMoversAsBoxes()
    {
        var targets = GetSelectedSceneObjects();
        if (targets.Count == 0)
        {
            EditorUtility.DisplayDialog("Setup Board Movers", "Select one or more box GameObjects in the Hierarchy first.", "OK");
            return;
        }

        var boardManager = UnityEngine.Object.FindAnyObjectByType<BoardManager>();
        if (boardManager == null)
        {
            EditorUtility.DisplayDialog("Setup Board Movers", "Create a BoardManager first: Tools > Lead The Way > Board > Create Board Manager.", "OK");
            return;
        }

        foreach (var target in targets)
        {
            var boardObject = GetOrAddComponent<BoardObject>(target);
            var mover = GetOrAddComponent<GridTileMover>(target);

            Undo.RecordObject(boardObject, "Setup Board Mover");
            Undo.RecordObject(mover, "Setup Board Mover");

            boardObject.Configure(BoardObjectType.Box, true, true, true);
            boardObject.SyncTileFromTransform(boardManager.WorldOrigin, boardManager.TileSize);
            mover.ConfigureBoardMovement(true, true);

            EditorUtility.SetDirty(boardObject);
            EditorUtility.SetDirty(mover);
        }

        boardManager.RebuildRegistry();
        EditorSceneManager.MarkSceneDirty(targets[0].scene);
        EditorUtility.DisplayDialog("Setup Board Movers", $"Configured {targets.Count} board-controlled box mover(s).", "OK");
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

            report.Add($"{boardObject.name}: {boardObject.ObjectType}, tile {boardObject.TilePosition}, active={boardObject.gameObject.activeInHierarchy}, scene={boardObject.gameObject.scene.name}, blocks={boardObject.BlocksMovement}, movable={boardObject.Movable}");
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

    private static GameObject SetupControlUIFromPrefab(Canvas canvas)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ControlUIPrefabPath);
        if (prefab == null)
        {
            EditorUtility.DisplayDialog("Setup Gameplay UI", $"Could not find UI prefab at {ControlUIPrefabPath}.", "OK");
            return null;
        }

        foreach (var existing in UnityEngine.Object.FindObjectsByType<SelectionPanelsUI>(FindObjectsInactive.Include))
        {
            if (existing == null || EditorUtility.IsPersistent(existing))
                continue;

            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        var root = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        Undo.RegisterCreatedObjectUndo(root, "Create Control UI From Prefab");
        root.name = "Control UI";
        Undo.SetTransformParent(root.transform, canvas.transform, "Parent Control UI To Canvas");
        root.transform.SetAsLastSibling();
        root.SetActive(true);

        UnwrapNestedCanvas(root.transform);

        var rootRect = root.GetComponent<RectTransform>();
        ConfigureRect(rootRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(520f, 96f));

        var selectionUI = root.GetComponent<SelectionPanelsUI>();
        var audioSource = GetOrAddComponent<AudioSource>(root);
        var objectPanel = FindDeep(root.transform, "Object Panel")?.gameObject;
        var actionPanel = FindDeep(root.transform, "Action Panel")?.gameObject;
        var objectContainer = FindDeep(root.transform, "Object Button Container");
        var actionContainer = FindDeep(root.transform, "Action Button Container");
        var objectTemplate = FindDeep(root.transform, "Object Button Template")?.GetComponent<Button>();
        var actionTemplate = FindDeep(root.transform, "Action Button Template")?.GetComponent<Button>();

        if (objectPanel != null)
            StyleMainPanel(objectPanel, true);
        if (actionPanel != null)
            StyleMainPanel(actionPanel, false);

        if (objectContainer != null)
            ConfigureButtonContainer(objectContainer);
        if (actionContainer != null)
            ConfigureButtonContainer(actionContainer);

        if (objectTemplate != null)
            StyleControlButton(objectTemplate);
        if (actionTemplate != null)
            StyleControlButton(actionTemplate);

        var previousIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/ButtonSet/Textures/icons/128x128/arrow_left.png");
        var nextIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/ButtonSet/Textures/icons/128x128/arrow_right.png");
        var playIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/ButtonSet/Textures/icons/128x128/play.png");

        var previousButton = objectPanel != null ? EnsureSmallIconButton(objectPanel.transform, "Previous Object Page", previousIcon, new Vector2(-74f, -8f)) : null;
        var nextButton = objectPanel != null ? EnsureSmallIconButton(objectPanel.transform, "Next Object Page", nextIcon, new Vector2(-18f, -8f)) : null;
        var pageText = objectPanel != null ? EnsureText(objectPanel.transform, "Object Page Text", "1/1", 13, FontStyle.Bold, TextAnchor.MiddleCenter) : null;
        if (pageText != null)
        {
            ConfigureRect(pageText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(-46f, -8f), new Vector2(38f, 16f));
            pageText.color = new Color(0.24f, 0.17f, 0.08f, 0.9f);
            pageText.raycastTarget = false;
        }

        var backButton = actionPanel != null ? EnsureBackButton(actionPanel.transform) : null;
        var objectTitle = objectPanel != null ? FindDeep(objectPanel.transform, "Title")?.GetComponent<Text>() : null;
        var actionTitle = actionPanel != null ? FindDeep(actionPanel.transform, "Title")?.GetComponent<Text>() : null;
        StyleTitle(objectTitle, "SELECT OBJECT");
        StyleTitle(actionTitle, "ACTION");

        var serialized = new SerializedObject(selectionUI);
        SetObject(serialized, "objectPanel", objectPanel);
        SetObject(serialized, "actionPanel", actionPanel);
        SetObject(serialized, "objectPanelTitle", objectTitle);
        SetObject(serialized, "objectButtonContainer", objectContainer);
        SetObject(serialized, "objectButtonTemplate", objectTemplate);
        SetObject(serialized, "objectPreviousPageButton", previousButton);
        SetObject(serialized, "objectNextPageButton", nextButton);
        SetObject(serialized, "objectPageText", pageText);
        SetObject(serialized, "previousPageIcon", previousIcon);
        SetObject(serialized, "nextPageIcon", nextIcon);
        SetObject(serialized, "actionPanelTitle", actionTitle);
        SetObject(serialized, "actionButtonContainer", actionContainer);
        SetObject(serialized, "actionButtonTemplate", actionTemplate);
        SetObject(serialized, "backButton", backButton);
        SetObject(serialized, "defaultActionIcon", playIcon);
        SetObject(serialized, "audioSource", audioSource);
        SetObject(serialized, "interactionFlowManager", UnityEngine.Object.FindAnyObjectByType<InteractionFlowManager>());
        serialized.ApplyModifiedProperties();

        if (objectPanel != null)
            objectPanel.SetActive(true);
        if (actionPanel != null)
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
        ConfigureCounterText(countText, "0", 26, FontStyle.Bold, TextAnchor.LowerLeft, new Vector2(12f, -18f), new Vector2(144f, 30f), new Color(1f, 0.93f, 0.72f, 1f));

        Undo.RecordObject(counterUI, "Setup Interaction Counter");
        counterUI.Configure(flowManager, countText);
        EditorUtility.SetDirty(counterUI);
        EditorUtility.SetDirty(counterRoot);
    }

    private static void UnwrapNestedCanvas(Transform root)
    {
        var nestedCanvas = root.GetComponentInChildren<Canvas>(true);
        if (nestedCanvas == null || nestedCanvas.transform == root)
            return;

        var nestedTransform = nestedCanvas.transform;
        while (nestedTransform.childCount > 0)
            Undo.SetTransformParent(nestedTransform.GetChild(0), root, "Move Styled UI Out Of Nested Canvas");

        Undo.DestroyObjectImmediate(nestedCanvas.gameObject);
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
