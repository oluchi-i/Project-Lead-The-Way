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

        moveRoutine = StartCoroutine(MoveToTile(fromTile, toTile, false));
        return true;
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

            yield return MoveToTile(fromTile, toTile, true);
        }

        boardManager.RebuildRegistry();
        moveRoutine = null;
    }

    private void CheckCurrentTile()
    {
        BoardObject spike = boardManager.GetObjectsAt(boardObject.TilePosition).Find(obj => obj.ObjectType == BoardObjectType.Hazard);
        Debug.Log(spike);
        if (spike != null)
            Debug.Log(spike.gameObject.GetComponent<SpikeToggle>().IsRaised);
        if (spike != null && spike.gameObject.GetComponent<SpikeToggle>().IsRaised)
        {
            Debug.Log("kill");
            walkAnimation.Kill();
        }
    }

    private IEnumerator MoveToTile(Vector2Int fromTile, Vector2Int toTile, bool updateBoardPosition)
    {
        isMoving = true;

        var startPosition = transform.position;
        var targetPosition = boardManager.TileToWorld(toTile, startPosition.y);
        var direction = targetPosition - startPosition;

        if (direction.sqrMagnitude > 0.0001f)
            yield return RotateToward(direction.normalized);

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

        if (updateBoardPosition)
        {
            boardObject.SetTilePosition(toTile);
            boardManager.RebuildRegistry();
        }

        if (walkAnimation != null)
            walkAnimation.SetWalking(false);
        
        isMoving = false;
        moveRoutine = null;

        CheckCurrentTile();
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
            Debug.LogWarning("BoardPlayerMover is missing required scene references. Run Tools > Lead The Way > Optimize > Wire Current Scene References.", this);
        }
    }
}
