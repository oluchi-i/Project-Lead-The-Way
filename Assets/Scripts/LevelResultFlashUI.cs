using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class LevelResultFlashUI : MonoBehaviour
{
    [SerializeField] private Image overlay;
    [SerializeField] private Color successColor = new Color(0.15f, 1f, 0.28f, 0.58f);
    [SerializeField] private Color failureColor = new Color(1f, 0.08f, 0.08f, 0.62f);
    [SerializeField] private float flashDuration = 0.18f;

    private Coroutine flashRoutine;

    private void Awake()
    {
        EnsureReferences();
        SetAlpha(0f);
    }

    public void Configure(Image newOverlay)
    {
        overlay = newOverlay;
        EnsureReferences();
        SetAlpha(0f);
    }

    public void Flash(bool success)
    {
        EnsureReferences();

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine(success ? successColor : failureColor));
    }

    private IEnumerator FlashRoutine(Color color)
    {
        overlay.enabled = true;
        overlay.color = color;
        yield return null;

        var elapsed = 0f;
        var duration = Mathf.Max(0.01f, flashDuration);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            var nextColor = color;
            nextColor.a = Mathf.Lerp(color.a, 0f, t);
            overlay.color = nextColor;
            yield return null;
        }

        SetAlpha(0f);
        flashRoutine = null;
    }

    private void EnsureReferences()
    {
        if (overlay == null)
            overlay = GetComponent<Image>();

        overlay.raycastTarget = false;
    }

    private void SetAlpha(float alpha)
    {
        if (overlay == null)
            return;

        var color = overlay.color;
        color.a = alpha;
        overlay.color = color;
        overlay.enabled = alpha > 0f;
    }
}
