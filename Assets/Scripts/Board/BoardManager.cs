using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

#pragma warning disable 0649 // Unity assigns serialized fields from scene objects.

public class BoardManager : MonoBehaviour
{
    [SerializeField] private Vector2Int boardSize = new Vector2Int(10, 10);
    [SerializeField] private Vector3 worldOrigin;
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private bool showCoordinateLabels = true;
    [SerializeField] private bool registerOnAwake = true;
    [SerializeField] private bool syncTilesFromTransformsOnAwake = true;

    private readonly Dictionary<string, BoardObject> objectsById = new Dictionary<string, BoardObject>();
    private readonly Dictionary<Vector2Int, List<BoardObject>> objectsByTile = new Dictionary<Vector2Int, List<BoardObject>>();

    public Vector2Int BoardSize => boardSize;
    public Vector3 WorldOrigin => worldOrigin;
    public float TileSize => Mathf.Max(0.01f, tileSize);

    private void Awake()
    {
        if (syncTilesFromTransformsOnAwake)
            SyncSceneObjectTilesFromTransforms();

        if (registerOnAwake)
            RebuildRegistry();
    }

    private void SyncSceneObjectTilesFromTransforms()
    {
        var boardObjects = FindSceneBoardObjects();
        foreach (var boardObject in boardObjects)
        {
            if (boardObject == null)
                continue;

            boardObject.SyncTileFromTransform(worldOrigin, TileSize);
        }
    }

    public void RebuildRegistry()
    {
        objectsById.Clear();
        objectsByTile.Clear();

        var boardObjects = FindSceneBoardObjects();
        foreach (var boardObject in boardObjects)
            Register(boardObject);
    }

    public static List<BoardObject> FindSceneBoardObjects(bool includeInactive = false)
    {
        var boardObjects = new List<BoardObject>();
        var seen = new HashSet<BoardObject>();
        for (var sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            var scene = SceneManager.GetSceneAt(sceneIndex);
            if (!scene.isLoaded)
                continue;

            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                foreach (var boardObject in root.GetComponentsInChildren<BoardObject>(true))
                    AddBoardObject(boardObjects, seen, boardObject, includeInactive);
            }
        }

