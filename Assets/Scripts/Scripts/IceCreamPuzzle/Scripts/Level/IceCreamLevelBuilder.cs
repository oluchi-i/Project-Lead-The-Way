using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
//  IceCreamLevelBuilder.cs — Level 1: "Sweetie Boulevard"
//
//  Board (8×8, y=0 at bottom):
//
//   Y  0  1  2  3  4  5  6  7
//   7  W  W  W  W  W  W  W  W
//   6  W  .  .  .  P  .  E  W   E=exit  P=Popsicle
//   5  W  .  .  .  .  .  .  W
//   4  W  .  I  .  .  .  .  W   I=IceCreamScoop
//   3  W  .  .  .  .  C  .  W   C=WaffleCone
//   2  W  .  .  B  .  .  .  W   B=FrozenBlock
//   1  W  @  .  .  .  .  .  W   @=character, facing East
//   0  W  W  W  W  W  W  W  W
//
//  Each obstacle gets:
//    • ObstacleBlock       – ice cream colour, GridManager registry
//    • BoardObject         – grid position, BoardManager registry
//    • GridTileMover       – smooth ice-slide animation
//    • SelectableControlObject – 4 directional ControlActions for SelectionPanelsUI
// ─────────────────────────────────────────────────────────────────────────────

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

    [Header("Max interactions for Level 1")]
    public int maxInteractionCount = 8;

    // ── Obstacle table ────────────────────────────────────────────────────
    static readonly int[]          ObsX    = { 3,  5,  2,  4 };
    static readonly int[]          ObsY    = { 2,  3,  4,  6 };
    static readonly ObstacleType[] ObsType =
    {
        ObstacleType.FrozenBlock,
        ObstacleType.WaffleCone,
        ObstacleType.IceCreamScoop,
        ObstacleType.Popsicle
    };
    static readonly string[] ObsNames = { "Frozen Block", "Waffle Cone", "Ice Cream Scoop", "Popsicle" };

    static readonly Vector2Int CharStart  = new Vector2Int(1, 1);
    static readonly Vector2Int ExitCell   = new Vector2Int(6, 6);
    const Direction            CharFacing = Direction.East;

    // ── Build ─────────────────────────────────────────────────────────────

    public void BuildLevel()
    {
        if (gridManager == null)
            gridManager = FindAnyObjectByType<GridManager>();

        gridManager.InitializeGrid(BuildLayout(), ExitCell);

        // Tell InteractionFlowManager the move budget for this level
        var flow = FindAnyObjectByType<InteractionFlowManager>();
        if (flow != null)
            flow.SetMaxInteractionCount(maxInteractionCount);

        for (int i = 0; i < ObsX.Length; i++)
            SpawnObstacle(new Vector2Int(ObsX[i], ObsY[i]), ObsType[i], ObsNames[i]);

        SpawnCharacter();
        Debug.Log("[IceCreamLevelBuilder] Level 1 built.");
    }

    // ── Layout ────────────────────────────────────────────────────────────

    CellType[,] BuildLayout()
    {
        CellType[,] cells = new CellType[8, 8];
        for (int x = 0; x < 8; x++)
            for (int y = 0; y < 8; y++)
                cells[x, y] = CellType.Empty;

        for (int x = 0; x < 8; x++) { cells[x, 0] = CellType.Wall; cells[x, 7] = CellType.Wall; }
        for (int y = 0; y < 8; y++) { cells[0, y] = CellType.Wall; cells[7, y] = CellType.Wall; }

        cells[ExitCell.x, ExitCell.y] = CellType.Exit;
        return cells;
    }

    // ── Obstacle spawner ──────────────────────────────────────────────────

    void SpawnObstacle(Vector2Int pos, ObstacleType type, string displayName)
    {
        Vector3    world = gridManager.GridToWorld(pos);
        GameObject go    = obstaclePrefab != null
            ? Instantiate(obstaclePrefab, world, Quaternion.identity)
            : MakeFallbackCube(world, type);

        go.name = "Obstacle_" + displayName.Replace(" ", "");

        // ── Core obstacle ─────────────────────────────────────────────────
        if (go.GetComponentInChildren<Collider>() == null)
            go.AddComponent<BoxCollider>();

        ObstacleBlock block = go.GetComponent<ObstacleBlock>();
        if (block == null) block = go.AddComponent<ObstacleBlock>();
        block.Initialize(pos, type);

        // ── Board integration ─────────────────────────────────────────────
        BoardObject boardObj = go.GetComponent<BoardObject>();
        if (boardObj == null) boardObj = go.AddComponent<BoardObject>();
        boardObj.SetGridPosition(pos);

        // ── Smooth slide mover ────────────────────────────────────────────
        GridTileMover mover = go.GetComponent<GridTileMover>();
        if (mover == null) mover = go.AddComponent<GridTileMover>();

        // ── Selection UI ──────────────────────────────────────────────────
        SetupSelectableActions(go, mover, displayName);
    }

    // ── Wire SelectableControlObject with 4 directional ControlActions ────

    void SetupSelectableActions(GameObject go, GridTileMover mover, string displayName)
    {
        SelectableControlObject selectable = go.GetComponent<SelectableControlObject>();
        if (selectable == null) selectable = go.AddComponent<SelectableControlObject>();

        selectable.displayName = displayName;
        selectable.actions.Clear();

        // Arrow key labels match SelectionPanelsUI keyboard mapping:
        //   Left  → TryMovePositiveX   (isometric NW)
        //   Right → TryMoveNegativeX   (isometric SE)
        //   Up    → TryMoveNegativeZ   (isometric NE)
        //   Down  → TryMovePositiveZ   (isometric SW)

        ControlAction aNW = new ControlAction();
        aNW.label = "◄ NW";
        GridTileMover m1 = mover; // capture for lambda
        aNW.onAction.AddListener(() => m1.TryMovePositiveX());
        selectable.actions.Add(aNW);

        ControlAction aSE = new ControlAction();
        aSE.label = "► SE";
        GridTileMover m2 = mover;
        aSE.onAction.AddListener(() => m2.TryMoveNegativeX());
        selectable.actions.Add(aSE);

        ControlAction aNE = new ControlAction();
        aNE.label = "▲ NE";
        GridTileMover m3 = mover;
        aNE.onAction.AddListener(() => m3.TryMoveNegativeZ());
        selectable.actions.Add(aNE);

        ControlAction aSW = new ControlAction();
        aSW.label = "▼ SW";
        GridTileMover m4 = mover;
        aSW.onAction.AddListener(() => m4.TryMovePositiveZ());
        selectable.actions.Add(aSW);
    }

    // ── Character spawner ─────────────────────────────────────────────────

    void SpawnCharacter()
    {
        Vector3    world = gridManager.GridToWorld(CharStart);
        GameObject go    = characterPrefab != null
            ? Instantiate(characterPrefab, world, Quaternion.identity)
            : MakeFallbackCharacter(world);

        go.name = "Character";

        // Wire CharacterMover
        CharacterMover mover = go.GetComponent<CharacterMover>();
        if (mover == null) mover = go.AddComponent<CharacterMover>();
        characterMover = mover;
        if (GameManager.Instance != null) GameManager.Instance.character = mover;
        mover.Initialize(CharStart, CharFacing);

        // Wire BoardPlayerMover (uses your existing walk-animation system)
        BoardObject boardObj = go.GetComponent<BoardObject>();
        if (boardObj == null) boardObj = go.AddComponent<BoardObject>();
        boardObj.SetGridPosition(CharStart);

        BoardPlayerMover bpm = go.GetComponent<BoardPlayerMover>();
        if (bpm == null) bpm = go.AddComponent<BoardPlayerMover>();

        BoardManager bm = FindAnyObjectByType<BoardManager>();
        if (bm != null)
            bpm.Configure(bm, boardObj);
    }

    // ── Primitive fallbacks ───────────────────────────────────────────────

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
