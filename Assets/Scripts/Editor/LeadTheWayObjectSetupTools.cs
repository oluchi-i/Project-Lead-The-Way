using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class LeadTheWayObjectSetupTools
{
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
        var flowManager = UnityEngine.Object.FindAnyObjectByType<InteractionFlowManager>();
        if (flowManager == null)
            flowManager = Undo.AddComponent<InteractionFlowManager>(GetOrCreateGameSystems());

        var canvas = FindOrCreateCanvas();
        var counterUI = UnityEngine.Object.FindAnyObjectByType<InteractionCounterUI>();
        var counterRoot = counterUI != null ? counterUI.gameObject : null;

        if (counterRoot == null)
        {
            counterRoot = new GameObject("Interaction Counter", typeof(RectTransform), typeof(Image), typeof(InteractionCounterUI));
            Undo.RegisterCreatedObjectUndo(counterRoot, "Create Interaction Counter");
            counterRoot.transform.SetParent(canvas.transform, false);
            counterUI = counterRoot.GetComponent<InteractionCounterUI>();
        }
        else
        {
            Undo.RecordObject(counterRoot.transform, "Setup Interaction Counter");
            counterRoot.transform.SetParent(canvas.transform, false);
        }

        var counterRect = counterRoot.GetComponent<RectTransform>();
        counterRect.anchorMin = new Vector2(0f, 1f);
        counterRect.anchorMax = new Vector2(0f, 1f);
        counterRect.pivot = new Vector2(0f, 1f);
        counterRect.anchoredPosition = new Vector2(18f, -18f);
        counterRect.sizeDelta = new Vector2(168f, 54f);

        var background = counterRoot.GetComponent<Image>();
        background.color = new Color(0.16f, 0.11f, 0.05f, 0.88f);

        var labelText = FindOrCreateText(counterRoot.transform, "Label");
        ConfigureCounterText(labelText, "INTERACTIONS", 10, FontStyle.Bold, TextAnchor.UpperLeft, new Vector2(12f, -7f), new Vector2(144f, 16f), new Color(1f, 0.82f, 0.24f, 1f));

        var countText = FindOrCreateText(counterRoot.transform, "Count");
        ConfigureCounterText(countText, "0", 26, FontStyle.Bold, TextAnchor.LowerLeft, new Vector2(12f, -18f), new Vector2(144f, 30f), new Color(1f, 0.93f, 0.72f, 1f));

        Undo.RecordObject(counterUI, "Setup Interaction Counter");
        counterUI.Configure(flowManager, countText);
        EditorUtility.SetDirty(counterUI);
        EditorUtility.SetDirty(counterRoot);

        EditorSceneManager.MarkSceneDirty(counterRoot.scene);
        Selection.activeGameObject = counterRoot;
        EditorGUIUtility.PingObject(counterRoot);
        EditorUtility.DisplayDialog("Setup Interaction Counter", "Created and wired the top-left interaction counter.", "OK");
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
        if (normalizedName.Contains("goal") || normalizedName.Contains("exit"))
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
            return canvas;

        var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasObject, "Create Canvas");

        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
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
