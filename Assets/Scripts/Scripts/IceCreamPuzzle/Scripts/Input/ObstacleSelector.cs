using UnityEngine;
using UnityEngine.InputSystem;

public class ObstacleSelector : MonoBehaviour
{
    public static ObstacleSelector Instance { get; private set; }

    [Header("Raycast")]
    public LayerMask obstacleLayer = ~0;
    public float     rayDistance   = 100f;

    public ObstacleBlock SelectedObstacle { get; private set; }

    Camera _cam;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _cam = Camera.main;
    }

    void Update()
    {
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            if (GameManager.Instance != null) GameManager.Instance.RestartLevel();
            return;
        }

        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.WaitingInput) return;

        HandleClick();
        HandleSlide();
        HandleStep();
    }

    void HandleClick()
    {
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        Ray ray = _cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, rayDistance, obstacleLayer))
        {
            ObstacleBlock obs = hit.collider.GetComponentInParent<ObstacleBlock>();
            Select(obs != null && obs.isMoveable ? obs : null);
        }
        else
        {
            Select(null);
        }
    }

    void HandleSlide()
    {
        if (SelectedObstacle == null) return;

        var kb = Keyboard.current;

        bool up    = kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame;
        bool right = kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame;
        bool down  = kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame;
        bool left  = kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame;

        Direction? dir = null;
        if (up)    dir = Direction.North;
        if (right) dir = Direction.East;
        if (down)  dir = Direction.South;
        if (left)  dir = Direction.West;

        if (dir == null) return;

        bool moved = GridManager.Instance.SlideObstacle(SelectedObstacle, dir.Value);
        if (moved)
        {
            AudioManager.Instance?.PlaySFX_Slide();
            if (GameManager.Instance != null) GameManager.Instance.OnObstacleSlid();
        }
    }

    void HandleStep()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame && GameManager.Instance != null)
            GameManager.Instance.AdvanceCharacter();
    }

    void Select(ObstacleBlock obs)
    {
        if (SelectedObstacle != null) SelectedObstacle.SetSelected(false);
        SelectedObstacle = obs;
        if (SelectedObstacle != null) SelectedObstacle.SetSelected(true);
    }
}
