using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoardObject))]
public class BoardPlayerMover : MonoBehaviour
{
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private BoardObject boardObject;
    [SerializeField] private PlayerMovement walkAnimation;
    [SerializeField] private float moveDuration = 0.28f;

    private bool isMoving;
    private Coroutine moveRoutine;

    public bool IsMoving => isMoving;

    private void Awake()
    {
        EnsureReferences();

        var body = GetComponent<Rigidbody>();
        if (body != null)
        {
            body.isKinematic = true;
            body.useGravity = false;
        }
    }

    public void Configure(BoardManager newBoardManager, BoardObject newBoardObject)
    {
        boardManager = newBoardManager;
        boardObject = newBoardObject;
        walkAnimation = GetComponent<PlayerMovement>();
    }

    public bool TryStep(Vector2Int boardDirection)
    {
        if (isMoving)
            return false;

        EnsureReferences();

        if (boardManager == null || boardObject == null)
            return false;

        if (!boardManager.TryMoveObject(boardObject, boardDirection, out var fromTile, out var toTile))
            return false;

        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        moveRoutine = StartCoroutine(MoveToTile(fromTile, toTile));
        return true;
    }

    private IEnumerator MoveToTile(Vector2Int fromTile, Vector2Int toTile)
    {
        isMoving = true;

        var startPosition = transform.position;
        var targetPosition = boardManager.TileToWorld(toTile, startPosition.y);
        var direction = targetPosition - startPosition;

        if (direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

        if (walkAnimation != null)
        {
            if (!walkAnimation.enabled)
                walkAnimation.enabled = true;

            walkAnimation.SetWalking(true);
        }

        var elapsed = 0f;
        var duration = Mathf.Max(0.01f, moveDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        transform.position = targetPosition;

        if (walkAnimation != null)
            walkAnimation.SetWalking(false);

        isMoving = false;
        moveRoutine = null;
    }

    private void EnsureReferences()
    {
        if (boardObject == null)
            boardObject = GetComponent<BoardObject>();

        if (boardManager == null)
            boardManager = FindAnyObjectByType<BoardManager>();

        if (walkAnimation == null)
            walkAnimation = GetComponent<PlayerMovement>();
    }
}
