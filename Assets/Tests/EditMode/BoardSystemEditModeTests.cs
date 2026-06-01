using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

public class BoardSystemEditModeTests
{
    private const string Level01ScenePath = "Assets/Scenes/Level01.unity";
    private const string BuildSettingsPath = "ProjectSettings/EditorBuildSettings.asset";

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

    [Test]
    public void BuildSettingsUseLevel01AsOnlyEnabledScene()
    {
        var buildSettings = ReadProjectFile(BuildSettingsPath);

        Assert.That(Regex.Matches(buildSettings, @"^\s*enabled:\s*1$", RegexOptions.Multiline).Count, Is.EqualTo(1));
        Assert.That(buildSettings, Does.Contain($"path: {Level01ScenePath}"));
        Assert.That(buildSettings, Does.Not.Contain("path: Assets/Scenes/Dev.unity"));
        Assert.That(buildSettings, Does.Not.Contain("path: Assets/Scenes/Player.unity"));
    }

    [Test]
    public void Level01HasRequiredFlowReferencesSerialized()
    {
        var scene = ReadProjectFile(Level01ScenePath);

        AssertSerializedReference(scene, "boardManager");
        AssertSerializedReference(scene, "playerMover");
        AssertSerializedReference(scene, "playerObject");
        AssertSerializedReference(scene, "startTile");
        AssertSerializedReference(scene, "startDoor");
        AssertSerializedReference(scene, "destinationDoor");
        AssertSerializedReference(scene, "resultFlashUI");
        Assert.That(scene, Does.Contain("maxInteractionCount: 6"));
    }

    [Test]
    public void Level01HasNoDuplicateBoardObjectIds()
    {
        var scene = ReadProjectFile(Level01ScenePath);
        var ids = Regex.Matches(scene, @"^\s*objectId:\s*(\S+)", RegexOptions.Multiline)
            .Cast<Match>()
            .Select(match => match.Groups[1].Value)
            .ToList();

        var duplicateIds = ids.GroupBy(id => id)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        Assert.That(duplicateIds, Is.Empty);
    }

    [Test]
    public void Level01ResultFeedbackHasAudioAndVisualReferences()
    {
        var scene = ReadProjectFile(Level01ScenePath);

        AssertSerializedReference(scene, "overlay");
        AssertSerializedReference(scene, "audioSource");
        AssertSerializedReference(scene, "successSound");
        AssertSerializedReference(scene, "failureSound");
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

    private static void AssertSerializedReference(string yaml, string fieldName)
    {
        var pattern = $@"^\s*{Regex.Escape(fieldName)}:\s*\{{fileID:\s*(?!0\}})[^}}]+\}}";
        Assert.That(Regex.IsMatch(yaml, pattern, RegexOptions.Multiline), Is.True, $"Expected '{fieldName}' to be wired.");
    }

    private static string ReadProjectFile(string relativePath)
    {
        var projectRoot = Directory.GetParent(Application.dataPath).FullName;
        return File.ReadAllText(Path.Combine(projectRoot, relativePath));
    }
}
