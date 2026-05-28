using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ButtonPress : MonoBehaviour
{
    [SerializeField] private Transform cap;
    [SerializeField] private float pressDepth = 0.18f;
    [SerializeField] private float pressDuration = 0.08f;
    [SerializeField] private float releaseDuration = 0.14f;
    [SerializeField] private AudioClip pressSound;
    [SerializeField] private float soundVolume = 1f;
    [SerializeField] private AudioSource audioSource;

    private Vector3 raisedLocalPosition;
    private Vector3 pressedLocalPosition;
    private Vector3 moveStart;
    private Vector3 moveTarget;
    private float moveElapsed;
    private float currentDuration;
    private bool isMoving;
    private bool isReleasing;

    private void Awake()
    {
        if (cap == null)
            cap = transform.Find("Cap") ?? transform.Find("cap");

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.2f;
        }

        if (cap == null)
            return;

        CachePositions();
    }

    private void Update()
    {
        if (!isMoving || cap == null)
            return;

        moveElapsed += Time.deltaTime;
        var duration = Mathf.Max(0.01f, currentDuration);
        var t = Mathf.Clamp01(moveElapsed / duration);
        t = Mathf.SmoothStep(0f, 1f, t);

        cap.localPosition = Vector3.Lerp(moveStart, moveTarget, t);

        if (moveElapsed < duration)
            return;

        cap.localPosition = moveTarget;

        if (isReleasing)
        {
            isMoving = false;
            isReleasing = false;
            return;
        }

        StartMove(raisedLocalPosition, releaseDuration, true);
    }

    public void PressButton()
    {
        if (cap == null || isMoving)
            return;

        if (pressSound != null && audioSource != null)
            audioSource.PlayOneShot(pressSound, soundVolume);

        StartMove(pressedLocalPosition, pressDuration, false);
    }

    public void Configure(Transform capTransform, AudioClip soundClip)
    {
        cap = capTransform;
        pressSound = soundClip;
        if (cap != null)
            CachePositions();
    }

    private void StartMove(Vector3 target, float duration, bool releasing)
    {
        moveStart = cap.localPosition;
        moveTarget = target;
        moveElapsed = 0f;
        currentDuration = duration;
        isReleasing = releasing;
        isMoving = true;
    }

    private void CachePositions()
    {
        raisedLocalPosition = cap.localPosition;
        pressedLocalPosition = raisedLocalPosition + Vector3.down * pressDepth;
    }
}
