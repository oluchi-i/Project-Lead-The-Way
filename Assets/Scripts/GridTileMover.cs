using UnityEngine;
using UnityEngine.InputSystem;

public class GridTileMover : MonoBehaviour
{
    [SerializeField] private bool keyboardInputEnabled = true;
    [SerializeField] private bool useBoardManager = true;
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private float moveDuration = 0.18f;

    private BoardManager boardManager;
    private BoardObject boardObject;
    private InteractionFlowManager interactionFlowManager;
    private bool isMoving;
    private float moveElapsed;
    private Vector3 moveStart;
    private Vector3 moveTarget;

    private void Awake()
    {
        boardObject = GetComponent<BoardObject>();
        boardManager = FindAnyObjectByType<BoardManager>();
        interactionFlowManager = FindAnyObjectByType<InteractionFlowManager>();
    }

    private void Update()
    {
        if (isMoving)
        {
            UpdateMove();
            return;
        }

        if (!keyboardInputEnabled || Keyboard.current == null)
            return;

        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
            MovePositiveX();
        else if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
            MoveNegativeX();
        else if (Keyboard.current.upArrowKey.wasPressedThisFrame)
            MoveNegativeZ();
        else if (Keyboard.current.downArrowKey.wasPressedThisFrame)
            MovePositiveZ();
    }

    public void MovePositiveX()
    {
        TryMove(Vector2Int.right, Vector3.right);
    }

    public void MoveNegativeX()
    {
        TryMove(Vector2Int.left, Vector3.left);
    }

    public void MovePositiveZ()
    {
        TryMove(Vector2Int.up, Vector3.forward);
    }

    public void MoveNegativeZ()
    {
        TryMove(Vector2Int.down, Vector3.back);
    }

    public void ConfigureBoardMovement(bool enabled, bool keyboardEnabled)
    {
        useBoardManager = enabled;
        keyboardInputEnabled = keyboardEnabled;
        boardObject = GetComponent<BoardObject>();
        boardManager = FindAnyObjectByType<BoardManager>();
    }

    private void TryMove(Vector2Int boardDirection, Vector3 fallbackWorldDirection)
    {
        if (isMoving)
            return;

        moveStart = transform.position;

        if (useBoardManager && TryMoveOnBoard(boardDirection, out var targetTile))
        {
            var safeBoardManager = boardManager != null ? boardManager : FindAnyObjectByType<BoardManager>();
            moveTarget = safeBoardManager.TileToWorld(targetTile, moveStart.y);
            RegisterSuccessfulInteraction();
        }
        else if (useBoardManager)
        {
            return;
        }
        else
        {
            moveTarget = moveStart + fallbackWorldDirection * tileSize;
        }

        moveElapsed = 0f;
        isMoving = true;
    }

    private void RegisterSuccessfulInteraction()
    {
        if (boardObject != null && boardObject.ObjectType == BoardObjectType.Player)
            return;

        if (interactionFlowManager == null)
            interactionFlowManager = FindAnyObjectByType<InteractionFlowManager>();

        if (interactionFlowManager != null)
            interactionFlowManager.RegisterInteraction();
    }

    private bool TryMoveOnBoard(Vector2Int boardDirection, out Vector2Int targetTile)
    {
        targetTile = default;

        if (boardObject == null)
            boardObject = GetComponent<BoardObject>();

        if (boardManager == null)
            boardManager = FindAnyObjectByType<BoardManager>();

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
        }
    }
}
