using System.Collections;
using UnityEngine;

#pragma warning disable 0649 // Unity assigns serialized fields from scenes.

public class DeathCinematicCamera : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private bool useOppositeBoardSideOffset = true;
    [SerializeField] private float approachDuration = 0.42f;
    [SerializeField] private float holdBeforeDeathDuration = 0.16f;
    [SerializeField] private float closeShotDistance = 2.2f;
    [SerializeField] private float closeShotHeight = 1.05f;
    [SerializeField] private float sideOffset = 0.8f;
    [SerializeField] private bool keepShotOnCurrentCameraSide = true;
    [SerializeField] private float minimumCameraSideDistance = 0.7f;
    [SerializeField] private float lookAtHeight = 0.85f;
    [SerializeField] private float cinematicFieldOfView = 34f;
    [SerializeField] private float fatalMoveDurationMultiplier = 2.7f;
    [SerializeField] private float fatalAnimationSpeedMultiplier = 0.42f;
    [SerializeField] private AnimationCurve approachCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Vector3 lockedCameraSideDirection;
    private bool hasLockedCameraSideDirection;

    public float FatalMoveDurationMultiplier => Mathf.Max(1f, fatalMoveDurationMultiplier);
    public float FatalAnimationSpeedMultiplier => Mathf.Clamp(fatalAnimationSpeedMultiplier, 0.05f, 1f);

    private void Awake()
    {
        EnsureCamera();
    }

    public void Configure(Camera cameraToUse)
    {
        targetCamera = cameraToUse;
    }

    public void Configure(Camera cameraToUse, BoardManager boardManagerToUse)
    {
        targetCamera = cameraToUse;
        boardManager = boardManagerToUse;
    }

    public IEnumerator MoveIntoShot(Transform player)
    {
        if (player == null || !EnsureCamera())
            yield break;

        CaptureShotSideDirection(player);
        CalculateShot(player, out var targetPosition, out var targetRotation);

        var startPosition = targetCamera.transform.position;
        var startRotation = targetCamera.transform.rotation;
        var startFieldOfView = targetCamera.fieldOfView;
        var targetFieldOfView = Mathf.Clamp(cinematicFieldOfView, 18f, 80f);
        var elapsed = 0f;
        var duration = Mathf.Max(0.01f, approachDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            t = approachCurve != null ? approachCurve.Evaluate(t) : Mathf.SmoothStep(0f, 1f, t);

            targetCamera.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            targetCamera.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            targetCamera.fieldOfView = Mathf.Lerp(startFieldOfView, targetFieldOfView, t);
            yield return null;
        }

        ApplyShot(player);

        if (holdBeforeDeathDuration > 0f)
            yield return new WaitForSeconds(holdBeforeDeathDuration);
    }

    public void ClearShotLock()
    {
        hasLockedCameraSideDirection = false;
    }

    public void ApplyShot(Transform player)
    {
        if (player == null || !EnsureCamera())
            return;

        CalculateShot(player, out var targetPosition, out var targetRotation);
        targetCamera.transform.position = targetPosition;
        targetCamera.transform.rotation = targetRotation;
        targetCamera.fieldOfView = Mathf.Clamp(cinematicFieldOfView, 18f, 80f);
    }

    private bool EnsureCamera()
    {
        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();

        if (targetCamera == null)
            targetCamera = Camera.main;

        return targetCamera != null;
    }

    private void CalculateShot(Transform player, out Vector3 shotPosition, out Quaternion shotRotation)
    {
        var lookTarget = player.position + Vector3.up * Mathf.Max(0f, lookAtHeight);
        var cameraSideDirection = hasLockedCameraSideDirection
            ? lockedCameraSideDirection
            : CalculateCameraSideDirection(player, lookTarget);
        var sideDirection = CalculateSideDirection(player, cameraSideDirection);
        var flatOffset = cameraSideDirection * Mathf.Max(0.2f, closeShotDistance)
            + sideDirection * Mathf.Abs(sideOffset);

        if (keepShotOnCurrentCameraSide)
            flatOffset = ClampToCurrentCameraSide(flatOffset, cameraSideDirection);

        shotPosition = lookTarget
            + flatOffset
            + Vector3.up * closeShotHeight;
        shotRotation = Quaternion.LookRotation(lookTarget - shotPosition, Vector3.up);
    }

    private void CaptureShotSideDirection(Transform player)
    {
        var lookTarget = player.position + Vector3.up * Mathf.Max(0f, lookAtHeight);
        lockedCameraSideDirection = CalculateCameraSideDirection(player, lookTarget);
        hasLockedCameraSideDirection = true;
    }

    private Vector3 CalculateCameraSideDirection(Transform player, Vector3 lookTarget)
    {
        var direction = targetCamera != null
            ? targetCamera.transform.position - lookTarget
            : -player.forward;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            direction = -player.forward;

        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector3.back;

        return direction.normalized;
    }

    private Vector3 ClampToCurrentCameraSide(Vector3 flatOffset, Vector3 cameraSideDirection)
    {
        var minimumDistance = Mathf.Max(0.05f, minimumCameraSideDistance);
        var currentSideDistance = Vector3.Dot(flatOffset, cameraSideDirection);
        if (currentSideDistance >= minimumDistance)
            return flatOffset;

        return flatOffset + cameraSideDirection * (minimumDistance - currentSideDistance);
    }

    private Vector3 CalculateSideDirection(Transform player, Vector3 cameraSideDirection)
    {
        if (!useOppositeBoardSideOffset)
            return Vector3.Cross(Vector3.up, cameraSideDirection).normalized * Mathf.Sign(sideOffset == 0f ? 1f : sideOffset);

        EnsureBoardManager();
        var desiredSide = CalculateOppositeBoardSide(player);
        var projectedSide = Vector3.ProjectOnPlane(desiredSide, cameraSideDirection);
        if (projectedSide.sqrMagnitude < 0.0001f)
            projectedSide = Vector3.Cross(Vector3.up, cameraSideDirection);

        return projectedSide.normalized;
    }

    private Vector3 CalculateOppositeBoardSide(Transform player)
    {
        var boardCenterX = boardManager != null
            ? boardManager.WorldOrigin.x + (boardManager.BoardSize.x - 1) * boardManager.TileSize * 0.5f
            : 0f;
        var playerIsLeftOfBoard = player.position.x <= boardCenterX;
        return playerIsLeftOfBoard ? Vector3.right : Vector3.left;
    }

    private void EnsureBoardManager()
    {
        if (boardManager == null)
            boardManager = FindAnyObjectByType<BoardManager>();
    }
}

#pragma warning restore 0649
