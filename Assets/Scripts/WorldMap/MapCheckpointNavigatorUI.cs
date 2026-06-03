using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using EasyTransition;

public sealed class MapCheckpointNavigatorUI : MonoBehaviour
{
    [SerializeField] private MapPath cameraPath;
    [SerializeField] private MapPath playerPath;
    [SerializeField] private MapPathFollower cameraFollower;
    [SerializeField] private MapPathFollower playerFollower;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button startButton;
    [SerializeField] private Button playButton;
    [SerializeField] private Text checkpointLabel;
    [SerializeField] private Image panelBackground;
    [SerializeField] private Image titleBackground;
    [SerializeField] private WorldMapTeleportEffect teleportEffect;
    [SerializeField] private string levelSceneName = "Level01";
    [SerializeField] private TransitionManager transitionManager;
    [SerializeField] private TransitionSettings sceneTransition;
    [SerializeField, Min(0f)] private float sceneTransitionDelay = 0f;
    [SerializeField] private int currentCheckpointIndex;
    [Header("Timing")]
    [Tooltip("Central duration used when the camera and player move between map checkpoints.")]
    [SerializeField, Min(0.01f)] private float transitionDuration = 1.5f;
    [SerializeField] private bool snapFollowersToCurrentCheckpointOnStart = true;
    [SerializeField] private bool enforceCameraFollowerAsMainCamera = true;
    [SerializeField] private UnityEvent levelPlayRequested = new UnityEvent();

    private bool waitingForMove;
    private bool mapStarted;
    private bool introMoveInProgress;
    private bool loadingScene;
    private bool returnedFromLevel;

    private void Awake()
    {
        if (previousButton != null)
            previousButton.onClick.AddListener(MovePrevious);
        if (nextButton != null)
            nextButton.onClick.AddListener(MoveNext);
        if (startButton != null)
            startButton.onClick.AddListener(StartFirstLevel);
        if (playButton != null)
            playButton.onClick.AddListener(PlayCurrentLevel);
    }

    private void Start()
    {
        ApplyReturnCheckpointState();

        if (enforceCameraFollowerAsMainCamera)
            EnforceCameraFollowerAsMainCamera();

        if (snapFollowersToCurrentCheckpointOnStart)
        {
            cameraFollower?.JumpToCheckpoint(currentCheckpointIndex);
            playerFollower?.JumpToCheckpoint(currentCheckpointIndex);
        }

        Refresh();
        if (returnedFromLevel && teleportEffect != null)
            StartCoroutine(teleportEffect.PlayAppearanceAndWait());
    }

    public void ConfigureSceneTransition(TransitionManager manager, TransitionSettings transition)
    {
        transitionManager = manager;
        sceneTransition = transition;
    }

    public void ConfigureTeleportEffect(WorldMapTeleportEffect newTeleportEffect)
    {
        teleportEffect = newTeleportEffect;
    }

    private void Update()
    {
        CompleteMoveIfReady();
        HandleKeyboardNavigation();
    }

    private void CompleteMoveIfReady()
    {
        if (!waitingForMove || IsMoving())
            return;

        waitingForMove = false;
        cameraFollower?.RefreshLookAt(true);
        if (introMoveInProgress)
        {
            introMoveInProgress = false;
            mapStarted = true;
        }
        else if (currentCheckpointIndex == 0)
        {
            mapStarted = false;
        }

        Refresh();
    }

