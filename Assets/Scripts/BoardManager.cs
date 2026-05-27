using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BoardManager : MonoBehaviour
{
    [SerializeField] private Vector2Int boardSize = new Vector2Int(10, 10);
    [SerializeField] private Vector3 worldOrigin;
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private bool registerOnAwake = true;

    private readonly Dictionary<string, BoardObject> objectsById = new Dictionary<string, BoardObject>();
    private readonly Dictionary<Vector2Int, List<BoardObject>> objectsByTile = new Dictionary<Vector2Int, List<BoardObject>>();

    public Vector2Int BoardSize => boardSize;
    public Vector3 WorldOrigin => worldOrigin;
    public float TileSize => Mathf.Max(0.01f, tileSize);

    private void Awake()
    {
        if (registerOnAwake)
            RebuildRegistry();
    }

    public void RebuildRegistry()
    {
        objectsById.Clear();
        objectsByTile.Clear();

        var boardObjects = FindSceneBoardObjects();
        foreach (var boardObject in boardObjects)
            Register(boardObject);
    }

    public static List<BoardObject> FindSceneBoardObjects()
    {
        var boardObjects = new List<BoardObject>();
        var seen = new HashSet<int>();
        for (var sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            var scene = SceneManager.GetSceneAt(sceneIndex);
            if (!scene.isLoaded)
                continue;

            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                foreach (var boardObject in root.GetComponentsInChildren<BoardObject>(true))
                    AddBoardObject(boardObjects, seen, boardObject);
            }
        }

        foreach (var boardObject in Resources.FindObjectsOfTypeAll<BoardObject>())
        {
            if (boardObject == null || !boardObject.gameObject.scene.IsValid())
                continue;

            AddBoardObject(boardObjects, seen, boardObject);
        }

        return boardObjects;
    }

    private static void AddBoardObject(List<BoardObject> boardObjects, HashSet<int> seen, BoardObject boardObject)
    {
        if (boardObject == null)
            return;

        var instanceId = boardObject.GetInstanceID();
        if (!seen.Add(instanceId))
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
            Debug.LogWarning($"BoardManager skipped duplicate BoardObject id '{boardObject.ObjectId}' on {boardObject.name}.", boardObject);
            return false;
        }

        objectsById.Add(boardObject.ObjectId, boardObject);

        if (boardObject.OccupiesTile)
        {
            if (!objectsByTile.TryGetValue(boardObject.TilePosition, out var tileObjects))
            {
                tileObjects = new List<BoardObject>();
                objectsByTile.Add(boardObject.TilePosition, tileObjects);
            }

            tileObjects.Add(boardObject);
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
        if (!IsInsideBounds(tile))
            return true;

        if (!objectsByTile.TryGetValue(tile, out var tileObjects))
            return false;

        foreach (var boardObject in tileObjects)
        {
            if (boardObject.BlocksMovement)
                return true;
        }

        return false;
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
            }
        }
    }
}
