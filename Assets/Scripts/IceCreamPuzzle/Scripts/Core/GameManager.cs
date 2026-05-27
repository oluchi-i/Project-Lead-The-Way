using UnityEngine;
using UnityEngine.Events;

public enum GameState { Initializing, WaitingInput, Stepping, Won, Lost }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Scene References")]
    public CharacterMover       character;
    public IceCreamLevelBuilder levelBuilder;
    public GameUI               ui;

    [Header("Events")]
    public UnityEvent      onLevelWon;
    public UnityEvent      onLevelLost;
    public UnityEvent<int> onMoveCountChanged;

    public GameState CurrentState { get; private set; }
    public int       MoveCount    { get; private set; }
    public int       StepCount    { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        CurrentState = GameState.Initializing;
    }

    void Start()
    {
        if (levelBuilder == null)
            levelBuilder = FindObjectOfType<IceCreamLevelBuilder>();

        levelBuilder.BuildLevel();
        SetState(GameState.WaitingInput);
        Debug.Log("[GameManager] Ready. Arrange obstacles, then press SPACE.");
    }

    public void OnObstacleSlid()
    {
        MoveCount++;
        if (onMoveCountChanged != null) onMoveCountChanged.Invoke(MoveCount);
        if (ui != null) ui.UpdateMoveCount(MoveCount);
    }

    public void AdvanceCharacter()
    {
        if (CurrentState != GameState.WaitingInput) return;

        SetState(GameState.Stepping);
        StepCount++;
        if (ui != null) ui.UpdateStepCount(StepCount);

        StepResult result = character.TakeStep();

        if (result == StepResult.ReachedExit)
        {
            SetState(GameState.Won);
            Debug.Log("Win! Steps:" + StepCount + " Moves:" + MoveCount);
            if (ui != null) ui.ShowWin(StepCount, MoveCount);
            if (onLevelWon != null) onLevelWon.Invoke();
        }
        else if (result == StepResult.Stuck)
        {
            SetState(GameState.Lost);
            Debug.Log("Stuck!");
            if (ui != null) ui.ShowLost();
            if (onLevelLost != null) onLevelLost.Invoke();
        }
        else
        {
            Invoke("FinishStep", character.StepDuration + 0.05f);
        }
    }

    public void RestartLevel()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    void FinishStep()
    {
        if (CurrentState == GameState.Stepping)
            SetState(GameState.WaitingInput);
    }

    void SetState(GameState next)
    {
        CurrentState = next;
        if (ui != null) ui.UpdateState(next);
    }
}