        return boardObjects;
    }

    private static void AddBoardObject(List<BoardObject> boardObjects, HashSet<BoardObject> seen, BoardObject boardObject, bool includeInactive)
    {
        if (boardObject == null)
            return;

        if (!includeInactive && !boardObject.gameObject.activeInHierarchy)
            return;

        if (!seen.Add(boardObject))
            return;

        boardObjects.Add(boardObject);
    }

    public bool Register(BoardObject boardObject)
    {
        if (boardObject == null)
            return false;

        boardObject.EnsureObjectId();

        if (objectsById.ContainsKey(boardObject.ObjectId))
        {
            Debug.LogWarning($"BoardManager found duplicate BoardObject id '{boardObject.ObjectId}' on {boardObject.name}. Tile occupancy will still be registered.", boardObject);
        }
        else
        {
            objectsById.Add(boardObject.ObjectId, boardObject);
        }

        foreach (var occupiedTile in boardObject.GetOccupiedTiles())
        {
            AddToTile(boardObject, occupiedTile);
        }

        return true;
    }

    public bool TryGetObject(string objectId, out BoardObject boardObject)
    {
        return objectsById.TryGetValue(objectId, out boardObject);
    }

    public List<BoardObject> GetObjectsAt(Vector2Int tile)
    {
        if (!objectsByTile.TryGetValue(tile, out var tileObjects))
            return new List<BoardObject>();

        return new List<BoardObject>(tileObjects);
    }

    public bool IsInsideBounds(Vector2Int tile)
    {
        return tile.x >= 0
            && tile.y >= 0
            && tile.x < boardSize.x
            && tile.y < boardSize.y;
    }

    public bool IsBlocked(Vector2Int tile)
    {
        return IsBlocked(tile, null);
    }

    public bool IsBlocked(Vector2Int tile, BoardObject ignoredObject)
    {
        if (!IsInsideBounds(tile))
            return true;

        return IsBlockedByObjects(tile, ignoredObject);
    }

    public bool CanEnterTile(BoardObject movingObject, Vector2Int tile)
    {
        if (movingObject == null)
            return false;

        foreach (var occupiedTile in movingObject.GetOccupiedTiles(tile))
        {
            if (!CanEnterSingleTile(movingObject, occupiedTile))
                return false;
        }

        return true;
    }

    private bool CanEnterSingleTile(BoardObject movingObject, Vector2Int tile)
    {
        if (IsInsideBounds(tile))
            return !IsBlockedByObjects(tile, movingObject);

        if (movingObject.ObjectType == BoardObjectType.Player && IsGoalTile(tile))
            return !IsBlockedByObjects(tile, movingObject, true);

        return false;
    }

    public bool IsGoalTile(Vector2Int tile)
    {
        if (!objectsByTile.TryGetValue(tile, out var tileObjects))
            return false;

        foreach (var boardObject in tileObjects)
        {
            if (boardObject != null && boardObject.ObjectType == BoardObjectType.Goal)
                return true;
        }

        return false;
    }

    public List<BoardObject> GetGoalObjects()
    {
        var goals = new List<BoardObject>();
        foreach (var boardObject in objectsById.Values)
        {
            if (boardObject != null && boardObject.ObjectType == BoardObjectType.Goal && boardObject.gameObject.activeInHierarchy)
                goals.Add(boardObject);
        }

        return goals;
    }

    private bool IsBlockedByObjects(Vector2Int tile, BoardObject ignoredObject)
    {
        return IsBlockedByObjects(tile, ignoredObject, false);
    }

    private bool IsBlockedByObjects(Vector2Int tile, BoardObject ignoredObject, bool allowPlayerGoalEntry)
    {
        if (!objectsByTile.TryGetValue(tile, out var tileObjects))
            return false;

        foreach (var boardObject in tileObjects)
        {
            if (boardObject == null || boardObject == ignoredObject)
                continue;

            if (allowPlayerGoalEntry && (boardObject.ObjectType == BoardObjectType.Goal || boardObject.ObjectType == BoardObjectType.Door))
                continue;

            if (boardObject.BlocksMovement)
                return true;
        }

        return false;
    }

    public bool TryMoveObject(BoardObject boardObject, Vector2Int direction, out Vector2Int fromTile, out Vector2Int toTile)
    {
        fromTile = boardObject != null ? boardObject.TilePosition : default;
        toTile = fromTile + direction;

        if (boardObject == null || !boardObject.Movable)
            return false;

        RebuildRegistry();

        fromTile = boardObject.TilePosition;
        toTile = fromTile + direction;

        if (!CanEnterTile(boardObject, toTile))
            return false;

        foreach (var occupiedTile in boardObject.GetOccupiedTiles(fromTile))
            RemoveFromTile(boardObject, occupiedTile);

        boardObject.SetTilePosition(toTile);

        foreach (var occupiedTile in boardObject.GetOccupiedTiles(toTile))
            AddToTile(boardObject, occupiedTile);

        return true;
    }

    private void RemoveFromTile(BoardObject boardObject, Vector2Int tile)
    {
        if (boardObject == null || !objectsByTile.TryGetValue(tile, out var tileObjects))
            return;

        tileObjects.Remove(boardObject);
        if (tileObjects.Count == 0)
            objectsByTile.Remove(tile);
    }

    private void AddToTile(BoardObject boardObject, Vector2Int tile)
    {
        if (boardObject == null || !boardObject.OccupiesTile)
            return;

        if (!objectsByTile.TryGetValue(tile, out var tileObjects))
        {
            tileObjects = new List<BoardObject>();
            objectsByTile.Add(tile, tileObjects);
        }

        if (!tileObjects.Contains(boardObject))
            tileObjects.Add(boardObject);
    }

    public Vector2Int WorldToTile(Vector3 worldPosition)
    {
        var localPosition = worldPosition - worldOrigin;
        return new Vector2Int(
            Mathf.RoundToInt(localPosition.x / TileSize),
            Mathf.RoundToInt(localPosition.z / TileSize));
    }

    public Vector3 TileToWorld(Vector2Int tile, float y)
    {
        return new Vector3(
            worldOrigin.x + tile.x * TileSize,
            y,
            worldOrigin.z + tile.y * TileSize);
    }

    private void OnDrawGizmosSelected()
    {
        var size = new Vector3(boardSize.x * TileSize, 0f, boardSize.y * TileSize);
        var center = worldOrigin + new Vector3(size.x - TileSize, 0f, size.z - TileSize) * 0.5f;

        Gizmos.color = new Color(1f, 0.72f, 0.15f, 0.6f);
        Gizmos.DrawWireCube(center, size);

        Gizmos.color = new Color(1f, 0.72f, 0.15f, 0.18f);
        for (var x = 0; x < boardSize.x; x++)
        {
            for (var y = 0; y < boardSize.y; y++)
            {
                var tileCenter = TileToWorld(new Vector2Int(x, y), worldOrigin.y);
                Gizmos.DrawWireCube(tileCenter, new Vector3(TileSize, 0.02f, TileSize));
#if UNITY_EDITOR
                if (showCoordinateLabels)
                    DrawCoordinateLabel(new Vector2Int(x, y), tileCenter);
#endif
            }
        }
    }

#if UNITY_EDITOR
    private void DrawCoordinateLabel(Vector2Int tile, Vector3 tileCenter)
    {
        var labelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 10
        };

        labelStyle.normal.textColor = new Color(1f, 0.86f, 0.42f, 0.95f);

        var labelPosition = tileCenter + Vector3.up * 0.08f;
        Handles.Label(labelPosition, $"{tile.x} , {tile.y}", labelStyle);
    }
#endif
}

#pragma warning restore 0649
