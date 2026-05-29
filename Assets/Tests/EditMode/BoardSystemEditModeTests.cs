using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class BoardSystemEditModeTests
{
    private readonly List<GameObject> createdObjects = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        for (var i = createdObjects.Count - 1; i >= 0; i--)
        {
            if (createdObjects[i] != null)
                Object.DestroyImmediate(createdObjects[i]);
        }

        createdObjects.Clear();
    }

    [Test]
    public void BoardObjectReportsAllOccupiedTilesWithoutDuplicates()
    {
        var boardObject = CreateBoardObject(
            "Wide Object",
            BoardObjectType.Decoration,
            new Vector2Int(2, 3),
            true,
            true,
            false,
            new List<Vector2Int> { Vector2Int.zero, Vector2Int.right, Vector2Int.right });

        var occupiedTiles = boardObject.GetOccupiedTiles();

        Assert.That(occupiedTiles, Is.EquivalentTo(new[] { new Vector2Int(2, 3), new Vector2Int(3, 3) }));
    }

    [Test]
    public void BoardManagerBlocksEveryTileInStaticFootprint()
    {
        var boardManager = CreateBoardManager();
        var bed = CreateBoardObject(
            "Bed",
            BoardObjectType.Decoration,
            new Vector2Int(2, 2),
            true,
            true,
            false,
            new List<Vector2Int> { Vector2Int.zero, Vector2Int.right, Vector2Int.up });

        boardManager.Register(bed);

        Assert.IsTrue(boardManager.IsBlocked(new Vector2Int(2, 2)));
        Assert.IsTrue(boardManager.IsBlocked(new Vector2Int(3, 2)));
        Assert.IsTrue(boardManager.IsBlocked(new Vector2Int(2, 3)));
        Assert.IsFalse(boardManager.IsBlocked(new Vector2Int(4, 4)));
    }

    [Test]
    public void MovableObjectCannotEnterTileBlockedByAnotherObject()
    {
        var boardManager = CreateBoardManager();
        var box = CreateBoardObject("Box", BoardObjectType.MovableObject, new Vector2Int(1, 1), true, true, true);
        var blocker = CreateBoardObject("Blocker", BoardObjectType.Decoration, new Vector2Int(2, 1), true, true, false);

        boardManager.Register(box);
        boardManager.Register(blocker);

        Assert.IsFalse(boardManager.CanEnterTile(box, new Vector2Int(2, 1)));
    }

    [Test]
    public void InactiveBoardObjectsDoNotBlockMovementWhenRegistryRebuilds()
    {
        var boardManager = CreateBoardManager();
        var box = CreateBoardObject("Box", BoardObjectType.MovableObject, new Vector2Int(1, 1), true, true, true);
        var inactiveBlocker = CreateBoardObject("Inactive Blocker", BoardObjectType.Decoration, new Vector2Int(2, 1), true, true, false);
        inactiveBlocker.gameObject.SetActive(false);

        boardManager.RebuildRegistry();

        Assert.IsTrue(boardManager.CanEnterTile(box, new Vector2Int(2, 1)));
    }

    [Test]
    public void PlayerCanEnterRegisteredGoalTileOutsideBoardBounds()
    {
        var boardManager = CreateBoardManager();
        var player = CreateBoardObject("Player", BoardObjectType.Player, new Vector2Int(0, 0), true, true, true);
        var goal = CreateBoardObject("Goal", BoardObjectType.Goal, new Vector2Int(-1, 0), true, false, false);

        boardManager.Register(player);
        boardManager.Register(goal);

        Assert.IsTrue(boardManager.CanEnterTile(player, new Vector2Int(-1, 0)));
    }

    [Test]
    public void InteractionLimitIsNeverLessThanOne()
    {
        var flowManager = CreateObject("Flow").AddComponent<InteractionFlowManager>();
        SetPrivateField(flowManager, "maxInteractionCount", 0);

        Assert.AreEqual(1, flowManager.MaxInteractionCount);
    }

    [Test]
    public void GridTileMoverDoesNotFallbackToWorldMovementWhenBoardMovementIsDisabled()
    {
        var moverObject = CreateObject("Loose Mover");
        var mover = moverObject.AddComponent<GridTileMover>();
        mover.ConfigureBoardMovement(false, false);

        var startPosition = moverObject.transform.position;

        Assert.IsFalse(mover.TryMovePositiveX());
        Assert.AreEqual(startPosition, moverObject.transform.position);
    }

    private BoardManager CreateBoardManager()
    {
        return CreateObject("Board Manager").AddComponent<BoardManager>();
    }

    private BoardObject CreateBoardObject(
        string name,
        BoardObjectType type,
        Vector2Int tilePosition,
        bool occupiesTile,
        bool blocksMovement,
        bool movable,
        List<Vector2Int> occupiedOffsets = null)
    {
        var boardObject = CreateObject(name).AddComponent<BoardObject>();
        boardObject.Configure(type, occupiesTile, blocksMovement, movable);
        boardObject.SetTilePosition(tilePosition);

        if (occupiedOffsets != null)
            SetPrivateField(boardObject, "occupiedTileOffsets", occupiedOffsets);

        return boardObject;
    }

    private GameObject CreateObject(string name)
    {
        var gameObject = new GameObject(name);
        createdObjects.Add(gameObject);
        return gameObject;
    }

    private static void SetPrivateField<T>(T target, string fieldName, object value)
    {
        var field = typeof(T).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, $"Expected field '{fieldName}' on {typeof(T).Name}.");
        field.SetValue(target, value);
    }
}
