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
    [SerializeField] private Text checkpointLabel;
    [SerializeField] private int currentCheckpointIndex;
    [SerializeField] private bool snapFollowersToCurrentCheckpointOnStart = true;
    [SerializeField] private bool enforceCameraFollowerAsMainCamera = true;

    private bool waitingForMove;

    private void Awake()
    {
        if (previousButton != null)
            previousButton.onClick.AddListener(MovePrevious);
        if (nextButton != null)
            nextButton.onClick.AddListener(MoveNext);
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
        Refresh();
    }

    public void Configure(
        MapPath newCameraPath,
        MapPath newPlayerPath,
        MapPathFollower newCameraFollower,
        MapPathFollower newPlayerFollower,
        Button newPreviousButton,
        Button newNextButton,
        Text newCheckpointLabel)
    {
        cameraPath = newCameraPath;
        playerPath = newPlayerPath;
        cameraFollower = newCameraFollower;
        playerFollower = newPlayerFollower;
        previousButton = newPreviousButton;
        nextButton = newNextButton;
        checkpointLabel = newCheckpointLabel;
        Refresh();
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
        cameraFollower?.MoveToCheckpoint(currentCheckpointIndex);
        playerFollower?.MoveToCheckpoint(currentCheckpointIndex);
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
        if (checkpointLabel != null)
            checkpointLabel.text = GetCheckpointText();

        if (previousButton != null)
            previousButton.interactable = CanMoveTo(currentCheckpointIndex - 1);

        if (nextButton != null)
            nextButton.interactable = CanMoveTo(currentCheckpointIndex + 1);
    }

    private string GetCheckpointText()
    {
        var label = string.Empty;
        if (playerPath != null)
            playerPath.TryGetCheckpointLabel(currentCheckpointIndex, out label);

        if (string.IsNullOrWhiteSpace(label))
            label = currentCheckpointIndex == 0 ? "Start" : "Level " + currentCheckpointIndex;

        return $"{label} (Checkpoint {currentCheckpointIndex})";
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
