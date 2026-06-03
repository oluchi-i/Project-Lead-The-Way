using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LeadTheWayWorldMapPrototypeTools
{
    private const string ScenePath = "Assets/Scenes/WorldMapPrototype.unity";
    private const string WorldMapRootName = "All-Locations World Map Prototype";
    private const string PathRootName = "World Map Path System";
    private const string WorldMapPlayerName = "World Map Player";
    private const string WorldMapCameraName = "World Map Camera";
    private const string MapPrefabRoot = "Assets/Prefabs/NonInteractables/Map";
    private const string LocationFolder = MapPrefabRoot + "/Locations";
    private const int Columns = 5;
    private const float SpacingX = 8.2f;
    private const float SpacingZ = 7.2f;
    private const float LocationTargetSize = 6.25f;

    [MenuItem("Tools/Lead The Way/Map/Generate World Map Prototype")]
    public static void GenerateWorldMapPrototype()
    {
        var locationPrefabPaths = GetLocationPrefabPaths();
        var missingPrefabs = GetMissingPrefabs(locationPrefabPaths);
        if (missingPrefabs.Count > 0)
        {
            EditorUtility.DisplayDialog(
                "Generate World Map Prototype",
                "Missing required map prefab(s):\n\n" + string.Join("\n", missingPrefabs),
                "OK");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "WorldMapPrototype";

        var root = new GameObject(WorldMapRootName);
        var waterRoot = CreateChild(root.transform, "Water");
        var locationsRoot = CreateChild(root.transform, "All Locations");
        var connectorRoot = CreateChild(root.transform, "Connector Bridge");
        var waypointRoot = CreateChild(root.transform, "Path Waypoints");

        BuildWater(waterRoot.transform, locationPrefabPaths.Count);

        for (var i = 0; i < locationPrefabPaths.Count; i++)
        {
            var path = locationPrefabPaths[i];
            var position = GetGridPosition(i, locationPrefabPaths.Count);
            var name = System.IO.Path.GetFileNameWithoutExtension(path);
            var location = InstantiateMapPrefab(path, "Location " + (i + 1).ToString("00") + " - " + name, position, Quaternion.identity, locationsRoot.transform);
            FitObjectToXZSize(location, LocationTargetSize);
            SnapObjectToGround(location);
            CreateWaypoint(waypointRoot.transform, "Player Waypoint " + (i + 1).ToString("00") + " - " + name, position + Vector3.up * 0.25f);
        }

        BuildSingleConnectorBridge(connectorRoot.transform, locationPrefabPaths.Count);
        CreateCameraAndLight(root.transform, locationPrefabPaths.Count);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();

        Selection.activeObject = root;
        EditorGUIUtility.PingObject(root);
        EditorUtility.DisplayDialog(
            "Generate World Map Prototype",
            $"Generated {locationPrefabPaths.Count} authored location prefab(s) in {ScenePath}.",
            "OK");
    }

    [MenuItem("Tools/Lead The Way/Map/Normalize Current World Map To Unit Scale")]
    public static void NormalizeCurrentWorldMapToUnitScale()
    {
        var root = GameObject.Find(WorldMapRootName);
        if (root == null)
        {
            EditorUtility.DisplayDialog(
                "Normalize World Map",
                $"Could not find {WorldMapRootName} in the active scene.",
                "OK");
            return;
        }

        var sourceScale = DetectLocationScale(root.transform);
        if (sourceScale <= 0.0001f)
        {
            EditorUtility.DisplayDialog(
                "Normalize World Map",
                "Could not find a uniformly scaled location or bridge object to use as the normalization source.",
                "OK");
            return;
        }

        if (Mathf.Approximately(sourceScale, 1f))
        {
            EditorUtility.DisplayDialog(
                "Normalize World Map",
                "The detected location scale is already 1, so no normalization was applied.",
                "OK");
            return;
        }

        var multiplier = 1f / sourceScale;
        if (!EditorUtility.DisplayDialog(
            "Normalize World Map",
            $"This one-time tool will set location/bridge prefab roots to scale 1,1,1 and scale map positions by {multiplier:0.###} so the composition stays aligned.\n\nRun it only once on this scene.",
            "Normalize",
            "Cancel"))
        {
            return;
        }

        var undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Normalize World Map To Unit Scale");

        NormalizePrefabGroup(root.transform.Find("All Locations"), multiplier);
        NormalizePrefabGroup(root.transform.Find("Connector Bridge"), multiplier);
        ScaleGeneratedWater(root.transform.Find("Water"), multiplier);
        ScaleDirectChildrenPositions(root.transform.Find("Path Waypoints"), multiplier);
        ScaleMapPathSystem(multiplier);
        ScaleNamedObjectPosition(WorldMapCameraName, multiplier);
        ScaleNamedObjectPosition("World Map Sun", multiplier);
        ScaleWorldMapPlayer(multiplier);

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"Lead The Way Map: normalized current world map to unit location scale using multiplier {multiplier:0.###}.");
    }

    private static List<string> GetLocationPrefabPaths()
    {
        var prefabPaths = new List<string>();
        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { LocationFolder });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(".prefab"))
                prefabPaths.Add(path);
        }

        prefabPaths.Sort(System.StringComparer.OrdinalIgnoreCase);
        return prefabPaths;
    }

    private static List<string> GetMissingPrefabs(IReadOnlyList<string> locationPrefabPaths)
    {
        var missing = new List<string>();
        if (locationPrefabPaths.Count == 0)
            missing.Add(LocationFolder + "/*.prefab");

        var bridgePath = $"{MapPrefabRoot}/Environment/wooden bridge.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(bridgePath) == null)
            missing.Add(bridgePath);

        return missing;
    }

    private static GameObject CreateChild(Transform parent, string name)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent);
        return child;
    }

    private static Material CreateMaterial(string name, Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        return new Material(shader)
        {
            name = name,
            color = color
        };
    }

    private static Vector3 GetGridPosition(int index, int count)
    {
        var rows = Mathf.CeilToInt(count / (float)Columns);
        var row = index / Columns;
        var col = index % Columns;
        var x = (col - (Columns - 1) * 0.5f) * SpacingX;
        var z = ((rows - 1) * 0.5f - row) * SpacingZ;
        return new Vector3(x, 0f, z);
    }

    private static void BuildWater(Transform parent, int locationCount)
    {
        var rows = Mathf.Max(1, Mathf.CeilToInt(locationCount / (float)Columns));
        var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
        water.name = "Single Water Plane";
        water.transform.SetParent(parent);
        water.transform.position = new Vector3(0f, -0.2f, 0f);
        water.transform.localScale = new Vector3(5.35f, 1f, Mathf.Max(2.15f, rows * 1.45f));
        Object.DestroyImmediate(water.GetComponent<Collider>());
        water.GetComponent<MeshRenderer>().sharedMaterial = CreateMaterial("Generated Simple Water", new Color(0.33f, 0.76f, 0.9f));
    }

    private static float DetectLocationScale(Transform root)
    {
        var scale = DetectScaleFromGroup(root.Find("All Locations"));
        if (scale > 0.0001f)
            return scale;

        return DetectScaleFromGroup(root.Find("Connector Bridge"));
    }

    private static float DetectScaleFromGroup(Transform group)
    {
        if (group == null)
            return 0f;

        for (var i = 0; i < group.childCount; i++)
        {
            var child = group.GetChild(i);
            if (IsUniformNonUnitScale(child.localScale))
                return child.localScale.x;
        }

        return 0f;
    }

    private static bool IsUniformNonUnitScale(Vector3 scale)
    {
        return Mathf.Abs(scale.x - scale.y) <= 0.001f
            && Mathf.Abs(scale.x - scale.z) <= 0.001f
            && Mathf.Abs(scale.x - 1f) > 0.001f
            && scale.x > 0.0001f;
    }

    private static void NormalizePrefabGroup(Transform group, float multiplier)
    {
        if (group == null)
            return;

        for (var i = 0; i < group.childCount; i++)
        {
            var child = group.GetChild(i);
            Undo.RecordObject(child, "Normalize Map Prefab Root");
            child.position = ScalePosition(child.position, multiplier);
            if (IsUniformNonUnitScale(child.localScale))
                child.localScale = Vector3.one;
            EditorUtility.SetDirty(child);
        }
    }

    private static void ScaleGeneratedWater(Transform waterRoot, float multiplier)
    {
        if (waterRoot == null)
            return;

        for (var i = 0; i < waterRoot.childCount; i++)
        {
            var child = waterRoot.GetChild(i);
            Undo.RecordObject(child, "Scale Generated Map Water");
            child.position = ScalePosition(child.position, multiplier);
            child.localScale = new Vector3(
                child.localScale.x * multiplier,
                child.localScale.y,
                child.localScale.z * multiplier);
            EditorUtility.SetDirty(child);
        }
    }

    private static void ScaleDirectChildrenPositions(Transform parent, float multiplier)
    {
        if (parent == null)
            return;

        for (var i = 0; i < parent.childCount; i++)
            ScaleTransformPosition(parent.GetChild(i), multiplier);
    }

    private static void ScaleMapPathSystem(float multiplier)
    {
        var pathRoot = GameObject.Find(PathRootName);
        if (pathRoot == null)
            return;

        foreach (var path in pathRoot.GetComponentsInChildren<MapPath>(true))
            ScaleDirectChildrenPositions(path.transform, multiplier);
    }

    private static void ScaleNamedObjectPosition(string objectName, float multiplier)
    {
        var target = GameObject.Find(objectName);
        if (target == null)
            return;

        ScaleTransformPosition(target.transform, multiplier);
    }

    private static void ScaleWorldMapPlayer(float multiplier)
    {
        var player = GameObject.Find(WorldMapPlayerName);
        if (player == null)
            return;

        ScaleTransformPosition(player.transform, multiplier);
        Undo.RecordObject(player.transform, "Scale World Map Player");
        player.transform.localScale *= multiplier;
        EditorUtility.SetDirty(player.transform);

        var visual = player.transform.Find("Player Visual");
        if (visual != null)
        {
            Undo.RecordObject(visual, "Normalize World Map Player Visual");
            visual.localScale = Vector3.one;
            EditorUtility.SetDirty(visual);
        }
    }

    private static void ScaleTransformPosition(Transform target, float multiplier)
    {
        Undo.RecordObject(target, "Scale World Map Position");
        target.position = ScalePosition(target.position, multiplier);
        EditorUtility.SetDirty(target);
    }

    private static Vector3 ScalePosition(Vector3 position, float multiplier)
    {
        return new Vector3(position.x * multiplier, position.y * multiplier, position.z * multiplier);
    }

    private static void BuildSingleConnectorBridge(Transform parent, int locationCount)
    {
        if (locationCount < 2)
            return;

        var start = GetGridPosition(0, locationCount);
        var end = GetGridPosition(1, locationCount);
        var direction = (end - start).normalized;
        var bridgePosition = Vector3.Lerp(start, end, 0.5f);
        bridgePosition.y = 0.05f;

        var bridge = InstantiateMapPrefab($"{MapPrefabRoot}/Environment/wooden bridge.prefab", "Connector Bridge Reference", bridgePosition, Quaternion.LookRotation(direction, Vector3.up), parent);
        FitObjectToXZSize(bridge, 2.4f);
        SnapObjectToGround(bridge);
    }

    private static GameObject InstantiateMapPrefab(string prefabPath, string name, Vector3 position, Quaternion rotation, Transform parent)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        instance.name = name;
        instance.transform.SetParent(parent);
        instance.transform.SetPositionAndRotation(position, rotation);
        return instance;
    }

    private static void CreateWaypoint(Transform parent, string name, Vector3 position)
    {
        var waypoint = new GameObject(name);
        waypoint.transform.SetParent(parent);
        waypoint.transform.position = position;
    }

    private static void CreateCameraAndLight(Transform root, int locationCount)
    {
        var rows = Mathf.Max(1, Mathf.CeilToInt(locationCount / (float)Columns));
        var cameraObject = new GameObject("World Map Camera", typeof(Camera));
        cameraObject.transform.SetParent(root);
        cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 23f, -26f), Quaternion.Euler(58f, 0f, 0f));
        var camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.46f, 0.68f, 0.84f);
        camera.orthographic = true;
        camera.orthographicSize = Mathf.Max(15.5f, rows * 7.2f);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 140f;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.78f, 0.8f, 0.72f);
        RenderSettings.fog = false;

        var lightObject = new GameObject("World Map Sun", typeof(Light));
        lightObject.transform.SetParent(root);
        lightObject.transform.rotation = Quaternion.Euler(48f, -38f, 0f);
        var light = lightObject.GetComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.12f;
        light.shadows = LightShadows.None;
    }

    private static void FitObjectToXZSize(GameObject target, float targetSize)
    {
        var bounds = CalculateBounds(target);
        var maxXZ = Mathf.Max(bounds.size.x, bounds.size.z);
        if (maxXZ <= 0.001f)
            return;

        var scale = targetSize / maxXZ;
        target.transform.localScale *= scale;
    }

    private static void SnapObjectToGround(GameObject target)
    {
        var bounds = CalculateBounds(target);
        var position = target.transform.position;
        position.y -= bounds.min.y;
        target.transform.position = position;
    }

    private static Bounds CalculateBounds(GameObject target)
    {
        var renderers = target.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(target.transform.position, Vector3.one);

        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds;
    }
}
