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
            StopCoroutine(activeMove);

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

        currentProgress = Mathf.Clamp01(targetProgress);
        ApplyProgress(currentProgress, true);
    }

    private IEnumerator MoveRoutine(float targetProgress)
    {
        var startProgress = currentProgress;
        var elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.SmoothStep(0f, 1f, elapsed / moveDuration);
            currentProgress = Mathf.Lerp(startProgress, targetProgress, t);
            ApplyProgress(currentProgress, false);
            yield return null;
        }

        currentProgress = targetProgress;
        ApplyProgress(currentProgress, false);
        activeMove = null;
        checkpointReached?.Invoke();
    }

    private void ApplyProgress(float progress, bool snapRotation)
    {
        transform.position = path.GetPoint(progress);
        if (lookAtTarget != null)
        {
            RotateToward(lookAtTarget.position - transform.position, snapRotation);
            return;
        }

        if (!rotateAlongPath)
            return;

        RotateToward(path.GetDirection(progress), snapRotation);
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
