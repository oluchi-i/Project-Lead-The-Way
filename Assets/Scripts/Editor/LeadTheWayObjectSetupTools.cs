using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LeadTheWayObjectSetupTools
{
    private const string ButtonSetFolder = "Assets/Art/UI/ButtonSet";
    private const string TempAssetFolder = "Assets/Temp/HaniJahanDesign/FreePack";
    private const string PermanentPackFolder = "Assets/Art/HaniJahanDesign/FreePack";
    private const string PermanentInteractablePrefabFolder = "Assets/Prefabs/Interactables";

    [MenuItem("Tools/Lead The Way/Setup Selected Movable Box")]
    public static void SetupSelectedMovableBox()
    {
        var target = Selection.activeGameObject;
        if (target == null)
        {
            EditorUtility.DisplayDialog("Setup Movable Box", "Select the box GameObject in the Hierarchy first.", "OK");
            return;
        }

        var mover = GetOrAddComponent<GridTileMover>(target);
        var selectable = GetOrAddComponent<SelectableControlObject>(target);
        var connector = GetOrAddComponent<ControlObjectConnector>(target);

        var actions = new List<ControlAction>
        {
            CreateMoveAction("X +1", "Textures/icons/128x128/arrow_left.png", mover.MovePositiveX),
            CreateMoveAction("X -1", "Textures/icons/128x128/arrow_right.png", mover.MoveNegativeX),
            CreateMoveAction("Z -1", "Textures/icons/128x128/arrow_up.png", mover.MoveNegativeZ),
            CreateMoveAction("Z +1", "Textures/icons/128x128/arrow_down.png", mover.MovePositiveZ)
        };

        Undo.RecordObject(mover, "Setup Movable Box");
        Undo.RecordObject(selectable, "Setup Movable Box Selectable");
        Undo.RecordObject(connector, "Setup Movable Box Connector");

        connector.Configure(GetNextControlSlot(target), ObjectNames.NicifyVariableName(target.name), null, actions);

        EditorUtility.SetDirty(mover);
        EditorUtility.SetDirty(selectable);
        EditorUtility.SetDirty(connector);
        EditorSceneManager.MarkSceneDirty(target.scene);

        Selection.activeGameObject = target;
        EditorGUIUtility.PingObject(target);
        EditorUtility.DisplayDialog("Setup Movable Box", $"Added tile movement and selectable UI controls to {target.name}.", "OK");
    }

    [MenuItem("Tools/Lead The Way/Setup Selected Spike Toggle")]
    public static void SetupSelectedSpikeToggle()
    {
        var target = Selection.activeGameObject;
        if (target == null)
        {
            EditorUtility.DisplayDialog("Setup Spike Toggle", "Select the Spike parent GameObject in the Hierarchy first.", "OK");
            return;
        }

        var spikes = target.transform.Find("spikes") ?? target.transform.Find("Spikes");
        if (spikes == null)
        {
            EditorUtility.DisplayDialog("Setup Spike Toggle", "Could not find a child named 'spikes' or 'Spikes'.", "OK");
            return;
        }

        var spikeToggle = GetOrAddComponent<SpikeToggle>(target);
        var selectable = GetOrAddComponent<SelectableControlObject>(target);
        var connector = GetOrAddComponent<ControlObjectConnector>(target);

        Undo.RecordObject(spikeToggle, "Setup Spike Toggle");
        spikeToggle.Configure(spikes);

        var action = new ControlAction
        {
            label = "Toggle Spikes",
            icon = LoadButtonSetSprite("Textures/icons/128x128/arrow_up.png")
        };
        UnityEventTools.AddPersistentListener(action.onSelected, spikeToggle.ToggleSpikes);

        Undo.RecordObject(selectable, "Setup Spike Selectable");
        Undo.RecordObject(connector, "Setup Spike Connector");
        connector.Configure(GetNextControlSlot(target), ObjectNames.NicifyVariableName(target.name), null, new List<ControlAction> { action });

        EditorUtility.SetDirty(spikeToggle);
        EditorUtility.SetDirty(selectable);
        EditorUtility.SetDirty(connector);
        EditorSceneManager.MarkSceneDirty(target.scene);

        Selection.activeGameObject = target;
        EditorGUIUtility.PingObject(target);
        EditorUtility.DisplayDialog("Setup Spike Toggle", $"Added spike toggle controls to {target.name}.", "OK");
    }

    [MenuItem("Tools/Lead The Way/Setup Selected Lever Toggle")]
    public static void SetupSelectedLeverToggle()
    {
        var target = Selection.activeGameObject;
        if (target == null)
        {
            EditorUtility.DisplayDialog("Setup Lever Toggle", "Select the Lever parent GameObject in the Hierarchy first.", "OK");
            return;
        }

        var arm = target.transform.Find("Arm") ?? target.transform.Find("arm");
        if (arm == null)
        {
            EditorUtility.DisplayDialog("Setup Lever Toggle", "Could not find a child named 'Arm' or 'arm'.", "OK");
            return;
        }

        var leverToggle = GetOrAddComponent<LeverToggle>(target);
        var selectable = GetOrAddComponent<SelectableControlObject>(target);
        var connector = GetOrAddComponent<ControlObjectConnector>(target);

        Undo.RecordObject(leverToggle, "Setup Lever Toggle");
        leverToggle.Configure(arm);

        var action = new ControlAction
        {
            label = "Toggle Lever",
            icon = LoadButtonSetSprite("Textures/icons/128x128/play.png")
        };
        UnityEventTools.AddPersistentListener(action.onSelected, leverToggle.ToggleLever);

        Undo.RecordObject(selectable, "Setup Lever Selectable");
        Undo.RecordObject(connector, "Setup Lever Connector");
        connector.Configure(GetNextControlSlot(target), ObjectNames.NicifyVariableName(target.name), null, new List<ControlAction> { action });

        EditorUtility.SetDirty(leverToggle);
        EditorUtility.SetDirty(selectable);
        EditorUtility.SetDirty(connector);
        EditorSceneManager.MarkSceneDirty(target.scene);

        Selection.activeGameObject = target;
        EditorGUIUtility.PingObject(target);
        EditorUtility.DisplayDialog("Setup Lever Toggle", $"Added lever toggle controls to {target.name}.", "OK");
    }

    [MenuItem("Tools/Lead The Way/Setup Selected Button Press")]
    public static void SetupSelectedButtonPress()
    {
        var target = Selection.activeGameObject;
        if (target == null)
        {
            EditorUtility.DisplayDialog("Setup Button Press", "Select the Button parent GameObject in the Hierarchy first.", "OK");
            return;
        }

        var cap = target.transform.Find("Cap") ?? target.transform.Find("cap");
        if (cap == null)
        {
            EditorUtility.DisplayDialog("Setup Button Press", "Could not find a child named 'Cap' or 'cap'.", "OK");
            return;
        }

        var buttonPress = GetOrAddComponent<ButtonPress>(target);
        var selectable = GetOrAddComponent<SelectableControlObject>(target);
        var connector = GetOrAddComponent<ControlObjectConnector>(target);
        var audioClip = LoadAudioClip("Assets/Sound/SoundEffects/button_object_click.mp3");

        Undo.RecordObject(buttonPress, "Setup Button Press");
        buttonPress.Configure(cap, audioClip);

        var action = new ControlAction
        {
            label = "Press Button",
            icon = LoadButtonSetSprite("Textures/icons/128x128/play.png")
        };
        UnityEventTools.AddPersistentListener(action.onSelected, buttonPress.PressButton);

        Undo.RecordObject(selectable, "Setup Button Selectable");
        Undo.RecordObject(connector, "Setup Button Connector");
        connector.Configure(GetNextControlSlot(target), ObjectNames.NicifyVariableName(target.name), null, new List<ControlAction> { action });

        EditorUtility.SetDirty(buttonPress);
        EditorUtility.SetDirty(selectable);
        EditorUtility.SetDirty(connector);
        EditorSceneManager.MarkSceneDirty(target.scene);

        Selection.activeGameObject = target;
        EditorGUIUtility.PingObject(target);
        EditorUtility.DisplayDialog("Setup Button Press", $"Added button press controls to {target.name}.", "OK");
    }

    [MenuItem("Tools/Lead The Way/Temp Cleanup/Make Selected Scene Objects Permanent")]
    public static void MakeSelectedSceneObjectsPermanent()
    {
        var selectedRoots = GetSelectedSceneRoots();
        if (selectedRoots.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Make Objects Permanent",
                "Select one or more scene objects in the Hierarchy first, such as Box, Spike, Lever, and Button.",
                "OK");
            return;
        }

        var tempDependencies = CollectTempDependencies(selectedRoots);
        if (!EditorUtility.DisplayDialog(
                "Make Objects Permanent",
                $"This will move {tempDependencies.Count} model/material/texture asset(s) out of Temp, unpack the selected prefab instances, and save {selectedRoots.Count} new prefab(s) in {PermanentInteractablePrefabFolder}. Continue?",
                "Continue",
                "Cancel"))
        {
            return;
        }

        EnsureFolder(PermanentPackFolder);
        EnsureFolder(PermanentInteractablePrefabFolder);

        var movedAssets = MoveTempDependencies(tempDependencies);
        AssetDatabase.Refresh();

        var createdPrefabs = new List<string>();
        foreach (var selectedRoot in selectedRoots)
        {
            var prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(selectedRoot);
            var root = prefabRoot != null ? prefabRoot : selectedRoot;

            if (prefabRoot != null)
                PrefabUtility.UnpackPrefabInstance(prefabRoot, PrefabUnpackMode.Completely, InteractionMode.UserAction);

            var prefabPath = AssetDatabase.GenerateUniqueAssetPath(
                $"{PermanentInteractablePrefabFolder}/{SanitizeFileName(root.name)}.prefab");

            var savedPrefab = PrefabUtility.SaveAsPrefabAssetAndConnect(
                root,
                prefabPath,
                InteractionMode.UserAction,
                out var success);

            if (success && savedPrefab != null)
                createdPrefabs.Add(prefabPath);
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Make Objects Permanent",
            $"Moved {movedAssets.Count} asset(s) out of Temp.\nCreated {createdPrefabs.Count} prefab(s).\n\nRun 'Report Open Scene Temp References' next before deleting Temp.",
            "OK");
    }

    [MenuItem("Tools/Lead The Way/Temp Cleanup/Report Open Scene Temp References")]
    public static void ReportOpenSceneTempReferences()
    {
        var roots = new List<GameObject>();
        SceneManager.GetActiveScene().GetRootGameObjects(roots);

        var dependencies = CollectTempReferences(roots);
        if (dependencies.Count == 0)
        {
            Debug.Log("Lead The Way Temp Cleanup: active scene has no references under Assets/Temp/HaniJahanDesign/FreePack.");
            EditorUtility.DisplayDialog("Temp References", "The active scene has no references under Temp.", "OK");
            return;
        }

        Debug.LogWarning("Lead The Way Temp Cleanup: active scene still references Temp assets:\n" + string.Join("\n", dependencies));
        EditorUtility.DisplayDialog(
            "Temp References",
            $"The active scene still references {dependencies.Count} Temp asset(s). Check the Console for the list.",
            "OK");
    }

    private static ControlAction CreateMoveAction(string label, string iconPath, UnityEngine.Events.UnityAction callback)
    {
        var action = new ControlAction
        {
            label = label,
            icon = LoadButtonSetSprite(iconPath)
        };
        UnityEventTools.AddPersistentListener(action.onSelected, callback);
        return action;
    }

    private static Sprite LoadButtonSetSprite(string relativePath)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{ButtonSetFolder}/{relativePath}");
    }

    private static AudioClip LoadAudioClip(string path)
    {
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }

    private static T GetOrAddComponent<T>(GameObject target) where T : Component
    {
        if (target.TryGetComponent<T>(out var existing))
            return existing;

        return Undo.AddComponent<T>(target);
    }

    private static int GetNextControlSlot(GameObject target)
    {
        var usedSlots = new HashSet<int>();
        foreach (var selectable in UnityEngine.Object.FindObjectsByType<SelectableControlObject>(FindObjectsSortMode.None))
        {
            if (selectable.gameObject != target)
                usedSlots.Add(selectable.slotNumber);
        }

        var slot = 1;
        while (usedSlots.Contains(slot))
            slot++;

        return slot;
    }

    private static List<GameObject> GetSelectedSceneRoots()
    {
        var roots = new List<GameObject>();
        foreach (var selected in Selection.gameObjects)
        {
            if (selected == null || EditorUtility.IsPersistent(selected))
                continue;

            var root = PrefabUtility.GetOutermostPrefabInstanceRoot(selected);
            if (root == null)
                root = selected;

            if (!roots.Contains(root))
                roots.Add(root);
        }

        return roots;
    }

    private static List<string> CollectTempDependencies(List<GameObject> roots)
    {
        var dependencies = new HashSet<string>();
        var scanQueue = new Queue<string>();

        foreach (var dependency in EditorUtility.CollectDependencies(roots.ToArray()))
        {
            var path = AssetDatabase.GetAssetPath(dependency);
            if (ShouldMoveOutOfTemp(path) && dependencies.Add(path))
                scanQueue.Enqueue(path);
        }

        while (scanQueue.Count > 0)
        {
            var path = scanQueue.Dequeue();
            foreach (var nestedDependency in AssetDatabase.GetDependencies(path, true))
            {
                if (ShouldMoveOutOfTemp(nestedDependency) && dependencies.Add(nestedDependency))
                    scanQueue.Enqueue(nestedDependency);
            }
        }

        var sortedDependencies = new List<string>(dependencies);
        sortedDependencies.Sort(StringComparer.Ordinal);
        return sortedDependencies;
    }

    private static List<string> CollectTempReferences(List<GameObject> roots)
    {
        var references = new HashSet<string>();
        foreach (var dependency in EditorUtility.CollectDependencies(roots.ToArray()))
        {
            var path = AssetDatabase.GetAssetPath(dependency);
            if (!string.IsNullOrWhiteSpace(path) && path.StartsWith(TempAssetFolder, StringComparison.Ordinal))
                references.Add(path);
        }

        var sortedReferences = new List<string>(references);
        sortedReferences.Sort(StringComparer.Ordinal);
        return sortedReferences;
    }

    private static List<string> MoveTempDependencies(List<string> dependencyPaths)
    {
        var movedAssets = new List<string>();
        foreach (var sourcePath in dependencyPaths)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
                continue;

            var destinationPath = GetPermanentPath(sourcePath);
            EnsureFolder(Path.GetDirectoryName(destinationPath)?.Replace('\\', '/'));

            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(destinationPath) != null)
                destinationPath = AssetDatabase.GenerateUniqueAssetPath(destinationPath);

            var error = AssetDatabase.MoveAsset(sourcePath, destinationPath);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError($"Lead The Way Temp Cleanup: failed to move {sourcePath} to {destinationPath}: {error}");
                continue;
            }

            movedAssets.Add(destinationPath);
        }

        return movedAssets;
    }

    private static bool ShouldMoveOutOfTemp(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
            return false;

        if (!assetPath.StartsWith(TempAssetFolder, StringComparison.Ordinal))
            return false;

        var extension = Path.GetExtension(assetPath).ToLowerInvariant();
        return extension == ".fbx"
            || extension == ".mat"
            || extension == ".png"
            || extension == ".jpg"
            || extension == ".jpeg"
            || extension == ".tga"
            || extension == ".psd";
    }

    private static string GetPermanentPath(string sourcePath)
    {
        var relativePath = sourcePath.Substring(TempAssetFolder.Length).TrimStart('/', '\\');
        return $"{PermanentPackFolder}/{relativePath}".Replace('\\', '/');
    }

    private static void EnsureFolder(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || AssetDatabase.IsValidFolder(folderPath))
            return;

        var normalized = folderPath.Replace('\\', '/');
        var pieces = normalized.Split('/');
        var current = pieces[0];

        for (var i = 1; i < pieces.Length; i++)
        {
            var next = $"{current}/{pieces[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, pieces[i]);

            current = next;
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        foreach (var invalidChar in Path.GetInvalidFileNameChars())
            fileName = fileName.Replace(invalidChar, '_');

        return string.IsNullOrWhiteSpace(fileName) ? "Interactable" : fileName;
    }
}
