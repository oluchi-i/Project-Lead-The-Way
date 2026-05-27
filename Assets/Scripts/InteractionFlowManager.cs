using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

public class InteractionFlowManager : MonoBehaviour
{
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private BoardPlayerMover playerMover;
    [SerializeField] private BoardObject playerObject;
    [SerializeField] private BoardObject targetDoor;
    [SerializeField] private int interactionCount;
    private int lastInteractionFrame = -1;

    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.right,
        Vector2Int.left,
        Vector2Int.up,
        Vector2Int.down
    };

    public int InteractionCount => interactionCount;
    public event Action<int> InteractionCountChanged;
    public bool HasRegisteredInteractionThisFrame => lastInteractionFrame == Time.frameCount;

    public void Configure(BoardManager newBoardManager, BoardPlayerMover newPlayerMover, BoardObject newPlayerObject, BoardObject newTargetDoor)
    {
        boardManager = newBoardManager;
        playerMover = newPlayerMover;
        playerObject = newPlayerObject;
        targetDoor = newTargetDoor;
    }

    private void Awake()
    {
        EnsureReferences();
    }

    public void RegisterInteraction(SelectableControlObject selectedObject, ControlAction action)
    {
        RegisterInteraction();
    }

    public void RegisterInteraction()
    {
        interactionCount++;
        lastInteractionFrame = Time.frameCount;
        InteractionCountChanged?.Invoke(interactionCount);
        StepPlayerTowardDoor();
    }

    public void StepPlayerTowardDoor()
    {
        EnsureReferences();

        if (boardManager == null || playerMover == null || playerObject == null || targetDoor == null)
            return;

        if (playerMover.IsMoving)
            return;

        boardManager.RebuildRegistry();

        if (TryFindNextStep(out var direction))
            playerMover.TryStep(direction);
    }

    private bool TryFindNextStep(out Vector2Int nextDirection)
    {
        nextDirection = default;

        var start = playerObject.TilePosition;
        var goalTiles = GetDoorApproachTiles();
        if (goalTiles.Count == 0 || goalTiles.Contains(start))
            return false;

        var queue = new Queue<Vector2Int>();
        var visited = new HashSet<Vector2Int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();

        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (goalTiles.Contains(current))
            {
                nextDirection = ReconstructFirstDirection(start, current, cameFrom);
                return nextDirection != Vector2Int.zero;
            }

            foreach (var direction in Directions)
            {
                var next = current + direction;
                if (visited.Contains(next) || !boardManager.CanEnterTile(playerObject, next))
                    continue;

                visited.Add(next);
                cameFrom[next] = current;
                queue.Enqueue(next);
            }
        }

        return false;
    }

    private HashSet<Vector2Int> GetDoorApproachTiles()
    {
        var goals = new HashSet<Vector2Int>();
        foreach (var goalObject in boardManager.GetGoalObjects())
        {
            if (goalObject != null && boardManager.CanEnterTile(playerObject, goalObject.TilePosition))
                goals.Add(goalObject.TilePosition);
        }

        if (goals.Count > 0 && IsTargetDoorOpen())
            return goals;

        var doorTile = targetDoor.TilePosition;

        foreach (var direction in Directions)
        {
            var candidate = doorTile + direction;
            if (boardManager.CanEnterTile(playerObject, candidate))
                goals.Add(candidate);
        }

        return goals;
    }

    private bool IsTargetDoorOpen()
    {
        if (targetDoor == null)
            return true;

        var door = targetDoor.GetComponentInChildren<DoorScript.Door>(true);
        return door == null || door.open;
    }

    private static Vector2Int ReconstructFirstDirection(Vector2Int start, Vector2Int goal, Dictionary<Vector2Int, Vector2Int> cameFrom)
    {
        var current = goal;
        while (cameFrom.TryGetValue(current, out var previous) && previous != start)
            current = previous;

        return current - start;
    }

    private void EnsureReferences()
    {
        if (boardManager == null)
            boardManager = FindAnyObjectByType<BoardManager>();

        if (playerMover == null)
            playerMover = FindAnyObjectByType<BoardPlayerMover>();

        if (playerObject == null && playerMover != null)
            playerObject = playerMover.GetComponent<BoardObject>();

        var boardObjects = BoardManager.FindSceneBoardObjects();

        if (playerObject == null)
            playerObject = boardObjects.FirstOrDefault(item => item != null && item.ObjectType == BoardObjectType.Player && item.gameObject.activeInHierarchy);

        if (targetDoor == null || !targetDoor.gameObject.activeInHierarchy)
            targetDoor = boardObjects.FirstOrDefault(item => item != null && item.ObjectType == BoardObjectType.Door && item.gameObject.activeInHierarchy);
    }
}
