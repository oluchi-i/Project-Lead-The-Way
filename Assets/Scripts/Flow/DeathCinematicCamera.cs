using System.Collections;
using UnityEngine;

#pragma warning disable 0649 // Unity assigns serialized fields from scenes.

public class DeathCinematicCamera : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float approachDuration = 0.42f;
    [SerializeField] private float holdBeforeDeathDuration = 0.16f;
    [SerializeField] private float closeShotDistance = 2.2f;
    [SerializeField] private float closeShotHeight = 1.05f;
    [SerializeField] private float sideOffset = 0.8f;
    [SerializeField] private float lookAtHeight = 0.85f;
    [SerializeField] private float cinematicFieldOfView = 34f;
    [SerializeField] private float fatalMoveDurationMultiplier = 2.7f;
    [SerializeField] private float fatalAnimationSpeedMultiplier = 0.42f;
    [SerializeField] private AnimationCurve approachCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

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

    public IEnumerator MoveIntoShot(Transform player)
    {
        if (player == null || !EnsureCamera())
            yield break;

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
        var forward = player.forward;
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;

        forward.y = 0f;
        forward.Normalize();

        var right = Vector3.Cross(Vector3.up, forward).normalized;
        shotPosition = lookTarget
            - forward * Mathf.Max(0.2f, closeShotDistance)
            + right * sideOffset
            + Vector3.up * closeShotHeight;
        shotRotation = Quaternion.LookRotation(lookTarget - shotPosition, Vector3.up);
    }
}

#pragma warning restore 0649
