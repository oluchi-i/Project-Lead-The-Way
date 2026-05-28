using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class InteractionFlowManager : MonoBehaviour
{
    private enum LevelFlowState
    {
        Intro,
        Playing,
        Success,
        Failure
    }

    [SerializeField] private BoardManager boardManager;
    [SerializeField] private BoardPlayerMover playerMover;
    [SerializeField] private BoardObject playerObject;
    [SerializeField] private BoardObject startTile;
    [SerializeField] private BoardObject startDoor;
    [FormerlySerializedAs("targetDoor")]
    [SerializeField] private BoardObject destinationDoor;
    [SerializeField] private Vector2Int gameplayStartTile = new Vector2Int(5, 2);
    [SerializeField] private LevelResultFlashUI resultFlashUI;
    [SerializeField] private int maxInteractionCount = 6;
    [SerializeField] private int interactionCount;

    private int lastInteractionFrame = -1;
    private int lastHandledActionFrame = -1;
    private LevelFlowState flowState = LevelFlowState.Intro;
    private DoorScript.Door startDoorScript;
    private DoorScript.Door destinationDoorScript;
    private Coroutine introRoutine;
    private Coroutine interactionResolutionRoutine;
    private int activeObjectActionCount;

    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.right,
        Vector2Int.left,
        Vector2Int.up,
        Vector2Int.down
    };

    public BoardObject StartTile => startTile;
    public BoardObject StartDoor => startDoor;
    public BoardObject DestinationDoor => destinationDoor;
    public Vector2Int GameplayStartTile => gameplayStartTile;
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
            return flowState == LevelFlowState.Playing
                && lastHandledActionFrame != Time.frameCount
                && activeObjectActionCount == 0
                && (playerMover == null || !playerMover.IsMoving);
        }
    }

    public void Configure(BoardManager newBoardManager, BoardPlayerMover newPlayerMover, BoardObject newPlayerObject, BoardObject newDestinationDoor)
    {
        boardManager = newBoardManager;
        playerMover = newPlayerMover;
        playerObject = newPlayerObject;
        destinationDoor = newDestinationDoor;
        destinationDoorScript = null;
    }

    public void ConfigureLevelFlow(BoardObject newStartTile, BoardObject newStartDoor, BoardObject newDestinationDoor)
    {
        startTile = newStartTile;
        startDoor = newStartDoor;
        destinationDoor = newDestinationDoor;
        startDoorScript = null;
        destinationDoorScript = null;
    }

    public void ConfigureResultFlash(LevelResultFlashUI newResultFlashUI)
    {
        resultFlashUI = newResultFlashUI;
    }

    private void Awake()
    {
        EnsureReferences();
    }

    private void Start()
    {
        EnsureReferences();
        InteractionCountChanged?.Invoke(interactionCount);

        if (CanRunIntro())
            introRoutine = StartCoroutine(RunIntro());
        else
            flowState = LevelFlowState.Playing;
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

        RegisterInteractionCore();
        return true;
    }

    public bool TryBeginObjectAction()
    {
        if (!CanAcceptAction)
            return false;

        activeObjectActionCount++;
        lastHandledActionFrame = Time.frameCount;
        return true;
    }

    public void CompleteObjectAction(bool countsAsInteraction)
    {
        if (activeObjectActionCount > 0)
            activeObjectActionCount--;

        if (!countsAsInteraction || flowState != LevelFlowState.Playing)
            return;

        RegisterInteractionCore();
    }

    private void RegisterInteractionCore()
    {
        interactionCount++;
        lastInteractionFrame = Time.frameCount;
        lastHandledActionFrame = Time.frameCount;
        InteractionCountChanged?.Invoke(interactionCount);
        StepPlayerTowardDoor();
        BeginInteractionResolution();
    }

    public void MarkActionHandledWithoutInteraction()
    {
        lastHandledActionFrame = Time.frameCount;
    }

    public void StepPlayerTowardDoor()
    {
        EnsureReferences();

        if (boardManager == null || playerMover == null || playerObject == null || destinationDoor == null)
            return;

        if (playerMover.IsMoving)
            return;

        boardManager.RebuildRegistry();

        if (TryFindNextStep(out var direction))
            playerMover.TryStep(direction);
    }

    private IEnumerator RunIntro()
    {
        flowState = LevelFlowState.Intro;
        boardManager.RebuildRegistry();
        playerMover.PlaceAtTile(startTile.TilePosition);
        yield return null;

        var entryDoor = GetDoorScript(startDoor, ref startDoorScript);
        if (entryDoor != null)
        {
            entryDoor.Open();
            yield return WaitForDoor(entryDoor);
        }

        var path = CreateIntroPath(playerObject.TilePosition, gameplayStartTile);
        if (path.Count > 0)
            yield return playerMover.PlayScriptedPath(path);

        if (entryDoor != null)
        {
            entryDoor.Close();
            yield return WaitForDoor(entryDoor);
        }

        boardManager.RebuildRegistry();
        flowState = LevelFlowState.Playing;
        introRoutine = null;
    }

    private bool CanRunIntro()
    {
        return boardManager != null
            && playerMover != null
            && playerObject != null
            && startTile != null;
    }

    private List<Vector2Int> CreateIntroPath(Vector2Int fromTile, Vector2Int toTile)
    {
        var path = new List<Vector2Int>();
        var current = fromTile;

        while (current.x != toTile.x)
        {
            current += current.x < toTile.x ? Vector2Int.right : Vector2Int.left;
            path.Add(current);
        }

        while (current.y != toTile.y)
        {
            current += current.y < toTile.y ? Vector2Int.up : Vector2Int.down;
            path.Add(current);
        }

        return path;
    }

    private IEnumerator WaitForDoor(DoorScript.Door door)
    {
        if (door == null)
            yield break;

        var elapsed = 0f;
        const float maxWait = 1.5f;
        while (door.IsAnimating && elapsed < maxWait)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
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
            if (goalObject == null)
                continue;

            foreach (var goalTile in goalObject.GetOccupiedTiles())
            {
                if (boardManager.CanEnterTile(playerObject, goalTile) && HasEnterableGoalApproach(goalTile))
                    goals.Add(goalTile);
            }
        }

        var doorOpen = IsDestinationDoorOpen();
        if (goals.Count > 0 && doorOpen)
            return goals;

        goals.Clear();
        if (destinationDoor == null)
            return goals;

        var doorTile = destinationDoor.TilePosition;
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

    private bool HasEnterableGoalApproach(Vector2Int goalTile)
    {
        if (boardManager.IsInsideBounds(goalTile))
            return true;

        foreach (var direction in Directions)
        {
            var candidate = goalTile + direction;
            if (boardManager.IsInsideBounds(candidate) && boardManager.CanEnterTile(playerObject, candidate))
                return true;
        }

        return false;
    }

    private bool IsDestinationDoorOpen()
    {
        var door = GetDoorScript(destinationDoor, ref destinationDoorScript);
        return door == null || door.open;
    }

    private DoorScript.Door GetDoorScript(BoardObject doorObject, ref DoorScript.Door cachedDoor)
    {
        if (doorObject == null)
            return null;

        if (cachedDoor == null)
            cachedDoor = doorObject.GetComponentInChildren<DoorScript.Door>(true);

        return cachedDoor;
    }

    private void BeginInteractionResolution()
    {
        if (flowState != LevelFlowState.Playing)
            return;

        if (interactionResolutionRoutine != null)
            StopCoroutine(interactionResolutionRoutine);

        interactionResolutionRoutine = StartCoroutine(ResolveInteractionAfterMovement());
    }

    private IEnumerator ResolveInteractionAfterMovement()
    {
        while (playerMover != null && playerMover.IsMoving)
            yield return null;

        if (IsPlayerOnGoalTile())
            CompleteLevel(true);
        else if (interactionCount >= MaxInteractionCount)
            CompleteLevel(false);

        interactionResolutionRoutine = null;
    }

    private void CompleteLevel(bool succeeded)
    {
        if (flowState == LevelFlowState.Success || flowState == LevelFlowState.Failure)
            return;

        flowState = succeeded ? LevelFlowState.Success : LevelFlowState.Failure;

        if (succeeded)
        {
            var exitDoor = GetDoorScript(destinationDoor, ref destinationDoorScript);
            if (exitDoor != null)
                exitDoor.Close();
        }

        if (resultFlashUI != null)
            resultFlashUI.Flash(succeeded);
    }

    private bool IsPlayerOnGoalTile()
    {
        EnsureReferences();

        if (boardManager == null || playerObject == null)
            return false;

        boardManager.RebuildRegistry();
        foreach (var goalObject in boardManager.GetGoalObjects())
        {
            if (goalObject != null && goalObject.GetOccupiedTiles().Contains(playerObject.TilePosition))
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

        var needsBoardObjectLookup = playerObject == null
            || startTile == null
            || startDoor == null
            || destinationDoor == null
            || (startTile != null && !startTile.gameObject.activeInHierarchy)
            || (startDoor != null && !startDoor.gameObject.activeInHierarchy)
            || (destinationDoor != null && !destinationDoor.gameObject.activeInHierarchy);

        if (needsBoardObjectLookup)
        {
            var boardObjects = BoardManager.FindSceneBoardObjects()
                .Where(item => item != null && item.gameObject.activeInHierarchy)
                .ToList();

            if (playerObject == null)
                playerObject = boardObjects.FirstOrDefault(item => item.ObjectType == BoardObjectType.Player);

            if (startTile == null || !startTile.gameObject.activeInHierarchy)
                startTile = FindNamedBoardObject(boardObjects, "start", item => item.ObjectType != BoardObjectType.Door);

            if (startDoor == null || !startDoor.gameObject.activeInHierarchy)
            {
                var previousStartDoor = startDoor;
                startDoor = FindNamedBoardObject(boardObjects, "start", item => item.ObjectType == BoardObjectType.Door);
                if (startDoor != previousStartDoor)
                    startDoorScript = null;
            }

            if (destinationDoor == null || !destinationDoor.gameObject.activeInHierarchy)
            {
                var previousDestinationDoor = destinationDoor;
                destinationDoor = FindDestinationDoorCandidate(boardObjects);
                if (destinationDoor != previousDestinationDoor)
                    destinationDoorScript = null;
            }
        }

        if (resultFlashUI == null)
            resultFlashUI = FindAnyObjectByType<LevelResultFlashUI>();
    }

    private BoardObject FindDestinationDoorCandidate(List<BoardObject> boardObjects)
    {
        var namedDoor = FindNamedBoardObject(boardObjects, "destination", item => item.ObjectType == BoardObjectType.Door)
            ?? FindNamedBoardObject(boardObjects, "exit", item => item.ObjectType == BoardObjectType.Door);

        if (namedDoor != null)
            return namedDoor;

        return boardObjects.FirstOrDefault(item => item.ObjectType == BoardObjectType.Door && item != startDoor);
    }

    private static BoardObject FindNamedBoardObject(List<BoardObject> boardObjects, string namePart, Func<BoardObject, bool> predicate)
    {
        return boardObjects.FirstOrDefault(item =>
            predicate(item)
            && item.name.IndexOf(namePart, StringComparison.OrdinalIgnoreCase) >= 0);
    }
}
