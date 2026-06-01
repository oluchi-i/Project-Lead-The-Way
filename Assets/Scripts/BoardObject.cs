using System;
using System.Collections.Generic;
using UnityEngine;

public class BoardObject : MonoBehaviour
{
    [SerializeField] private string objectId;
    [SerializeField] private BoardObjectType objectType = BoardObjectType.Decoration;
    [SerializeField] private Vector2Int tilePosition;
    [SerializeField] private List<Vector2Int> occupiedTileOffsets = new List<Vector2Int> { Vector2Int.zero };
    [SerializeField] private bool occupiesTile = true;
    [SerializeField] private bool blocksMovement;
    [SerializeField] private bool movable;

    public string ObjectId => objectId;
    public BoardObjectType ObjectType => objectType;
    public Vector2Int TilePosition => tilePosition;
    public IReadOnlyList<Vector2Int> OccupiedTileOffsets => occupiedTileOffsets;
    public bool OccupiesTile => occupiesTile;
    public bool BlocksMovement => blocksMovement;
    public bool Movable => movable;

    private void Reset()
    {
        EnsureObjectId();
        SyncTileFromTransform();
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(objectId))
            objectId = CreateObjectId(gameObject.name);

        EnsureDefaultFootprint();
    }

    public void Configure(BoardObjectType type, bool occupies, bool blocks, bool canMove)
    {
        EnsureObjectId();
        objectType = type;
        occupiesTile = occupies;
        blocksMovement = blocks;
        movable = canMove;
    }

    public void EnsureObjectId()
    {
        if (!string.IsNullOrWhiteSpace(objectId))
            return;

        objectId = CreateObjectId(gameObject.name);
    }

    public bool EnsureUniqueObjectId(HashSet<string> usedObjectIds)
    {
        if (usedObjectIds == null)
            return false;

        var changed = false;
        if (string.IsNullOrWhiteSpace(objectId) || usedObjectIds.Contains(objectId))
        {
            objectId = CreateObjectId(gameObject.name);
            changed = true;
        }

        usedObjectIds.Add(objectId);
        return changed;
    }

    public void SetTilePosition(Vector2Int tile)
    {
        tilePosition = tile;
    }

    public List<Vector2Int> GetOccupiedTiles()
    {
        return GetOccupiedTiles(tilePosition);
    }

    public List<Vector2Int> GetOccupiedTiles(Vector2Int anchorTile)
    {
        var occupiedTiles = new List<Vector2Int>();
        if (!occupiesTile)
            return occupiedTiles;

        if (occupiedTileOffsets == null || occupiedTileOffsets.Count == 0)
        {
            occupiedTiles.Add(anchorTile);
            return occupiedTiles;
        }

        foreach (var offset in occupiedTileOffsets)
        {
            var tile = anchorTile + offset;
            if (!occupiedTiles.Contains(tile))
                occupiedTiles.Add(tile);
        }

        return occupiedTiles;
    }

    public void SyncTileFromTransform()
    {
        SyncTileFromTransform(Vector3.zero, 1f);
    }

    public void SyncTileFromTransform(Vector3 worldOrigin, float tileSize)
    {
        var safeTileSize = Mathf.Max(0.01f, tileSize);
        var localPosition = transform.position - worldOrigin;
        tilePosition = new Vector2Int(
            Mathf.RoundToInt(localPosition.x / safeTileSize),
            Mathf.RoundToInt(localPosition.z / safeTileSize));
    }

    public static string CreateObjectId(string objectName)
    {
        var prefix = string.IsNullOrWhiteSpace(objectName) ? "object" : objectName.Trim().ToLowerInvariant();
        prefix = prefix.Replace(" ", "-");
        return $"{prefix}-{Guid.NewGuid():N}".Substring(0, Mathf.Min(prefix.Length + 9, prefix.Length + 33));
    }

    private void EnsureDefaultFootprint()
    {
        if (occupiedTileOffsets == null)
            occupiedTileOffsets = new List<Vector2Int>();

        if (occupiedTileOffsets.Count == 0)
            occupiedTileOffsets.Add(Vector2Int.zero);
    }
}
