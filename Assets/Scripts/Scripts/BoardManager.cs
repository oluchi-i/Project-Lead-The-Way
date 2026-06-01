using System.Collections.Generic;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
//  BoardManager.cs
//  Bridges the GridManager cell state with the BoardObject component system.
//  Provides TryMoveObject (ice-slide mechanic) and TileToWorld used by
//  BoardPlayerMover and GridTileMover.
//
//  Place on any persistent GameObject (e.g. the GameManager GO).
// ─────────────────────────────────────────────────────────────────────────────

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────

    [Header("Grid")]
    [SerializeField] private Vector3 worldOrigin = Vector3.zero;
    [SerializeField] private float   tileSize    = 1f;

    /// <summary>World-space origin of tile (0,0). Used by LeadTheWayObjectSetupTools.</summary>
    public Vector3 WorldOrigin => worldOrigin;

    /// <summary>World units per tile. Used by LeadTheWayObjectSetupTools.</summary>
    public float TileSize => Mathf.Max(0.01f, tileSize);

    // ── Object registry ───────────────────────────────────────────────────

    // Static registry — BoardObject calls Register/Unregister via OnEnable/OnDisable
    private static readonly List<BoardObject> _objects = new List<BoardObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public static void Register(BoardObject obj)
    {
        if (obj != null && !_objects.Contains(obj))
            _objects.Add(obj);
    }

    public static void Unregister(BoardObject obj)
    {
        _objects.Remove(obj);
    }

    /// <summary>Returns all active BoardObjects — used by SelectionPanelsUI.</summary>
    public static IEnumerable<BoardObject> FindSceneBoardObjects()
    {
        return _objects;
    }

    /// <summary>
    /// Re-scans the scene and rebuilds the static registry from all active BoardObjects.
    /// Called by LeadTheWayObjectSetupTools after bulk setup operations so the registry
    /// reflects the current scene state without requiring Play mode.
    /// </summary>
    public void RebuildRegistry()
    {
        _objects.Clear();

#if UNITY_EDITOR
        // In the editor we scan all BoardObjects in the scene, including those
        // that may not have fired OnEnable yet (e.g. prefabs just added).
        var all = UnityEngine.Object.FindObjectsByType<BoardObject>(
            UnityEngine.FindObjectsInactive.Exclude,
            UnityEngine.FindObjectsSortMode.None);
        foreach (var obj in all)
            if (obj != null && !_objects.Contains(obj))
                _objects.Add(obj);
#endif
    }

    // ── Movement ───────────────────────────────────────────────────────────

    /// <summary>
    /// Ice-slide mechanic: moves obj in direction until hitting a wall or
    /// another obstacle.  Updates GridManager registries.
    /// Returns false if the object cannot move at all.
    /// </summary>
    public bool TryMoveObject(BoardObject obj, Vector2Int direction,
                              out Vector2Int fromTile, out Vector2Int toTile)
    {
        fromTile = obj.GridPosition;
        toTile   = fromTile;

        if (GridManager.Instance == null) return false;

        Vector2Int current = fromTile;
        Vector2Int next    = current + direction;

        // Can't move even one step?
        if (GridManager.Instance.IsWall(next) || GridManager.Instance.HasObstacle(next))
            return false;

        // Slide until blocked
        while (!GridManager.Instance.IsWall(next) && !GridManager.Instance.HasObstacle(next))
        {
            current = next;
            next    = current + direction;
        }

        if (current == fromTile) return false;

        toTile = current;
        obj.SetGridPosition(toTile);

        // Keep GridManager obstacle registry in sync
        ObstacleBlock block = obj.GetComponent<ObstacleBlock>();
        if (block != null)
        {
            GridManager.Instance.UnregisterObstacle(fromTile);
            GridManager.Instance.RegisterObstacle(toTile, block);
        }

        return true;
    }

    // ── Coordinate helpers ────────────────────────────────────────────────

    /// <summary>
    /// Converts a grid tile to world position.
    /// y is passed through so callers can preserve the object's current height.
    /// Falls back to the inspector-configured worldOrigin + tileSize when
    /// GridManager is not present (edit-time safe).
    /// </summary>
    public Vector3 TileToWorld(Vector2Int tile, float y = 0f)
    {
        if (GridManager.Instance != null)
        {
            Vector3 w = GridManager.Instance.GridToWorld(tile);
            w.y = y;
            return w;
        }

        // Fallback: use own origin/tileSize so the method works in edit-time tools
        return new Vector3(
            worldOrigin.x + tile.x * TileSize,
            y,
            worldOrigin.z + tile.y * TileSize);
    }
}
