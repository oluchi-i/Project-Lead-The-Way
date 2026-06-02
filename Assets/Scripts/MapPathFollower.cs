using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public sealed class MapPathFollower : MonoBehaviour
{
    [SerializeField] private MapPath path;
    [SerializeField, Min(0.01f)] private float moveDuration = 1.5f;
    [SerializeField] private bool rotateAlongPath = true;
    [SerializeField] private Transform lookAtTarget;
    [SerializeField, Min(0.01f)] private float rotationSpeed = 12f;
    [SerializeField] private bool turnBeforeMove;
    [SerializeField, Min(0.01f)] private float turnDuration = 0.28f;
    [SerializeField] private Vector3 initialFacingDirection;
    [SerializeField] private PlayerMovement movementAnimation;
    [SerializeField] private UnityEvent checkpointReached = new UnityEvent();

    private Coroutine activeMove;
    private float currentProgress;

    public bool IsMoving => activeMove != null;

    private void Start()
    {
        if (path != null)
            ApplyProgress(currentProgress, true);
    }

    public void Configure(MapPath newPath)
    {
        path = newPath;
    }

    public void ConfigureLookTarget(Transform target)
    {
        lookAtTarget = target;
    }

    public void ConfigureMovementAnimation(PlayerMovement animation)
    {
        movementAnimation = animation;
    }

    public void ConfigureMovementStyle(bool shouldTurnBeforeMove, Vector3 startFacingDirection)
    {
        turnBeforeMove = shouldTurnBeforeMove;
        initialFacingDirection = startFacingDirection;
    }

    public void MoveToCheckpoint(int checkpointIndex)
    {
        if (path == null || !path.TryGetCheckpointProgress(checkpointIndex, out var targetProgress))
            return;

        MoveToProgress(targetProgress);
    }

    public void MoveToProgress(float targetProgress)
    {
        if (path == null)
            return;

        if (activeMove != null)
        {
            StopCoroutine(activeMove);
            movementAnimation?.SetWalking(false);
        }

        activeMove = StartCoroutine(MoveRoutine(Mathf.Clamp01(targetProgress)));
    }

    public void JumpToCheckpoint(int checkpointIndex)
    {
        if (path == null || !path.TryGetCheckpointProgress(checkpointIndex, out var targetProgress))
            return;

        if (activeMove != null)
        {
            StopCoroutine(activeMove);
            activeMove = null;
        }

        movementAnimation?.SetWalking(false);
        currentProgress = Mathf.Clamp01(targetProgress);
        ApplyProgress(currentProgress, true);
    }

    private IEnumerator MoveRoutine(float targetProgress)
    {
        var startProgress = currentProgress;
        var travelDirection = GetSegmentFacingDirection(startProgress, targetProgress);
        var elapsed = 0f;

        if (turnBeforeMove && lookAtTarget == null)
            yield return TurnTowardRoutine(travelDirection);

        movementAnimation?.SetWalking(true);

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.SmoothStep(0f, 1f, elapsed / moveDuration);
            currentProgress = Mathf.Lerp(startProgress, targetProgress, t);
            ApplyProgress(currentProgress, false, travelDirection);
            yield return null;
        }

        currentProgress = targetProgress;
        var finalFacingDirection = GetArrivalFacingDirection(targetProgress, travelDirection);
        ApplyProgress(currentProgress, false, finalFacingDirection);
        if (turnBeforeMove && lookAtTarget == null && IsStartProgress(targetProgress))
            yield return TurnTowardRoutine(finalFacingDirection);

        activeMove = null;
        movementAnimation?.SetWalking(false);
        checkpointReached?.Invoke();
    }

    private IEnumerator TurnTowardRoutine(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0.0001f)
            yield break;

        var startRotation = transform.rotation;
        var targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        var elapsed = 0f;

        while (elapsed < turnDuration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.SmoothStep(0f, 1f, elapsed / turnDuration);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        transform.rotation = targetRotation;
    }

    private void OnDisable()
    {
        movementAnimation?.SetWalking(false);
    }

    private void ApplyProgress(float progress, bool snapRotation)
    {
        ApplyProgress(progress, snapRotation, GetDefaultFacingDirection(progress));
    }

    private void ApplyProgress(float progress, bool snapRotation, Vector3 facingDirection)
    {
        transform.position = path.GetPoint(progress);
        if (lookAtTarget != null)
        {
            RotateToward(lookAtTarget.position - transform.position, snapRotation);
            return;
        }

        if (!rotateAlongPath)
            return;

        RotateToward(facingDirection, snapRotation);
    }

    private Vector3 GetDefaultFacingDirection(float progress)
    {
        if (lookAtTarget == null && initialFacingDirection.sqrMagnitude > 0.0001f && IsStartProgress(progress))
            return initialFacingDirection.normalized;

        return path.GetDirection(progress);
    }

    private Vector3 GetSegmentFacingDirection(float startProgress, float targetProgress)
    {
        var startPoint = path.GetPoint(startProgress);
        var targetPoint = path.GetPoint(targetProgress);
        var direction = targetPoint - startPoint;
        if (direction.sqrMagnitude <= 0.0001f)
            direction = path.GetDirection(startProgress);

        return direction.normalized;
    }

    private Vector3 GetArrivalFacingDirection(float targetProgress, Vector3 travelDirection)
    {
        if (lookAtTarget == null && initialFacingDirection.sqrMagnitude > 0.0001f && IsStartProgress(targetProgress))
            return initialFacingDirection.normalized;

        return travelDirection;
    }

    private static bool IsStartProgress(float progress)
    {
        return progress <= 0.0001f;
    }

    private void RotateToward(Vector3 direction, bool snapRotation)
    {
        if (direction.sqrMagnitude <= 0.0001f)
            return;

        var targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = snapRotation
            ? targetRotation
            : Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
    }
}
