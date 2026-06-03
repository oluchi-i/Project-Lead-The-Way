using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public sealed class MapPathFollower : MonoBehaviour
{
    private const float DefaultMoveDuration = 1.5f;
    private const float DirectionLookAhead = 0.01f;

    [SerializeField] private MapPath path;
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

    public void MoveToCheckpoint(int checkpointIndex, float duration)
    {
        if (path == null || !path.TryGetCheckpointProgress(checkpointIndex, out var targetProgress))
            return;

        MoveToProgress(targetProgress, duration);
    }

    public void MoveToProgress(float targetProgress)
    {
        MoveToProgress(targetProgress, DefaultMoveDuration);
    }

    public void MoveToProgress(float targetProgress, float duration)
    {
        if (path == null)
            return;

        if (activeMove != null)
        {
            StopCoroutine(activeMove);
            movementAnimation?.SetWalking(false);
        }

        activeMove = StartCoroutine(MoveRoutine(Mathf.Clamp01(targetProgress), Mathf.Max(0.01f, duration)));
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

    private IEnumerator MoveRoutine(float targetProgress, float duration)
    {
        var startProgress = currentProgress;
        var travelDirection = GetCurveFacingDirection(startProgress, startProgress, targetProgress, GetSegmentFacingDirection(startProgress, targetProgress));
        var elapsed = 0f;
        var movementDuration = duration;

        if (turnBeforeMove && lookAtTarget == null)
        {
            var boundedTurnDuration = Mathf.Min(turnDuration, duration * 0.4f);
            movementDuration = Mathf.Max(0.01f, duration - boundedTurnDuration);
            yield return TurnTowardRoutine(travelDirection, boundedTurnDuration);
        }

        movementAnimation?.SetWalking(true);

        while (elapsed < movementDuration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.SmoothStep(0f, 1f, elapsed / movementDuration);
            currentProgress = Mathf.Lerp(startProgress, targetProgress, t);
            var curveDirection = GetCurveFacingDirection(currentProgress, startProgress, targetProgress, travelDirection);
            ApplyProgress(currentProgress, false, curveDirection);
            travelDirection = curveDirection;
            yield return null;
        }

        currentProgress = targetProgress;
        var finalFacingDirection = GetArrivalFacingDirection(targetProgress, startProgress, travelDirection);
        ApplyProgress(currentProgress, false, finalFacingDirection);
        if (turnBeforeMove && lookAtTarget == null && IsStartProgress(targetProgress))
            RotateToward(finalFacingDirection, true);

        activeMove = null;
        movementAnimation?.SetWalking(false);
        checkpointReached?.Invoke();
    }

    private IEnumerator TurnTowardRoutine(Vector3 direction, float duration)
    {
        if (direction.sqrMagnitude <= 0.0001f)
            yield break;

        var startRotation = transform.rotation;
        var targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        var elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        transform.rotation = targetRotation;
    }

    private void OnDisable()
    {
        movementAnimation?.SetWalking(false);
    }

    private void LateUpdate()
    {
        if (activeMove == null)
            RefreshLookAt(true);
    }

    public void RefreshLookAt(bool snapRotation)
    {
        if (lookAtTarget == null)
            return;

        RotateToward(lookAtTarget.position - transform.position, snapRotation);
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

    private Vector3 GetArrivalFacingDirection(float targetProgress, float startProgress, Vector3 travelDirection)
    {
        if (lookAtTarget == null && initialFacingDirection.sqrMagnitude > 0.0001f && IsStartProgress(targetProgress))
            return initialFacingDirection.normalized;

        return GetCurveFacingDirection(targetProgress, startProgress, targetProgress, travelDirection);
    }

    private Vector3 GetCurveFacingDirection(float progress, float startProgress, float targetProgress, Vector3 fallbackDirection)
    {
        var travelSign = targetProgress >= startProgress ? 1f : -1f;
        var sampleProgress = Mathf.Clamp01(progress + (DirectionLookAhead * travelSign));
        var direction = path.GetPoint(sampleProgress) - path.GetPoint(progress);
        if (direction.sqrMagnitude <= 0.0001f)
        {
            sampleProgress = Mathf.Clamp01(progress - (DirectionLookAhead * travelSign));
            direction = path.GetPoint(progress) - path.GetPoint(sampleProgress);
        }

        if (direction.sqrMagnitude <= 0.0001f)
            direction = fallbackDirection;

        direction.y = 0f;
        return direction.sqrMagnitude > 0.0001f ? direction.normalized : fallbackDirection;
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
