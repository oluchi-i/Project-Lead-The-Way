using System.Collections.Generic;
using UnityEngine;

public enum CellType  { Empty, Wall, Exit }
public enum Direction { North, East, South, West }

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Tile Size (world units per cell)")]
    public float tileSize = 1f;

    [Header("Prefabs (optional — primitives used as fallback)")]
    public GameObject floorTilePrefab;
    public GameObject wallTilePrefab;
    public GameObject exitTilePrefab;

    private CellType[,] _cells;
    private readonly Dictionary<Vector2Int, ObstacleBlock> _obstacles = new Dictionary<Vector2Int, ObstacleBlock>();

    public int Width   { get; private set; }
    public int Height  { get; private set; }
    public Vector2Int ExitPos { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void InitializeGrid(CellType[,] cells, Vector2Int exitPos)
    {
        _cells  = cells;
        ExitPos = exitPos;
        Width   = cells.GetLength(0);
        Height  = cells.GetLength(1);
        SpawnTileVisuals();
    }

    public Vector3 GridToWorld(Vector2Int pos)
    {
        return new Vector3(pos.x * tileSize, 0f, pos.y * tileSize);
    }

    public bool IsInBounds(Vector2Int p)
    {
        return p.x >= 0 && p.x < Width && p.y >= 0 && p.y < Height;
    }

    public bool IsWall(Vector2Int p)
    {
        return !IsInBounds(p) || _cells[p.x, p.y] == CellType.Wall;
    }

    public bool IsExit(Vector2Int p)
    {
        return IsInBounds(p) && _cells[p.x, p.y] == CellType.Exit;
    }

    public bool IsWalkable(Vector2Int p)
    {
        return IsInBounds(p) && _cells[p.x, p.y] != CellType.Wall && !_obstacles.ContainsKey(p);
    }

    public void RegisterObstacle(Vector2Int pos, ObstacleBlock obs)   { _obstacles[pos] = obs; }
    public void UnregisterObstacle(Vector2Int pos)                     { _obstacles.Remove(pos); }
    public bool HasObstacle(Vector2Int pos)                            { return _obstacles.ContainsKey(pos); }

    public ObstacleBlock GetObstacle(Vector2Int pos)
    {
        ObstacleBlock obs;
        _obstacles.TryGetValue(pos, out obs);
        return obs;
    }

    public bool SlideObstacle(ObstacleBlock obs, Direction dir)
    {
        Vector2Int delta   = DirToVec(dir);
        Vector2Int current = obs.GridPos;
        Vector2Int next    = current + delta;

        if (IsWall(next) || _obstacles.ContainsKey(next)) return false;

        while (!IsWall(next) && !_obstacles.ContainsKey(next))
        {
            current = next;
            next    = current + delta;
        }

        UnregisterObstacle(obs.GridPos);
        obs.MoveTo(current);
        RegisterObstacle(current, obs);
        return true;
    }

    public static Vector2Int DirToVec(Direction d)
    {
        if (d == Direction.North) return new Vector2Int( 0,  1);
        if (d == Direction.East)  return new Vector2Int( 1,  0);
        if (d == Direction.South) return new Vector2Int( 0, -1);
        if (d == Direction.West)  return new Vector2Int(-1,  0);
        return Vector2Int.zero;
    }

    public static Direction RotateCW(Direction d)
    {
        if (d == Direction.North) return Direction.East;
        if (d == Direction.East)  return Direction.South;
        if (d == Direction.South) return Direction.West;
        return Direction.North;
    }

    static readonly Color ColFloorA = new Color(1.00f, 0.93f, 0.90f);
    static readonly Color ColFloorB = new Color(1.00f, 0.87f, 0.80f);
    static readonly Color ColWall   = new Color(0.95f, 0.58f, 0.63f);
    static readonly Color ColExit   = new Color(0.45f, 0.91f, 0.60f);

    void SpawnTileVisuals()
    {
        Transform tileRoot = new GameObject("Tiles").transform;
        tileRoot.SetParent(transform);

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                Vector2Int pos   = new Vector2Int(x, y);
                Vector3    world = GridToWorld(pos);
                CellType   cell  = _cells[x, y];

                GameObject prefab = null;
                if      (cell == CellType.Wall) prefab = wallTilePrefab;
                else if (cell == CellType.Exit) prefab = exitTilePrefab;
                else                            prefab = floorTilePrefab;

                GameObject tile;
                if (prefab != null)
                    tile = Instantiate(prefab, world, Quaternion.identity, tileRoot);
                else
                    tile = BuildProceduralTile(cell, world, tileRoot);

                tile.name = "Tile_" + x + "_" + y + "_" + cell;

                if (cell == CellType.Exit && prefab == null)
                    AddExitMarker(world);
            }
        }
    }

    GameObject BuildProceduralTile(CellType cell, Vector3 world, Transform parent)
    {
        bool isWall = (cell == CellType.Wall);
        GameObject go;

        if (isWall)
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.position   = world + Vector3.up * 0.25f;
            go.transform.localScale = new Vector3(tileSize, 0.5f, tileSize);
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.transform.position = world;
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = new Vector3(tileSize, tileSize, 1f);
        }

        go.transform.SetParent(parent);
        Destroy(go.GetComponent<Collider>());

        Color col;
        if      (cell == CellType.Wall) col = ColWall;
        else if (cell == CellType.Exit) col = ColExit;
        else    col = ((x(world) + z(world)) % 2 == 0) ? ColFloorA : ColFloorB;

        Renderer rend = go.GetComponent<Renderer>();
        Material mat  = new Material(Shader.Find("Unlit/Color"));
        mat.color     = col;
        rend.material = mat;
        return go;
    }

    int x(Vector3 w) { return Mathf.RoundToInt(w.x / tileSize); }
    int z(Vector3 w) { return Mathf.RoundToInt(w.z / tileSize); }

    void AddExitMarker(Vector3 world)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(marker.GetComponent<Collider>());
        marker.transform.position   = world + Vector3.up * 0.02f;
        marker.transform.rotation   = Quaternion.Euler(90f, 0f, 0f);
        marker.transform.localScale = Vector3.one * (tileSize * 0.55f);
        marker.name = "ExitMarker";
        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = new Color(0.2f, 0.95f, 0.45f);
        marker.GetComponent<Renderer>().material = mat;
    }
}
