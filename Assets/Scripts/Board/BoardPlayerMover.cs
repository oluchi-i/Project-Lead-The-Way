using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoardObject))]
public class BoardPlayerMover : MonoBehaviour
{
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private BoardObject boardObject;
    [SerializeField] private PlayerMovement walkAnimation;
    [SerializeField] private float rotateDuration = 0.16f;
    [SerializeField] private float moveDuration = 0.28f;

    private bool isMoving;
    private Coroutine moveRoutine;
    private bool loggedMissingReferences;

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

        moveRoutine = StartCoroutine(MoveToTile(fromTile, toTile, false, 1f, 1f));
        return true;
    }

    public IEnumerator PlayCinematicStep(Vector2Int boardDirection, float durationMultiplier, float animationSpeedMultiplier)
    {
        if (isMoving)
            yield break;

        EnsureReferences();

        if (boardManager == null || boardObject == null)
            yield break;

        if (!boardManager.TryMoveObject(boardObject, boardDirection, out var fromTile, out var toTile))
            yield break;

        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        var move = MoveToTile(fromTile, toTile, false, durationMultiplier, animationSpeedMultiplier);
        while (move.MoveNext())
            yield return move.Current;
    }

    public void PlaceAtTile(Vector2Int tile)
    {
        EnsureReferences();

        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        isMoving = false;

        if (boardObject != null)
            boardObject.SetTilePosition(tile);

        if (boardManager != null)
        {
            transform.position = boardManager.TileToWorld(tile, transform.position.y);
            boardManager.RebuildRegistry();
        }
    }

    public IEnumerator PlayScriptedPath(IReadOnlyList<Vector2Int> tiles)
    {
        if (isMoving || tiles == null || tiles.Count == 0)
            yield break;

        EnsureReferences();

        if (boardManager == null || boardObject == null)
            yield break;

        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        for (var i = 0; i < tiles.Count; i++)
        {
            var fromTile = boardObject.TilePosition;
            var toTile = tiles[i];
            if (fromTile == toTile)
                continue;

            yield return MoveToTile(fromTile, toTile, true, 1f, 1f);
        }

        boardManager.RebuildRegistry();
        moveRoutine = null;
    }

    private IEnumerator MoveToTile(Vector2Int fromTile, Vector2Int toTile, bool updateBoardPosition, float durationMultiplier, float animationSpeedMultiplier)
    {
        isMoving = true;

        var startPosition = transform.position;
        var targetPosition = boardManager.TileToWorld(toTile, startPosition.y);
        var direction = targetPosition - startPosition;

        if (direction.sqrMagnitude > 0.0001f)
        {
            var rotation = RotateToward(direction.normalized);
            while (rotation.MoveNext())
                yield return rotation.Current;
        }

        var originalWalkSpeed = 1f;
        var changedWalkSpeed = false;
        if (walkAnimation != null)
        {
            if (!walkAnimation.enabled)
                walkAnimation.enabled = true;

            originalWalkSpeed = walkAnimation.speed;
            walkAnimation.speed = originalWalkSpeed * Mathf.Clamp(animationSpeedMultiplier, 0.05f, 1f);
            changedWalkSpeed = true;
            walkAnimation.SetWalking(true);
        }

        var elapsed = 0f;
        var duration = Mathf.Max(0.01f, moveDuration * Mathf.Max(1f, durationMultiplier));

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        transform.position = targetPosition;

        if (updateBoardPosition)
        {
            boardObject.SetTilePosition(toTile);
            boardManager.RebuildRegistry();
        }

        if (walkAnimation != null)
        {
            walkAnimation.SetWalking(false);

            if (changedWalkSpeed)
                walkAnimation.speed = originalWalkSpeed;
        }
        
        isMoving = false;
        moveRoutine = null;
    }

    private IEnumerator RotateToward(Vector3 direction)
    {
        var targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        if (Quaternion.Angle(transform.rotation, targetRotation) < 0.5f)
        {
            transform.rotation = targetRotation;
            yield break;
        }

        var startRotation = transform.rotation;
        var elapsed = 0f;
        var duration = Mathf.Max(0.01f, rotateDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        transform.rotation = targetRotation;
    }

    private void EnsureReferences()
    {
        if (boardObject == null)
            boardObject = GetComponent<BoardObject>();

        if (walkAnimation == null)
            walkAnimation = GetComponent<PlayerMovement>();

        if (!loggedMissingReferences && (boardManager == null || boardObject == null))
        {
            loggedMissingReferences = true;
            Debug.LogWarning("BoardPlayerMover is missing required scene references. Run Tools > Lead The Way > Scene > Wire Current Scene References.", this);
        }
    }
}
