using System.Collections;
using UnityEngine;

public sealed class WorldMapTeleportEffect : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private GameObject teleportEffectPrefab;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip teleportSound;
    [SerializeField] private float effectDuration = 1f;
    [SerializeField] private float appearanceDelay = 0.16f;
    [SerializeField] private float appearanceDuration = 0.72f;
    [SerializeField] private float riseOffset = 0.12f;
    [SerializeField] private float yawDegrees = 32f;
    [SerializeField] private float scaleOvershoot = 1.08f;
    [SerializeField] private float heightOffset = 0.04f;
    [SerializeField] private float effectScale = 0.12f;
    [SerializeField] private bool playOnStart = false;

    private Coroutine activeEffect;
    private Transform visualRoot;
    private Vector3 originalVisualScale = Vector3.one;
    private Vector3 originalVisualLocalPosition;
    private Quaternion originalVisualLocalRotation = Quaternion.identity;
    private bool cachedVisualPose;

    private void Start()
    {
        if (playOnStart)
            PlayAtTarget();
    }

    public void Configure(Transform newTarget, GameObject newTeleportEffectPrefab, AudioSource newAudioSource, AudioClip newTeleportSound)
    {
        target = newTarget;
        teleportEffectPrefab = newTeleportEffectPrefab;
        audioSource = newAudioSource;
        teleportSound = newTeleportSound;
    }

    public void PlayAtTarget()
    {
        if (!isActiveAndEnabled || teleportEffectPrefab == null || target == null)
            return;

        if (activeEffect != null)
            StopCoroutine(activeEffect);

        activeEffect = StartCoroutine(PlayEffectRoutine());
    }

    public IEnumerator PlayAtTargetAndWait()
    {
        if (!isActiveAndEnabled || teleportEffectPrefab == null || target == null)
            yield break;

        if (activeEffect != null)
            StopCoroutine(activeEffect);

        yield return PlayEffectRoutine();
    }

    public IEnumerator PlayAppearanceAndWait()
    {
        yield return PlayVisualTransitionRoutine(true);
    }

    public IEnumerator PlayDisappearanceAndWait()
    {
        yield return PlayVisualTransitionRoutine(false);
    }

    private IEnumerator PlayVisualTransitionRoutine(bool appearing)
    {
        if (!isActiveAndEnabled || teleportEffectPrefab == null || target == null)
            yield break;

        if (activeEffect != null)
            StopCoroutine(activeEffect);

        CacheVisualPose();

        PlaySound();
        var effect = SpawnEffect();
        var elapsed = 0f;
        var duration = Mathf.Max(0.01f, effectDuration);
        var transitionDelay = Mathf.Clamp(appearanceDelay, 0f, duration);
        var transitionDuration = Mathf.Max(0.01f, appearanceDuration);
        var transitionEndTime = Mathf.Min(duration, transitionDelay + transitionDuration);

        if (appearing)
        {
            SetVisualVisible(false);
            SetVisualTransition(0f, true);
        }
        else
        {
            SetVisualVisible(true);
            SetVisualTransition(0f, false);
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            if (elapsed >= transitionDelay && elapsed <= transitionEndTime)
            {
                var t = Mathf.Clamp01((elapsed - transitionDelay) / Mathf.Max(0.01f, transitionEndTime - transitionDelay));
                t = Mathf.SmoothStep(0f, 1f, t);

                if (appearing)
                    SetVisualVisible(true);

                SetVisualTransition(t, appearing);
            }

            yield return null;
        }

        SetVisualTransition(1f, appearing);
        SetVisualVisible(appearing);
        DestroyEffect(effect);
        activeEffect = null;
    }

    private IEnumerator PlayEffectRoutine()
    {
        PlaySound();
        var effect = SpawnEffect();

        yield return new WaitForSeconds(Mathf.Max(0.01f, effectDuration));

        DestroyEffect(effect);
        activeEffect = null;
    }

    private GameObject SpawnEffect()
    {
        var position = target.position + Vector3.up * heightOffset;
        var effect = Instantiate(teleportEffectPrefab, position, Quaternion.identity);
        effect.name = teleportEffectPrefab.name + " Map Runtime";
        effect.transform.localScale = Vector3.one * Mathf.Max(0.01f, effectScale);
        return effect;
    }

    private static void DestroyEffect(GameObject effect)
    {
        if (effect != null)
            Destroy(effect);
    }

    private void CacheVisualPose()
    {
        if (cachedVisualPose)
            return;

        visualRoot = FindVisualRoot();
        if (visualRoot == null)
            return;

        originalVisualScale = visualRoot.localScale;
        originalVisualLocalPosition = visualRoot.localPosition;
        originalVisualLocalRotation = visualRoot.localRotation;
        cachedVisualPose = true;
    }

    private Transform FindVisualRoot()
    {
        if (visualRoot != null)
            return visualRoot;

        if (target == null)
            return null;

        var namedVisual = target.Find("Player Visual");
        if (namedVisual != null)
            return namedVisual;

        var movement = target.GetComponentInChildren<PlayerMovement>(true);
        return movement != null ? movement.transform : target;
    }

    private void SetVisualVisible(bool visible)
    {
        CacheVisualPose();
        if (visualRoot == null)
            return;

        foreach (var renderer in visualRoot.GetComponentsInChildren<Renderer>(true))
            renderer.enabled = visible;
    }

    private void SetVisualTransition(float progress, bool appearing)
    {
        CacheVisualPose();
        if (visualRoot == null)
            return;

        var t = Mathf.Clamp01(progress);
        var visibleProgress = appearing ? t : 1f - t;
        var smoothVisibleProgress = Mathf.SmoothStep(0f, 1f, visibleProgress);
        var scaleMultiplier = appearing ? CalculateAppearScale(t) : smoothVisibleProgress;
        var offsetDirection = appearing ? 1f - t : t;
        var yawDirection = appearing ? 1f - t : -t;

        visualRoot.localScale = originalVisualScale * scaleMultiplier;
        visualRoot.localPosition = originalVisualLocalPosition + Vector3.up * riseOffset * offsetDirection;
        visualRoot.localRotation = originalVisualLocalRotation * Quaternion.Euler(0f, yawDegrees * yawDirection, 0f);

        if (t >= 1f)
        {
            visualRoot.localPosition = originalVisualLocalPosition;
            visualRoot.localRotation = originalVisualLocalRotation;
            visualRoot.localScale = appearing ? originalVisualScale : Vector3.zero;
        }
    }

    private float CalculateAppearScale(float progress)
    {
        var t = Mathf.Clamp01(progress);
        var overshoot = Mathf.Max(1f, scaleOvershoot);
        if (t < 0.82f)
            return Mathf.Lerp(0f, overshoot, Mathf.SmoothStep(0f, 1f, t / 0.82f));

        return Mathf.Lerp(overshoot, 1f, Mathf.SmoothStep(0f, 1f, (t - 0.82f) / 0.18f));
    }

    private void PlaySound()
    {
        if (teleportSound == null)
            return;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        audioSource.PlayOneShot(teleportSound);
    }
}
