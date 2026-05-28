using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;

public static class LeadTheWayDoorPrefabTools
{
    private const string InteractableDoorFolder = "Assets/Prefabs/Interactables/Doors";
    private const string DoorOpenSoundPath = "Assets/Art/Doors/Audio/Door_Open.wav";
    private const string DoorCloseSoundPath = "Assets/Art/Doors/Audio/Door_Close.wav";
    private const string DefaultDoorActionIconPath = "Assets/Art/UI/ButtonSet/Textures/icons/128x128/play.png";
    private const string GeneratedDoorIconFolder = "Assets/Art/UI/ObjectIcons";

    [MenuItem("Tools/Lead The Way/Doors/Setup Interactable Door Prefabs")]
    public static void SetupInteractableDoorPrefabs()
    {
        if (!AssetDatabase.IsValidFolder(InteractableDoorFolder))
        {
            EditorUtility.DisplayDialog("Setup Door Prefabs", $"Missing folder:\n{InteractableDoorFolder}", "OK");
            return;
        }

        var prefabPaths = AssetDatabase
            .FindAssets("t:Prefab", new[] { InteractableDoorFolder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .ToList();

        var configuredCount = 0;
        foreach (var prefabPath in prefabPaths)
        {
            if (ConfigureDoorPrefab(prefabPath))
                configuredCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Setup Door Prefabs",
            configuredCount == 1
                ? "Configured 1 interactable door prefab."
                : $"Configured {configuredCount} interactable door prefabs.",
            "OK");
    }

    private static bool ConfigureDoorPrefab(string prefabPath)
    {
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var prefabName = Path.GetFileNameWithoutExtension(prefabPath);
            root.name = prefabName;

            var door = root.GetComponentInChildren<DoorScript.Door>(true);
            if (door == null)
            {
                var doorTransform = root.transform.Find("Door") ?? root.transform;
                door = doorTransform.gameObject.AddComponent<DoorScript.Door>();
            }

            var audioSource = door.GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = door.gameObject.AddComponent<AudioSource>();

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.2f;

            door.asource = audioSource;
            door.openDoor = AssetDatabase.LoadAssetAtPath<AudioClip>(DoorOpenSoundPath);
            door.closeDoor = AssetDatabase.LoadAssetAtPath<AudioClip>(DoorCloseSoundPath);
            door.soundVolume = 1.5f;
            door.spatialBlend = 0.2f;

            var boardObject = root.GetComponent<BoardObject>();
            if (boardObject == null)
                boardObject = root.AddComponent<BoardObject>();

            boardObject.Configure(BoardObjectType.Door, true, true, false);

            var selectable = root.GetComponent<SelectableControlObject>();
            if (selectable == null)
                selectable = root.AddComponent<SelectableControlObject>();

            var icon = FindDoorIcon(prefabName);
            if (icon == null)
                icon = LeadTheWayIconTools.GenerateIconAssetForObject(root, $"{GeneratedDoorIconFolder}/{prefabName}.png");

            selectable.Configure("Door", icon, CreateDoorActions(door));

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static List<ControlAction> CreateDoorActions(DoorScript.Door door)
    {
        var action = new ControlAction
        {
            label = "Open / Close",
            icon = AssetDatabase.LoadAssetAtPath<Sprite>(DefaultDoorActionIconPath)
        };

        UnityEventTools.AddPersistentListener(action.onSelected, door.OpenDoor);
        return new List<ControlAction> { action };
    }

    private static Sprite FindDoorIcon(string prefabName)
    {
        var directIcon = AssetDatabase.LoadAssetAtPath<Sprite>($"{GeneratedDoorIconFolder}/{prefabName}.png");
        if (directIcon != null)
            return directIcon;

        if (prefabName.StartsWith("Door_"))
        {
            var legacyIcon = AssetDatabase.LoadAssetAtPath<Sprite>($"{GeneratedDoorIconFolder}/Door_4_{prefabName.Substring(5)}.png");
            if (legacyIcon != null)
                return legacyIcon;
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>($"{GeneratedDoorIconFolder}/Door_Yellow.png");
    }
}
