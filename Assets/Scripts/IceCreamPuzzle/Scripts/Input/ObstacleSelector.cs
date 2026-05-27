using UnityEngine;

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
        if (Input.GetKeyDown(KeyCode.R))
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
        if (!Input.GetMouseButtonDown(0)) return;
        Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
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

        bool up    = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
        bool right = Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);
        bool down  = Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);
        bool left  = Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow);

        Direction? dir = null;
        if (up)    dir = Direction.North;
        if (right) dir = Direction.East;
        if (down)  dir = Direction.South;
        if (left)  dir = Direction.West;

        if (dir == null) return;

        bool moved = GridManager.Instance.SlideObstacle(SelectedObstacle, dir.Value);
        if (moved && GameManager.Instance != null) GameManager.Instance.OnObstacleSlid();
    }

    void HandleStep()
    {
        if (Input.GetKeyDown(KeyCode.Space) && GameManager.Instance != null)
            GameManager.Instance.AdvanceCharacter();
    }

    void Select(ObstacleBlock obs)
    {
        if (SelectedObstacle != null) SelectedObstacle.SetSelected(false);
        SelectedObstacle = obs;
        if (SelectedObstacle != null) SelectedObstacle.SetSelected(true);
    }
}
