using UnityEngine;

#pragma warning disable 0649 // Unity assigns serialized fields from prefabs.

public class SpikeToggle : MonoBehaviour
{
    [SerializeField] private Transform spikes;
    [SerializeField] private float raisedOffset = 1f;
    [SerializeField] private float moveDuration = 0.2f;
    [SerializeField] private bool startRaised;

    private Vector3 loweredLocalPosition;
    private Vector3 raisedLocalPosition;
    private Vector3 moveStart;
    private Vector3 moveTarget;
    private float moveElapsed;
    private bool isMoving;
    private bool isRaised;

    private void Awake()
    {
        if (spikes == null)
            spikes = transform.Find("spikes") ?? transform.Find("Spikes");

        if (spikes == null)
            return;

        CachePositions();
        isRaised = startRaised;
        spikes.localPosition = isRaised ? raisedLocalPosition : loweredLocalPosition;
    }

    private void Update()
    {
        if (!isMoving || spikes == null)
            return;

        moveElapsed += Time.deltaTime;
        var duration = Mathf.Max(0.01f, moveDuration);
        var t = Mathf.Clamp01(moveElapsed / duration);
        t = Mathf.SmoothStep(0f, 1f, t);

        spikes.localPosition = Vector3.Lerp(moveStart, moveTarget, t);

        if (moveElapsed >= duration)
        {
            spikes.localPosition = moveTarget;
            isMoving = false;
        }
    }

    public void ToggleSpikes()
    {
        if (spikes == null)
            return;

        isRaised = !isRaised;
        moveStart = spikes.localPosition;
        moveTarget = isRaised ? raisedLocalPosition : loweredLocalPosition;
        moveElapsed = 0f;
        isMoving = true;
    }

    public void Configure(Transform spikesTransform)
    {
        spikes = spikesTransform;
        if (spikes != null)
        {
            CachePositions();
            spikes.localPosition = isRaised ? raisedLocalPosition : loweredLocalPosition;
        }
    }

    private void CachePositions()
    {
        loweredLocalPosition = spikes.localPosition;
        raisedLocalPosition = loweredLocalPosition + Vector3.up * raisedOffset;
    }
}

#pragma warning restore 0649
