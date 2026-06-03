using System.Collections;
using UnityEngine;

#pragma warning disable 0649 // Unity assigns serialized fields from scenes.

public class IntroCameraTransition : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform startCameraPose;
    [SerializeField] private Transform gameplayCameraPose;
    [SerializeField] private Transform endingCameraPose;
    [SerializeField] private float startFieldOfView = 60f;
    [SerializeField] private float gameplayFieldOfView = 60f;
    [SerializeField] private float endingFieldOfView = 60f;
    [SerializeField] private float transitionDuration = 1.15f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    public Transform StartCameraPose => startCameraPose;
    public Transform GameplayCameraPose => gameplayCameraPose;
    public Transform EndingCameraPose => endingCameraPose;
    public bool HasEndingPose => endingCameraPose != null;

    private void Awake()
    {
        EnsureCamera();
    }

    public void Configure(Camera cameraToUse, Transform newStartCameraPose, Transform newGameplayCameraPose)
    {
        targetCamera = cameraToUse;
        startCameraPose = newStartCameraPose;
        gameplayCameraPose = newGameplayCameraPose;
    }

    public void ConfigureFieldOfView(float newStartFieldOfView, float newGameplayFieldOfView)
    {
        startFieldOfView = Mathf.Clamp(newStartFieldOfView, 18f, 90f);
        gameplayFieldOfView = Mathf.Clamp(newGameplayFieldOfView, 18f, 90f);
    }

    public void ConfigureEndingPose(Transform newEndingCameraPose, float newEndingFieldOfView)
    {
        endingCameraPose = newEndingCameraPose;
        endingFieldOfView = Mathf.Clamp(newEndingFieldOfView, 18f, 90f);
    }

    public void PlaceAtStart()
    {
        if (!CanUsePose(startCameraPose))
            return;

        ApplyPose(startCameraPose, startFieldOfView);
    }

    public IEnumerator TransitionToGameplay()
    {
        yield return TransitionToPose(gameplayCameraPose, gameplayFieldOfView);
    }

    public IEnumerator TransitionToEnding()
    {
        yield return TransitionToPose(endingCameraPose, endingFieldOfView);
    }

    private bool CanUsePose(Transform pose)
    {
        return EnsureCamera() && pose != null;
    }

    private bool EnsureCamera()
    {
        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();

        if (targetCamera == null)
            targetCamera = Camera.main;

        return targetCamera != null;
    }

    private void ApplyPose(Transform pose, float fieldOfView)
    {
        targetCamera.transform.position = pose.position;
        targetCamera.transform.rotation = pose.rotation;
        targetCamera.fieldOfView = Mathf.Clamp(fieldOfView, 18f, 90f);
    }

    private IEnumerator TransitionToPose(Transform pose, float fieldOfView)
    {
        if (!CanUsePose(pose))
            yield break;

        var startPosition = targetCamera.transform.position;
        var startRotation = targetCamera.transform.rotation;
        var startFov = targetCamera.fieldOfView;
        var targetPosition = pose.position;
        var targetRotation = pose.rotation;
        var targetFov = Mathf.Clamp(fieldOfView, 18f, 90f);
        var elapsed = 0f;
        var duration = Mathf.Max(0.01f, transitionDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            t = transitionCurve != null ? transitionCurve.Evaluate(t) : Mathf.SmoothStep(0f, 1f, t);

            targetCamera.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            targetCamera.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            targetCamera.fieldOfView = Mathf.Lerp(startFov, targetFov, t);
            yield return null;
        }

        ApplyPose(pose, fieldOfView);
    }
}

#pragma warning restore 0649
