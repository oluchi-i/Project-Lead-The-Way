using System;
using UnityEngine;

public class BoardObject : MonoBehaviour
{
    [SerializeField] private string objectId;
    [SerializeField] private BoardObjectType objectType = BoardObjectType.Other;
    [SerializeField] private Vector2Int tilePosition;
    [SerializeField] private bool occupiesTile = true;
    [SerializeField] private bool blocksMovement;
    [SerializeField] private bool movable;

    public string ObjectId => objectId;
    public BoardObjectType ObjectType => objectType;
    public Vector2Int TilePosition => tilePosition;
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

    public void SetTilePosition(Vector2Int tile)
    {
        tilePosition = tile;
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
}
