using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;

public static class LeadTheWayObjectSetupTools
{
    private const string ObjectIconFolder = "Assets/Art/UI/ObjectIcons";
    private const string ArrowRightIconPath = "Assets/Art/UI/ButtonSet/Textures/icons/128x128/arrow_right.png";
    private const string ArrowLeftIconPath = "Assets/Art/UI/ButtonSet/Textures/icons/128x128/arrow_left.png";
    private const string ArrowUpIconPath = "Assets/Art/UI/ButtonSet/Textures/icons/128x128/arrow_up.png";
    private const string ArrowDownIconPath = "Assets/Art/UI/ButtonSet/Textures/icons/128x128/arrow_down.png";

    [MenuItem("Tools/Lead The Way/UI/Wire Control UI Audio Source")]
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
}
