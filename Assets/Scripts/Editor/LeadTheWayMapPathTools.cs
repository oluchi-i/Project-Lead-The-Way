using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LeadTheWayMapPathTools
{
    private const string RootName = "World Map Path System";

    [MenuItem("Tools/Lead The Way/Map Path/Create Camera And Player Path Setup")]
    public static void CreateCameraAndPlayerPathSetup()
    {
        var root = GameObject.Find(RootName);
        if (root == null)
        {
            root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Create World Map Path System");
        }

        var cameraPath = EnsurePath(root.transform, "Camera Path", new Vector3(-4f, 6f, -8f), new Vector3(0f, 7f, -6f), new Vector3(4f, 6f, -8f));
        var playerPath = EnsurePath(root.transform, "Player Path", new Vector3(-4f, 0f, 0f), new Vector3(0f, 0f, 1.4f), new Vector3(4f, 0f, 0f));

        Selection.objects = new Object[] { cameraPath.gameObject, playerPath.gameObject };
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorUtility.DisplayDialog(
            "Map Path Setup",
            "Created Camera Path and Player Path with 2 invisible checkpoints and 1 invisible control point each.",
            "OK");
    }

    [MenuItem("Tools/Lead The Way/Map Path/Add Checkpoint To Selected Path")]
    public static void AddCheckpointToSelectedPath()
    {
        var path = GetSelectedPath();
        if (path == null)
            return;

        var checkpointCount = path.CheckpointCount;
        var position = path.transform.childCount > 0
            ? path.transform.GetChild(path.transform.childCount - 1).position + Vector3.right * 2f
            : path.transform.position;

        var checkpoint = CreateCheckpoint(path.transform, checkpointCount + 1, position);
        Selection.activeGameObject = checkpoint.gameObject;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    [MenuItem("Tools/Lead The Way/Map Path/Add Control Point After Selected")]
    public static void AddControlPointAfterSelected()
    {
        var selected = Selection.activeTransform;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("Map Path", "Select a checkpoint or control point first.", "OK");
            return;
        }

        var parentPath = selected.GetComponentInParent<MapPath>();
        if (parentPath == null || selected == parentPath.transform)
        {
            EditorUtility.DisplayDialog("Map Path", "Select a child checkpoint/control point under a MapPath.", "OK");
            return;
        }

        var insertIndex = selected.GetSiblingIndex() + 1;
        var position = selected.position + Vector3.forward;
        var controlPoint = CreateControlPoint(parentPath.transform, "Control Point", position);
        controlPoint.transform.SetSiblingIndex(insertIndex);

        RenameControlPoints(parentPath.transform);
        Selection.activeGameObject = controlPoint.gameObject;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    [MenuItem("Tools/Lead The Way/Map Path/Add Checkpoint To Selected Path", true)]
    private static bool CanAddCheckpointToSelectedPath()
    {
        return GetSelectedPath(false) != null;
    }

    [MenuItem("Tools/Lead The Way/Map Path/Add Control Point After Selected", true)]
    private static bool CanAddControlPointAfterSelected()
    {
        var selected = Selection.activeTransform;
        return selected != null && selected.GetComponentInParent<MapPath>() != null && selected.GetComponent<MapPath>() == null;
    }

    private static MapPath GetSelectedPath(bool showDialog = true)
    {
        var path = Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponent<MapPath>()
            : null;

        if (path == null && showDialog)
            EditorUtility.DisplayDialog("Map Path", "Select a GameObject with a MapPath component.", "OK");

        return path;
    }

    private static MapPath EnsurePath(Transform root, string pathName, Vector3 checkpointA, Vector3 controlPoint, Vector3 checkpointB)
    {
        var pathTransform = root.Find(pathName);
        GameObject pathObject;
        if (pathTransform == null)
        {
            pathObject = new GameObject(pathName);
            Undo.RegisterCreatedObjectUndo(pathObject, "Create " + pathName);
            pathObject.transform.SetParent(root);
        }
        else
        {
            pathObject = pathTransform.gameObject;
        }

        var path = pathObject.GetComponent<MapPath>();
        if (path == null)
            path = Undo.AddComponent<MapPath>(pathObject);

        EnsurePoint(pathObject.transform, "Checkpoint 01", checkpointA, true, "checkpoint-01");
        EnsurePoint(pathObject.transform, "Control Point 01A", controlPoint, false, "control-point-01a");
        EnsurePoint(pathObject.transform, "Checkpoint 02", checkpointB, true, "checkpoint-02");
        return path;
    }

    private static void EnsurePoint(Transform parent, string name, Vector3 position, bool checkpoint, string id)
    {
        var child = parent.Find(name);
        GameObject pointObject;
        if (child == null)
        {
            pointObject = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(pointObject, "Create " + name);
            pointObject.transform.SetParent(parent);
        }
        else
        {
            pointObject = child.gameObject;
        }

        pointObject.transform.position = position;
        RemoveVisualComponents(pointObject);

        if (checkpoint)
        {
            var component = pointObject.GetComponent<MapCheckpoint>();
            if (component == null)
                component = Undo.AddComponent<MapCheckpoint>(pointObject);

            component.Configure(id);
            var control = pointObject.GetComponent<MapPathControlPoint>();
            if (control != null)
                Undo.DestroyObjectImmediate(control);
        }
        else
        {
            var component = pointObject.GetComponent<MapPathControlPoint>();
            if (component == null)
                component = Undo.AddComponent<MapPathControlPoint>(pointObject);

            component.Configure(id);
            var mapCheckpoint = pointObject.GetComponent<MapCheckpoint>();
            if (mapCheckpoint != null)
                Undo.DestroyObjectImmediate(mapCheckpoint);
        }

        EditorUtility.SetDirty(pointObject);
    }

    private static MapCheckpoint CreateCheckpoint(Transform parent, int number, Vector3 position)
    {
        var pointObject = new GameObject("Checkpoint " + number.ToString("00"));
        Undo.RegisterCreatedObjectUndo(pointObject, "Create Map Checkpoint");
        pointObject.transform.SetParent(parent);
        pointObject.transform.position = position;
        var checkpoint = Undo.AddComponent<MapCheckpoint>(pointObject);
        checkpoint.Configure("checkpoint-" + number.ToString("00"));
        return checkpoint;
    }

    private static MapPathControlPoint CreateControlPoint(Transform parent, string name, Vector3 position)
    {
        var pointObject = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(pointObject, "Create Map Control Point");
        pointObject.transform.SetParent(parent);
        pointObject.transform.position = position;
        var controlPoint = Undo.AddComponent<MapPathControlPoint>(pointObject);
        controlPoint.Configure("control-point");
        return controlPoint;
    }

    private static void RenameControlPoints(Transform path)
    {
        var controlIndex = 1;
        for (var i = 0; i < path.childCount; i++)
        {
            var child = path.GetChild(i);
            var controlPoint = child.GetComponent<MapPathControlPoint>();
            if (controlPoint == null)
                continue;

            var name = "Control Point " + controlIndex.ToString("00");
            child.name = name;
            controlPoint.Configure("control-point-" + controlIndex.ToString("00"));
            EditorUtility.SetDirty(controlPoint);
            controlIndex++;
        }
    }

    private static void RemoveVisualComponents(GameObject pointObject)
    {
        foreach (var renderer in pointObject.GetComponents<Renderer>())
            Undo.DestroyObjectImmediate(renderer);
        foreach (var filter in pointObject.GetComponents<MeshFilter>())
            Undo.DestroyObjectImmediate(filter);
        foreach (var collider in pointObject.GetComponents<Collider>())
            Undo.DestroyObjectImmediate(collider);
    }
}
