using System.Collections.Generic;
using UnityEngine;

public sealed class MapPath : MonoBehaviour
{
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color pathColor = new Color(0.2f, 0.75f, 1f, 1f);
    [SerializeField] private Color checkpointColor = new Color(1f, 0.82f, 0.2f, 1f);
    [SerializeField] private Color controlPointColor = new Color(0.55f, 0.35f, 1f, 1f);
    [SerializeField, Min(0.01f)] private float pointGizmoRadius = 0.04f;
    [SerializeField, Min(4)] private int gizmoSamplesPerSegment = 16;

    public int CheckpointCount
    {
        get
        {
            var count = 0;
            for (var i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i).GetComponent<MapCheckpoint>() != null)
                    count++;
            }

            return count;
        }
    }

    public bool TryGetCheckpointLabel(int checkpointIndex, out string label)
    {
        if (TryGetCheckpoint(checkpointIndex, out var checkpoint))
        {
            label = string.IsNullOrWhiteSpace(checkpoint.DisplayName)
                ? GetDefaultCheckpointLabel(checkpointIndex)
                : checkpoint.DisplayName;
            return true;
        }

        label = string.Empty;
        return false;
    }

    public bool TryGetCheckpointLevelSceneName(int checkpointIndex, out string sceneName)
    {
        if (TryGetCheckpoint(checkpointIndex, out var checkpoint))
        {
            sceneName = checkpoint.ResolvedLevelSceneName;
            return !string.IsNullOrWhiteSpace(sceneName);
        }

        sceneName = string.Empty;
        return false;
    }

    public bool TryGetCheckpoint(int checkpointIndex, out MapCheckpoint checkpoint)
    {
        var seenCheckpoints = 0;
        for (var i = 0; i < transform.childCount; i++)
        {
            checkpoint = transform.GetChild(i).GetComponent<MapCheckpoint>();
            if (checkpoint == null)
                continue;

            if (seenCheckpoints == checkpointIndex)
                return true;

            seenCheckpoints++;
        }

        checkpoint = null;
        return false;
    }

    private static string GetDefaultCheckpointLabel(int checkpointIndex)
    {
        return checkpointIndex == 0 ? "Start" : "Level " + checkpointIndex;
    }

    public Vector3 GetPoint(float t)
    {
        var points = GetOrderedPointPositions();
        return Evaluate(points, t);
    }

    public Vector3 GetDirection(float t)
    {
        var previous = GetPoint(Mathf.Clamp01(t - 0.0025f));
        var next = GetPoint(Mathf.Clamp01(t + 0.0025f));
        var direction = next - previous;
        return direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
    }

    public bool TryGetCheckpointProgress(int checkpointIndex, out float progress)
    {
        var orderedNodes = GetOrderedNodes();
        var pathPointIndex = -1;
        var seenCheckpoints = 0;

        for (var i = 0; i < orderedNodes.Count; i++)
        {
            if (!orderedNodes[i].IsCheckpoint)
                continue;

            if (seenCheckpoints == checkpointIndex)
            {
                pathPointIndex = i;
                break;
            }

            seenCheckpoints++;
        }

        if (pathPointIndex < 0 || orderedNodes.Count <= 1)
        {
            progress = 0f;
            return false;
        }

        progress = pathPointIndex / (float)(orderedNodes.Count - 1);
        return true;
    }

    private List<Vector3> GetOrderedPointPositions()
    {
        var nodes = GetOrderedNodes();
        var points = new List<Vector3>(nodes.Count);
        foreach (var node in nodes)
            points.Add(node.Transform.position);

        return points;
    }

    private List<PathNode> GetOrderedNodes()
    {
        var nodes = new List<PathNode>();
        for (var i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            if (child.GetComponent<MapCheckpoint>() != null)
            {
                nodes.Add(new PathNode(child, true));
                continue;
            }

            if (child.GetComponent<MapPathControlPoint>() != null)
                nodes.Add(new PathNode(child, false));
        }

        return nodes;
    }

    private static Vector3 Evaluate(IReadOnlyList<Vector3> points, float t)
    {
        if (points.Count == 0)
            return Vector3.zero;
        if (points.Count == 1)
            return points[0];

        t = Mathf.Clamp01(t);
        var segmentCount = points.Count - 1;
        var scaledT = t * segmentCount;
        var segment = Mathf.Min(Mathf.FloorToInt(scaledT), segmentCount - 1);
        var localT = scaledT - segment;

        var p0 = points[Mathf.Max(segment - 1, 0)];
        var p1 = points[segment];
        var p2 = points[segment + 1];
        var p3 = points[Mathf.Min(segment + 2, points.Count - 1)];

        return CatmullRom(p0, p1, p2, p3, localT);
    }

    private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        var t2 = t * t;
        var t3 = t2 * t;
        return 0.5f * ((2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos)
            return;

        var nodes = GetOrderedNodes();
        if (nodes.Count == 0)
            return;

        for (var i = 0; i < nodes.Count; i++)
        {
            Gizmos.color = nodes[i].IsCheckpoint ? checkpointColor : controlPointColor;
            Gizmos.DrawSphere(nodes[i].Transform.position, pointGizmoRadius);
        }

        if (nodes.Count < 2)
            return;

        Gizmos.color = pathColor;
        var sampleCount = Mathf.Max(8, (nodes.Count - 1) * gizmoSamplesPerSegment);
        var previous = GetPoint(0f);
        for (var i = 1; i <= sampleCount; i++)
        {
            var current = GetPoint(i / (float)sampleCount);
            Gizmos.DrawLine(previous, current);
            previous = current;
        }
    }

    private readonly struct PathNode
    {
        public readonly Transform Transform;
        public readonly bool IsCheckpoint;

        public PathNode(Transform transform, bool isCheckpoint)
        {
            Transform = transform;
            IsCheckpoint = isCheckpoint;
        }
    }
}
