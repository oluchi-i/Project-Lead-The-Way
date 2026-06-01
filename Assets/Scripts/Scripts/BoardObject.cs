using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
//  BoardObject.cs
//  Marks a GameObject as living on the board grid.
//  Auto-registers with BoardManager on enable/disable.
//
//  Add to: obstacle prefabs, the character prefab.
// ─────────────────────────────────────────────────────────────────────────────

public class BoardObject : MonoBehaviour
{
    // ── Board metadata ────────────────────────────────────────────────────

    public BoardObjectType ObjectType     { get; private set; } = BoardObjectType.None;
    public bool            OccupiesTile   { get; private set; } = true;
    public bool            BlocksMovement { get; private set; } = true;

    // Named "Movable" to match what LeadTheWayObjectSetupTools reads via
    // boardObject.Movable in the Report tool.
    public bool Movable { get; private set; } = false;

    // ── Grid position ─────────────────────────────────────────────────────

    /// <summary>Current board tile. Matches GridPosition alias used by BoardPlayerMover.</summary>
    public Vector2Int GridPosition  { get; private set; }

    /// <summary>Alias used by LeadTheWayObjectSetupTools (Report) and legacy callers.</summary>
    public Vector2Int TilePosition  => GridPosition;

    private void OnEnable()  { BoardManager.Register(this); }
    private void OnDisable() { BoardManager.Unregister(this); }

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>Called by BoardManager and IceCreamLevelBuilder to set position.</summary>
    public void SetGridPosition(Vector2Int pos) { GridPosition = pos; }

    /// <summary>
    /// Called by LeadTheWayObjectSetupTools and runtime builders to set board metadata.
    /// </summary>
    public void Configure(BoardObjectType type, bool occupiesTile, bool blocksMovement, bool movable)
    {
        ObjectType     = type;
        OccupiesTile   = occupiesTile;
        BlocksMovement = blocksMovement;
        Movable        = movable;
    }

    /// <summary>
    /// Derives the grid tile from this object's world position.
    /// Used by LeadTheWayObjectSetupTools after placing objects in the scene.
    /// </summary>
    public void SyncTileFromTransform(Vector3 worldOrigin, float tileSize)
    {
        if (tileSize <= 0f)
            tileSize = 1f;

        var local = transform.position - worldOrigin;
        var tile  = new Vector2Int(
            Mathf.RoundToInt(local.x / tileSize),
            Mathf.RoundToInt(local.z / tileSize));

        GridPosition = tile;
    }
}
