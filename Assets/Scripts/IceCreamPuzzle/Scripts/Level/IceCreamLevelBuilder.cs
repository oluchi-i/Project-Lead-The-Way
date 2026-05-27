using UnityEngine;

// Level map (8x8):
//   Y  0  1  2  3  4  5  6  7
//   7  W  W  W  W  W  W  W  W
//   6  W  .  .  .  P  .  E  W   E=exit  P=Popsicle
//   5  W  .  .  .  .  .  .  W
//   4  W  .  I  .  .  .  .  W   I=IceCreamScoop
//   3  W  .  .  .  .  C  .  W   C=WaffleCone
//   2  W  .  .  B  .  .  .  W   B=FrozenBlock
//   1  W  @  .  .  .  .  .  W   @=character start, facing East
//   0  W  W  W  W  W  W  W  W
//      X  0  1  2  3  4  5  6  7

public class IceCreamLevelBuilder : MonoBehaviour
{
    [Header("Prefabs — leave empty to use primitive fallbacks")]
    public GameObject floorTilePrefab;
    public GameObject wallTilePrefab;
    public GameObject exitTilePrefab;
    public GameObject obstaclePrefab;
    public GameObject characterPrefab;

    [Header("Scene references")]
    public GridManager    gridManager;
    public CharacterMover characterMover;

    // ── Obstacle table ────────────────────────────────────────────────────────
    static readonly int[]          ObsX    = { 3,  5,  2,  4 };
    static readonly int[]          ObsY    = { 2,  3,  4,  6 };
    static readonly ObstacleType[] ObsType =
    {
        ObstacleType.FrozenBlock,
        ObstacleType.WaffleCone,
        ObstacleType.IceCreamScoop,
        ObstacleType.Popsicle
    };

    static readonly Vector2Int CharStart   = new Vector2Int(1, 1);
    static readonly Vector2Int ExitCell    = new Vector2Int(6, 6);
    const Direction            CharFacing  = Direction.East;

    public void BuildLevel()
    {
        if (gridManager == null)
            gridManager = FindObjectOfType<GridManager>();

        CellType[,] cells = BuildLayout();
        gridManager.InitializeGrid(cells, ExitCell);

        for (int i = 0; i < ObsX.Length; i++)
            SpawnObstacle(new Vector2Int(ObsX[i], ObsY[i]), ObsType[i]);

        SpawnCharacter();
        Debug.Log("[IceCreamLevelBuilder] Level built.");
    }

    // ── Layout ────────────────────────────────────────────────────────────────

    CellType[,] BuildLayout()
    {
        CellType[,] cells = new CellType[8, 8];

        // Default: empty
        for (int x = 0; x < 8; x++)
            for (int y = 0; y < 8; y++)
                cells[x, y] = CellType.Empty;

        // Border walls
        for (int x = 0; x < 8; x++) { cells[x, 0] = CellType.Wall; cells[x, 7] = CellType.Wall; }
        for (int y = 0; y < 8; y++) { cells[0, y] = CellType.Wall; cells[7, y] = CellType.Wall; }

        cells[ExitCell.x, ExitCell.y] = CellType.Exit;
        return cells;
    }

    // ── Spawners ──────────────────────────────────────────────────────────────

    void SpawnObstacle(Vector2Int pos, ObstacleType type)
    {
        Vector3    world = gridManager.GridToWorld(pos);
        GameObject go;

        if (obstaclePrefab != null)
        {
            go = Instantiate(obstaclePrefab, world, Quaternion.identity);
        }
        else
        {
            go = MakeFallbackCube(world, type);
        }

        go.name = "Obstacle_" + type + "_" + pos.x + "_" + pos.y;

        if (go.GetComponentInChildren<Collider>() == null)
            go.AddComponent<BoxCollider>();

        ObstacleBlock block = go.GetComponent<ObstacleBlock>();
        if (block == null) block = go.AddComponent<ObstacleBlock>();
        block.Initialize(pos, type);
    }

    void SpawnCharacter()
    {
        Vector3    world = gridManager.GridToWorld(CharStart);
        GameObject go;

        if (characterPrefab != null)
            go = Instantiate(characterPrefab, world, Quaternion.identity);
        else
            go = MakeFallbackCharacter(world);

        go.name = "Character";

        CharacterMover mover = go.GetComponent<CharacterMover>();
        if (mover == null) mover = go.AddComponent<CharacterMover>();

        characterMover = mover;
        if (GameManager.Instance != null)
            GameManager.Instance.character = mover;

        mover.Initialize(CharStart, CharFacing);
    }

    // ── Primitive fallbacks ───────────────────────────────────────────────────

    GameObject MakeFallbackCube(Vector3 world, ObstacleType type)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.transform.position   = world + Vector3.up * 0.5f;
        go.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);

        if (type == ObstacleType.IceCreamScoop)
        {
            GameObject scoop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            scoop.transform.SetParent(go.transform);
            scoop.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            scoop.transform.localScale    = Vector3.one * 0.75f;
            Destroy(scoop.GetComponent<Collider>());
        }

        return go;
    }

    GameObject MakeFallbackCharacter(Vector3 world)
    {
        GameObject root = new GameObject("CharacterRoot");
        root.transform.position = world;

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.transform.SetParent(root.transform);
        body.transform.localPosition = Vector3.up * 0.5f;
        body.transform.localScale    = new Vector3(0.4f, 0.5f, 0.4f);
        body.GetComponent<Renderer>().material.color = new Color(0.3f, 0.8f, 1f);
        Destroy(body.GetComponent<Collider>());

        GameObject hat = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hat.transform.SetParent(root.transform);
        hat.transform.localPosition = new Vector3(0f, 1.1f, 0f);
        hat.transform.localScale    = Vector3.one * 0.35f;
        hat.GetComponent<Renderer>().material.color = new Color(1f, 0.6f, 0.7f);
        Destroy(hat.GetComponent<Collider>());

        return root;
    }
}
