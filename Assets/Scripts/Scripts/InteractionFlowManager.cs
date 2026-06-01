using System;
using UnityEngine;
using UnityEngine.Events;

// ─────────────────────────────────────────────────────────────────────────────
//  InteractionFlowManager.cs
//  Tracks how many obstacle moves (interactions) the player has used this level.
//  Consumed by:
//   • SelectionPanelsUI  — checks CanAcceptAction before allowing moves
//   • InteractionCounterUI — subscribes to InteractionCountChanged for display
//   • LevelResultFlashUI — registered via ConfigureResultFlash
//
//  Place on any persistent GameObject.
// ─────────────────────────────────────────────────────────────────────────────

public class InteractionFlowManager : MonoBehaviour
{
    [Header("Limits")]
    [SerializeField] private int maxInteractionCount = 10;
    [SerializeField] private bool unlimitedInteractions = false;

    [Header("Events")]
    public UnityEvent onLimitReached;
    public UnityEvent onInteractionRegistered;

    // ── Runtime refs (wired by LeadTheWayObjectSetupTools or at runtime) ──

    private BoardManager        _boardManager;
    private BoardPlayerMover    _playerMover;
    private BoardObject         _playerObject;
    private BoardObject         _targetDoor;
    private LevelResultFlashUI  _resultFlash;

    // ── Public state ──────────────────────────────────────────────────────

    /// <summary>Raised whenever the count changes. Passes the new count.</summary>
    public event Action<int> InteractionCountChanged;

    public int  InteractionCount    { get; private set; }
    public int  MaxInteractionCount { get { return maxInteractionCount; } }

    /// <summary>False when the player has used all their moves.</summary>
    public bool CanAcceptAction
    {
        get { return unlimitedInteractions || InteractionCount < maxInteractionCount; }
    }

    /// <summary>True during the frame an interaction was successfully registered.</summary>
    public bool HasRegisteredInteractionThisFrame { get; private set; }

    /// <summary>True during the frame any action was handled (even if rejected).</summary>
    public bool HasHandledActionThisFrame { get; private set; }

    // ── Unity ─────────────────────────────────────────────────────────────

    private void LateUpdate()
    {
        // Reset per-frame flags after all scripts have had a chance to read them
        HasRegisteredInteractionThisFrame = false;
        HasHandledActionThisFrame = false;
    }

    // ── Editor / runtime wiring ───────────────────────────────────────────

    /// <summary>
    /// Called by LeadTheWayObjectSetupTools to wire scene references at edit-time,
    /// and may also be called at runtime by level builders.
    /// </summary>
    public void Configure(BoardManager boardManager, BoardPlayerMover playerMover,
                          BoardObject playerObject, BoardObject targetDoor)
    {
        _boardManager = boardManager;
        _playerMover  = playerMover;
        _playerObject = playerObject;
        _targetDoor   = targetDoor;
    }

    /// <summary>
    /// Called by LeadTheWayObjectSetupTools to register the flash overlay
    /// so it can be triggered on win/loss.
    /// </summary>
    public void ConfigureResultFlash(LevelResultFlashUI flashUI)
    {
        _resultFlash = flashUI;
    }

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>
    /// Attempt to consume one interaction slot.
    /// Returns true if accepted; fires InteractionCountChanged.
    /// </summary>
    public bool RegisterInteraction(SelectableControlObject obj, ControlAction action)
    {
        HasHandledActionThisFrame = true;

        if (!CanAcceptAction)
            return false;

        InteractionCount++;
        HasRegisteredInteractionThisFrame = true;

        if (InteractionCountChanged != null)
            InteractionCountChanged.Invoke(InteractionCount);

        if (onInteractionRegistered != null)
            onInteractionRegistered.Invoke();

        if (!CanAcceptAction && onLimitReached != null)
            onLimitReached.Invoke();

        return true;
    }

    /// <summary>Flash the result overlay for a win or loss.</summary>
    public void FlashResult(bool success)
    {
        if (_resultFlash != null)
            _resultFlash.Flash(success);
    }

    public void ResetCount()
    {
        InteractionCount = 0;
        HasRegisteredInteractionThisFrame = false;
        HasHandledActionThisFrame = false;

        if (InteractionCountChanged != null)
            InteractionCountChanged.Invoke(InteractionCount);
    }

    public void SetMaxInteractionCount(int max)
    {
        maxInteractionCount = Mathf.Max(1, max);

        if (InteractionCountChanged != null)
            InteractionCountChanged.Invoke(InteractionCount);
    }
}
