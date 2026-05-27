using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class InteractionFlowManager : MonoBehaviour
{
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private BoardPlayerMover playerMover;
    [SerializeField] private BoardObject playerObject;
    [SerializeField] private BoardObject targetDoor;
    [SerializeField] private LevelResultFlashUI resultFlashUI;
    [SerializeField] private int maxInteractionCount = 6;
    [SerializeField] private int interactionCount;

    private int lastInteractionFrame = -1;
    private int lastHandledActionFrame = -1;
    private bool levelFinished;
    private DoorScript.Door targetDoorScript;
    private Coroutine actionLimitRoutine;

    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.right,
        Vector2Int.left,
        Vector2Int.up,
        Vector2Int.down
    };

    public int InteractionCount => interactionCount;
    public int MaxInteractionCount => Mathf.Max(1, maxInteractionCount);
    public event Action<int> InteractionCountChanged;
    public bool HasRegisteredInteractionThisFrame => lastInteractionFrame == Time.frameCount;
    public bool HasHandledActionThisFrame => lastHandledActionFrame == Time.frameCount;
    public bool CanAcceptAction
    {
        get
        {
            EnsureReferences();
            return !levelFinished
                && lastHandledActionFrame != Time.frameCount
                && (playerMover == null || !playerMover.IsMoving);
        }
    }

    public void Configure(BoardManager newBoardManager, BoardPlayerMover newPlayerMover, BoardObject newPlayerObject, BoardObject newTargetDoor)
    {
        boardManager = newBoardManager;
        playerMover = newPlayerMover;
        playerObject = newPlayerObject;
        targetDoor = newTargetDoor;
        targetDoorScript = null;
    }

    public void ConfigureResultFlash(LevelResultFlashUI newResultFlashUI)
    {
        resultFlashUI = newResultFlashUI;
    }

    private void Awake()
    {
        EnsureReferences();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            ReloadCurrentLevel();
    }

    public bool RegisterInteraction(SelectableControlObject selectedObject, ControlAction action)
    {
        return RegisterInteraction();
    }

    public bool RegisterInteraction()
    {
        if (!CanAcceptAction)
            return false;

        interactionCount++;
        lastInteractionFrame = Time.frameCount;
        lastHandledActionFrame = Time.frameCount;
        InteractionCountChanged?.Invoke(interactionCount);
        StepPlayerTowardDoor();

        if (interactionCount >= MaxInteractionCount)
            BeginActionLimitResolution();

        return true;
    }

    public void MarkActionHandledWithoutInteraction()
    {
        lastHandledActionFrame = Time.frameCount;
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

        var doorOpen = IsTargetDoorOpen();
        if (goals.Count > 0 && doorOpen)
            return goals;

        goals.Clear();
        var doorTile = targetDoor.TilePosition;

        foreach (var direction in Directions)
        {
            var candidate = doorTile + direction;
            if (!doorOpen && boardManager.IsGoalTile(candidate))
                continue;

            if (boardManager.CanEnterTile(playerObject, candidate))
                goals.Add(candidate);
        }

        return goals;
    }

    private bool IsTargetDoorOpen()
    {
        if (targetDoor == null)
            return true;

        if (targetDoorScript == null)
            targetDoorScript = targetDoor.GetComponentInChildren<DoorScript.Door>(true);

        return targetDoorScript == null || targetDoorScript.open;
    }

    private void BeginActionLimitResolution()
    {
        if (levelFinished)
            return;

        levelFinished = true;

        if (actionLimitRoutine != null)
            StopCoroutine(actionLimitRoutine);

        actionLimitRoutine = StartCoroutine(ResolveActionLimitAfterMovement());
    }

    private IEnumerator ResolveActionLimitAfterMovement()
    {
        while (playerMover != null && playerMover.IsMoving)
            yield return null;

        var succeeded = IsPlayerOnGoalTile();
        if (resultFlashUI != null)
            resultFlashUI.Flash(succeeded);

        actionLimitRoutine = null;
    }

    private bool IsPlayerOnGoalTile()
    {
        EnsureReferences();

        if (boardManager == null || playerObject == null)
            return false;

        boardManager.RebuildRegistry();
        foreach (var goalObject in boardManager.GetGoalObjects())
        {
            if (goalObject != null && goalObject.TilePosition == playerObject.TilePosition)
                return true;
        }

        return false;
    }

    private void ReloadCurrentLevel()
    {
        var activeScene = SceneManager.GetActiveScene();
        if (activeScene.buildIndex >= 0)
            SceneManager.LoadScene(activeScene.buildIndex);
        else
            SceneManager.LoadScene(activeScene.name);
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

        var needsBoardObjectLookup = playerObject == null || targetDoor == null || !targetDoor.gameObject.activeInHierarchy;
        if (needsBoardObjectLookup)
        {
            var boardObjects = BoardManager.FindSceneBoardObjects();
            if (playerObject == null)
                playerObject = boardObjects.FirstOrDefault(item => item != null && item.ObjectType == BoardObjectType.Player && item.gameObject.activeInHierarchy);

            if (targetDoor == null || !targetDoor.gameObject.activeInHierarchy)
            {
                var previousDoor = targetDoor;
                targetDoor = boardObjects.FirstOrDefault(item => item != null && item.ObjectType == BoardObjectType.Door && item.gameObject.activeInHierarchy);
                if (targetDoor != previousDoor)
                    targetDoorScript = null;
            }
        }

        if (resultFlashUI == null)
            resultFlashUI = FindAnyObjectByType<LevelResultFlashUI>();
    }
}
