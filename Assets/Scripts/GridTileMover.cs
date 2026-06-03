using UnityEngine;

public class GridTileMover : MonoBehaviour
{
    [SerializeField] private bool useBoardManager = true;
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private float moveDuration = 0.18f;
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private BoardObject boardObject;
    [SerializeField] private InteractionFlowManager interactionFlowManager;

    private bool isMoving;
    private float moveElapsed;
    private Vector3 moveStart;
    private Vector3 moveTarget;
    private bool pendingInteractionOnMoveComplete;
    private bool loggedMissingReferences;

    public bool IsMoving => isMoving;

    private void Awake()
    {
        EnsureReferences();
    }

    private void Update()
    {
        if (isMoving)
            UpdateMove();
    }

    public void MovePositiveX()
    {
        TryMove(Vector2Int.right);
    }

    public void MoveNegativeX()
    {
        TryMove(Vector2Int.left);
    }

    public void MovePositiveZ()
    {
        TryMove(Vector2Int.up);
    }

    public void MoveNegativeZ()
    {
        TryMove(Vector2Int.down);
    }

    public bool TryMovePositiveX()
    {
        return TryMove(Vector2Int.right);
    }

    public bool TryMoveNegativeX()
    {
        return TryMove(Vector2Int.left);
    }

    public bool TryMovePositiveZ()
    {
        return TryMove(Vector2Int.up);
    }

    public bool TryMoveNegativeZ()
    {
        return TryMove(Vector2Int.down);
    }

    public void ConfigureBoardMovement(bool enabled, bool keyboardEnabled)
    {
        useBoardManager = enabled;
        EnsureReferences();
    }

    public void Configure(BoardManager newBoardManager, BoardObject newBoardObject, InteractionFlowManager newInteractionFlowManager, bool enabled = true)
    {
        boardManager = newBoardManager;
        boardObject = newBoardObject;
        interactionFlowManager = newInteractionFlowManager;
        useBoardManager = enabled;

        if (boardManager != null)
            tileSize = boardManager.TileSize;
    }

    private bool TryMove(Vector2Int boardDirection)
    {
        if (isMoving)
            return false;

        EnsureReferences();

        if (!useBoardManager || boardManager == null || boardObject == null || interactionFlowManager == null)
            return false;

        if (!interactionFlowManager.CanAcceptAction)
            return false;

        moveStart = transform.position;

        var shouldRegisterInteraction = ShouldRegisterInteraction();
        var beganObjectAction = false;
        if (shouldRegisterInteraction)
        {
            if (!interactionFlowManager.TryBeginObjectAction())
                return false;

            beganObjectAction = true;
        }

        if (TryMoveOnBoard(boardDirection, out var targetTile))
        {
            moveTarget = boardManager.TileToWorld(targetTile, moveStart.y);
            pendingInteractionOnMoveComplete = beganObjectAction;
        }
        else
        {
            if (beganObjectAction)
                interactionFlowManager.CompleteObjectAction(false);

            MarkActionHandledWithoutInteraction();
            return false;
        }

        moveElapsed = 0f;
        isMoving = true;
        return true;
    }

    private bool ShouldRegisterInteraction()
    {
        return boardObject == null || boardObject.ObjectType != BoardObjectType.Player;
    }

    private void RegisterCompletedInteraction()
    {
        EnsureReferences();

        if (interactionFlowManager != null)
            interactionFlowManager.CompleteObjectAction(pendingInteractionOnMoveComplete);

        pendingInteractionOnMoveComplete = false;
    }

    private void MarkActionHandledWithoutInteraction()
    {
        EnsureReferences();

        if (interactionFlowManager != null)
            interactionFlowManager.MarkActionHandledWithoutInteraction();
    }

    private bool TryMoveOnBoard(Vector2Int boardDirection, out Vector2Int targetTile)
    {
        targetTile = default;

        EnsureReferences();

        if (boardObject == null || boardManager == null)
            return false;

        return boardManager.TryMoveObject(boardObject, boardDirection, out _, out targetTile);
    }

    private void UpdateMove()
    {
        moveElapsed += Time.deltaTime;
        var duration = Mathf.Max(0.01f, moveDuration);
        var t = Mathf.Clamp01(moveElapsed / duration);
        t = Mathf.SmoothStep(0f, 1f, t);

        transform.position = Vector3.Lerp(moveStart, moveTarget, t);

        if (moveElapsed >= duration)
        {
            transform.position = moveTarget;
            isMoving = false;
            RegisterCompletedInteraction();
        }
    }

    private void EnsureReferences()
    {
        if (boardObject == null)
            boardObject = GetComponent<BoardObject>();

        if (!loggedMissingReferences && useBoardManager && (boardManager == null || boardObject == null || interactionFlowManager == null))
        {
            loggedMissingReferences = true;
            Debug.LogWarning("GridTileMover is missing required board/action references. Run Tools > Lead The Way > Scene > Wire Current Scene References.", this);
        }
    }
}