    private void HandleKeyboardNavigation()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || waitingForMove || IsMoving())
            return;

        if (keyboard.rightArrowKey.wasPressedThisFrame)
        {
            if (!mapStarted && currentCheckpointIndex == 0)
                StartFirstLevel();
            else
                MoveNext();
        }
        else if (keyboard.leftArrowKey.wasPressedThisFrame && mapStarted)
        {
            MovePrevious();
        }
    }

    public void Configure(
        MapPath newCameraPath,
        MapPath newPlayerPath,
        MapPathFollower newCameraFollower,
        MapPathFollower newPlayerFollower,
        Button newPreviousButton,
        Button newNextButton,
        Button newStartButton,
        Button newPlayButton,
        Text newCheckpointLabel,
        Image newPanelBackground,
        Image newTitleBackground,
        WorldMapTeleportEffect newTeleportEffect)
    {
        cameraPath = newCameraPath;
        playerPath = newPlayerPath;
        cameraFollower = newCameraFollower;
        playerFollower = newPlayerFollower;
        previousButton = newPreviousButton;
        nextButton = newNextButton;
        startButton = newStartButton;
        playButton = newPlayButton;
        checkpointLabel = newCheckpointLabel;
        panelBackground = newPanelBackground;
        titleBackground = newTitleBackground;
        teleportEffect = newTeleportEffect;
        Refresh();
    }

    public void StartFirstLevel()
    {
        if (!CanMoveTo(1))
            return;

        introMoveInProgress = true;
        MoveTo(1);
    }

    public void MovePrevious()
    {
        if (CanMoveTo(currentCheckpointIndex - 1))
            MoveTo(currentCheckpointIndex - 1);
    }

    public void MoveNext()
    {
        if (CanMoveTo(currentCheckpointIndex + 1))
            MoveTo(currentCheckpointIndex + 1);
    }

    public void PlayCurrentLevel()
    {
        if (!mapStarted || waitingForMove || IsMoving() || loadingScene)
            return;

        levelPlayRequested?.Invoke();
        StartCoroutine(LoadCurrentLevelRoutine());
    }

    private IEnumerator LoadCurrentLevelRoutine()
    {
        var targetSceneName = GetCurrentLevelSceneName();
        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogWarning("Lead The Way Map: No level scene name is configured on MapCheckpointNavigatorUI.", this);
            yield break;
        }

        Debug.Log("Lead The Way Map: Loading " + targetSceneName + " for " + GetCheckpointText() + ".");
        loadingScene = true;
        Refresh();

        if (teleportEffect != null)
            yield return teleportEffect.PlayDisappearanceAndWait();

        WorldMapProgressState.SetReturnCheckpoint(currentCheckpointIndex);
        SceneTransitionLoader.LoadScene(targetSceneName, transitionManager, sceneTransition, sceneTransitionDelay);
    }

    private void ApplyReturnCheckpointState()
    {
        if (!WorldMapProgressState.HasReturnCheckpoint)
            return;

        currentCheckpointIndex = Mathf.Max(0, WorldMapProgressState.ReturnCheckpointIndex);
        mapStarted = currentCheckpointIndex > 0;
        introMoveInProgress = false;
        waitingForMove = false;
        returnedFromLevel = mapStarted;
        WorldMapProgressState.ClearReturnCheckpoint();
    }

    private void MoveTo(int checkpointIndex)
    {
        currentCheckpointIndex = checkpointIndex;
        cameraFollower?.MoveToCheckpoint(currentCheckpointIndex, transitionDuration);
        playerFollower?.MoveToCheckpoint(currentCheckpointIndex, transitionDuration);
        waitingForMove = true;
        Refresh();
    }

    private bool CanMoveTo(int checkpointIndex)
    {
        if (checkpointIndex < 0 || IsMoving())
            return false;

        if (cameraPath == null || playerPath == null || cameraFollower == null || playerFollower == null)
            return false;

        return checkpointIndex < cameraPath.CheckpointCount && checkpointIndex < playerPath.CheckpointCount;
    }

    private bool IsMoving()
    {
        return (cameraFollower != null && cameraFollower.IsMoving) ||
            (playerFollower != null && playerFollower.IsMoving);
    }

    private void Refresh()
    {
        var isMovingBetweenCheckpoints = waitingForMove || IsMoving();
        var showIntro = !mapStarted && !introMoveInProgress && !isMovingBetweenCheckpoints && currentCheckpointIndex == 0;
        var showLevelNavigation = mapStarted && !introMoveInProgress && !isMovingBetweenCheckpoints;

        if (panelBackground != null)
            panelBackground.enabled = showLevelNavigation;

        if (checkpointLabel != null)
        {
            checkpointLabel.text = GetCheckpointText();
            checkpointLabel.gameObject.SetActive(showLevelNavigation);
        }

        if (titleBackground != null)
            titleBackground.gameObject.SetActive(showLevelNavigation);

        if (startButton != null)
        {
            startButton.gameObject.SetActive(showIntro);
            startButton.interactable = CanMoveTo(1);
        }

        if (previousButton != null)
        {
            previousButton.gameObject.SetActive(showLevelNavigation);
            previousButton.interactable = CanMoveTo(currentCheckpointIndex - 1);
        }

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(showLevelNavigation);
            nextButton.interactable = CanMoveTo(currentCheckpointIndex + 1);
        }

        if (playButton != null)
        {
            playButton.gameObject.SetActive(showLevelNavigation);
            playButton.interactable = showLevelNavigation && !loadingScene;
        }
    }

    private string GetCheckpointText()
    {
        var label = string.Empty;
        if (playerPath != null)
            playerPath.TryGetCheckpointLabel(currentCheckpointIndex, out label);

        if (string.IsNullOrWhiteSpace(label))
            label = currentCheckpointIndex == 0 ? "Start" : "Level " + currentCheckpointIndex;

        return label.ToUpperInvariant();
    }

    private string GetCurrentLevelSceneName()
    {
        if (playerPath != null && playerPath.TryGetCheckpointLevelSceneName(currentCheckpointIndex, out var checkpointSceneName))
            return checkpointSceneName;

        if (cameraPath != null && cameraPath.TryGetCheckpointLevelSceneName(currentCheckpointIndex, out checkpointSceneName))
            return checkpointSceneName;

        if (currentCheckpointIndex > 0)
            return "Level" + currentCheckpointIndex.ToString("00");

        return levelSceneName;
    }

    private void EnforceCameraFollowerAsMainCamera()
    {
        if (cameraFollower == null || !cameraFollower.TryGetComponent<Camera>(out var activeCamera))
            return;

        activeCamera.enabled = true;
        activeCamera.depth = 100f;
        activeCamera.rect = new Rect(0f, 0f, 1f, 1f);
        activeCamera.targetTexture = null;
        activeCamera.gameObject.tag = "MainCamera";

        foreach (var camera in FindObjectsByType<Camera>(FindObjectsInactive.Include))
        {
            if (camera == activeCamera)
                continue;

            camera.enabled = false;
            if (camera.CompareTag("MainCamera"))
                camera.gameObject.tag = "Untagged";
        }
    }
}
