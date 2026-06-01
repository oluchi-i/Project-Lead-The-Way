using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LeadTheWayPalmovMapPrototypeTools
{
    private const string ScenePath = "Assets/Scenes/WorldMapPrototype.unity";
    private const string PackRoot = "Assets/Palmov Island/Low Poly Atmospheric Locations Pack/Prefabs";
    private const string LocationFolder = PackRoot + "/Location with environment";
    private const int Columns = 5;
    private const float SpacingX = 8.2f;
    private const float SpacingZ = 7.2f;
    private const float LocationTargetSize = 6.25f;

    [MenuItem("Tools/Lead The Way/Map/Generate Palmov World Map Prototype")]
    public static void GeneratePalmovWorldMapPrototype()
    {
        var locationPrefabPaths = GetLocationPrefabPaths();
        var missingPrefabs = GetMissingPrefabs(locationPrefabPaths);
        if (missingPrefabs.Count > 0)
        {
            EditorUtility.DisplayDialog(
                "Generate Palmov World Map Prototype",
                "Missing required Palmov prefab(s):\n\n" + string.Join("\n", missingPrefabs),
                "OK");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "WorldMapPrototype";

        var root = new GameObject("Palmov All-Locations World Map Prototype");
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
            var location = InstantiatePalmovPrefab(path, "Location " + (i + 1).ToString("00") + " - " + name, position, Quaternion.identity, locationsRoot.transform);
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
            "Generate Palmov World Map Prototype",
            $"Generated {locationPrefabPaths.Count} authored location prefab(s) in {ScenePath}.",
            "OK");
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

        var bridgePath = $"{PackRoot}/Environment/wooden bridge.prefab";
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

    private static void BuildSingleConnectorBridge(Transform parent, int locationCount)
    {
        if (locationCount < 2)
            return;

        var start = GetGridPosition(0, locationCount);
        var end = GetGridPosition(1, locationCount);
        var direction = (end - start).normalized;
        var bridgePosition = Vector3.Lerp(start, end, 0.5f);
        bridgePosition.y = 0.05f;

        var bridge = InstantiatePalmovPrefab($"{PackRoot}/Environment/wooden bridge.prefab", "Connector Bridge Reference", bridgePosition, Quaternion.LookRotation(direction, Vector3.up), parent);
        FitObjectToXZSize(bridge, 2.4f);
        SnapObjectToGround(bridge);
    }

    private static GameObject InstantiatePalmovPrefab(string prefabPath, string name, Vector3 position, Quaternion rotation, Transform parent)
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
