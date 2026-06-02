using UnityEngine;
using UnityEngine.UI;

public sealed class MapCheckpointNavigatorUI : MonoBehaviour
{
    [SerializeField] private MapPath cameraPath;
    [SerializeField] private MapPath playerPath;
    [SerializeField] private MapPathFollower cameraFollower;
    [SerializeField] private MapPathFollower playerFollower;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button startButton;
    [SerializeField] private Text checkpointLabel;
    [SerializeField] private Image panelBackground;
    [SerializeField] private int currentCheckpointIndex;
    [Header("Timing")]
    [Tooltip("Central duration used when the camera and player move between map checkpoints.")]
    [SerializeField, Min(0.01f)] private float transitionDuration = 1.5f;
    [SerializeField] private bool snapFollowersToCurrentCheckpointOnStart = true;
    [SerializeField] private bool enforceCameraFollowerAsMainCamera = true;

    private bool waitingForMove;
    private bool mapStarted;
    private bool introMoveInProgress;

    private void Awake()
    {
        if (previousButton != null)
            previousButton.onClick.AddListener(MovePrevious);
        if (nextButton != null)
            nextButton.onClick.AddListener(MoveNext);
        if (startButton != null)
            startButton.onClick.AddListener(StartFirstLevel);
    }

    private void Start()
    {
        if (enforceCameraFollowerAsMainCamera)
            EnforceCameraFollowerAsMainCamera();

        if (snapFollowersToCurrentCheckpointOnStart)
        {
            cameraFollower?.JumpToCheckpoint(currentCheckpointIndex);
            playerFollower?.JumpToCheckpoint(currentCheckpointIndex);
        }

        Refresh();
    }

    private void Update()
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

    public void Configure(
        MapPath newCameraPath,
        MapPath newPlayerPath,
        MapPathFollower newCameraFollower,
        MapPathFollower newPlayerFollower,
        Button newPreviousButton,
        Button newNextButton,
        Button newStartButton,
        Text newCheckpointLabel,
        Image newPanelBackground)
    {
        cameraPath = newCameraPath;
        playerPath = newPlayerPath;
        cameraFollower = newCameraFollower;
        playerFollower = newPlayerFollower;
        previousButton = newPreviousButton;
        nextButton = newNextButton;
        startButton = newStartButton;
        checkpointLabel = newCheckpointLabel;
        panelBackground = newPanelBackground;
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
