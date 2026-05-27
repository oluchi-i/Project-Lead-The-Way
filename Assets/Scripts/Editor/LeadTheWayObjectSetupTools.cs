using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LeadTheWayObjectSetupTools
{
    private const string ButtonSetFolder = "Assets/Art/UI/ButtonSet";

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
        foreach (var selectable in Object.FindObjectsByType<SelectableControlObject>(FindObjectsSortMode.None))
        {
            if (selectable.gameObject != target)
                usedSlots.Add(selectable.slotNumber);
        }

        var slot = 1;
        while (usedSlots.Contains(slot))
            slot++;

        return slot;
    }
}
