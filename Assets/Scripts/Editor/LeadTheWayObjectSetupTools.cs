using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

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
                return (true, false, true);
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
