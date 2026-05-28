using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static partial class LeadTheWayObjectSetupTools
{
    private const string PanelSoftPath = "Assets/Prefabs/UI/Sprites/PanelSoft.asset";
    private const string BadgeSoftPath = "Assets/Prefabs/UI/Sprites/BadgeSoft.asset";
    private const string PoppinsBoldPath = "Assets/Art/UI/Fonts/Poppins-Bold.ttf";
    private const string ButtonRoundPath = "Assets/Art/UI/ButtonSet/Textures/buttons/button_round_130.png";
    private const string SmallPanelPath = "Assets/Art/UI/ButtonSet/Textures/controls/universal_panel_20.png";
    private const string MediumCountdownDialPath = "Assets/Art/UI/Countdown/CountdownDialMedium.png";
    private const string ControlUIPrefabPath = "Assets/Prefabs/UI/ControlUI.prefab";

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
}
