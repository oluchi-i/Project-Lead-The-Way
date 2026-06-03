using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#pragma warning disable 0649 // Unity assigns serialized fields from scenes.

public class PauseMenuUI : MonoBehaviour
{
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button musicToggleButton;
    [SerializeField] private Image musicToggleIcon;
    [SerializeField] private GameplayMusicController musicController;
    [SerializeField] private GameObject pauseOverlay;
    [SerializeField] private CanvasGroup overlayCanvasGroup;
    [SerializeField] private float fadeDuration = 0.14f;

    private Coroutine fadeRoutine;
    private bool isPaused;
    private float previousTimeScale = 1f;

    private void Awake()
    {
        EnsureReferences();

        if (pauseButton != null)
            pauseButton.onClick.AddListener(Pause);

        if (resumeButton != null)
            resumeButton.onClick.AddListener(Resume);

        if (closeButton != null)
            closeButton.onClick.AddListener(Resume);

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartLevel);

        if (musicToggleButton != null)
            musicToggleButton.onClick.AddListener(ToggleMusic);

        if (musicController != null)
            musicController.MuteStateChanged += HandleMusicMuteStateChanged;

        UpdateMusicToggleVisual();
        SetOverlayVisible(false, true);
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (!keyboard.escapeKey.wasPressedThisFrame && !keyboard.pKey.wasPressedThisFrame)
            return;

        if (isPaused)
            Resume();
        else
            Pause();
    }

    private void OnDestroy()
    {
        if (isPaused)
            Time.timeScale = previousTimeScale;

        if (musicController != null)
            musicController.MuteStateChanged -= HandleMusicMuteStateChanged;
    }

    public void Configure(
        Button newPauseButton,
        Button newResumeButton,
        Button newRestartButton,
        Button newCloseButton,
        Button newMusicToggleButton,
        Image newMusicToggleIcon,
        GameplayMusicController newMusicController,
        GameObject newPauseOverlay,
        CanvasGroup newOverlayCanvasGroup)
    {
        pauseButton = newPauseButton;
        resumeButton = newResumeButton;
        restartButton = newRestartButton;
        closeButton = newCloseButton;
        musicToggleButton = newMusicToggleButton;
        musicToggleIcon = newMusicToggleIcon;
        musicController = newMusicController;
        pauseOverlay = newPauseOverlay;
        overlayCanvasGroup = newOverlayCanvasGroup;
    }

    public void Pause()
    {
        if (isPaused)
            return;

        previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
        isPaused = true;
        SetOverlayVisible(true, false);
    }

    public void Resume()
    {
        if (!isPaused)
            return;

        Time.timeScale = previousTimeScale;
        isPaused = false;
        SetOverlayVisible(false, false);
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        isPaused = false;

        var activeScene = SceneManager.GetActiveScene();
        if (activeScene.buildIndex >= 0)
            SceneManager.LoadScene(activeScene.buildIndex);
        else
            SceneManager.LoadScene(activeScene.name);
    }

    private void ToggleMusic()
    {
        if (musicController == null)
            return;

        musicController.ToggleMuted();
        UpdateMusicToggleVisual();
    }

    private void HandleMusicMuteStateChanged(bool muted)
    {
        UpdateMusicToggleVisual();
    }

    private void EnsureReferences()
    {
        if (pauseOverlay == null)
        {
            var overlay = transform.Find("Pause Overlay");
            if (overlay != null)
                pauseOverlay = overlay.gameObject;
        }

        if (overlayCanvasGroup == null && pauseOverlay != null)
            overlayCanvasGroup = pauseOverlay.GetComponent<CanvasGroup>();

        if (overlayCanvasGroup == null && pauseOverlay != null)
            overlayCanvasGroup = pauseOverlay.AddComponent<CanvasGroup>();

        if (musicController == null)
            musicController = FindAnyObjectByType<GameplayMusicController>();
    }

    private void UpdateMusicToggleVisual()
    {
        if (musicToggleIcon == null || musicController == null)
            return;

        musicToggleIcon.color = musicController.IsMuted
            ? new Color(0.13f, 0.09f, 0.04f, 0.38f)
            : new Color(0.13f, 0.09f, 0.04f, 1f);
    }

    private void SetOverlayVisible(bool visible, bool immediate)
    {
        EnsureReferences();

        if (pauseOverlay == null || overlayCanvasGroup == null)
            return;

        pauseOverlay.SetActive(true);
        overlayCanvasGroup.interactable = visible;
        overlayCanvasGroup.blocksRaycasts = visible;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        if (immediate)
        {
            overlayCanvasGroup.alpha = visible ? 1f : 0f;
            pauseOverlay.SetActive(visible);
            return;
        }

        fadeRoutine = StartCoroutine(FadeOverlay(visible));
    }

    private IEnumerator FadeOverlay(bool visible)
    {
        var startAlpha = overlayCanvasGroup.alpha;
        var targetAlpha = visible ? 1f : 0f;
        var elapsed = 0f;
        var duration = Mathf.Max(0.01f, fadeDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);
            overlayCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        overlayCanvasGroup.alpha = targetAlpha;
        pauseOverlay.SetActive(visible);
        fadeRoutine = null;
    }
}

#pragma warning restore 0649
