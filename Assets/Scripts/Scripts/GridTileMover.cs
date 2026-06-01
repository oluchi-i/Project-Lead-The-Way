using System.Collections;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
//  GridTileMover.cs
//  Smooth ice-slide movement for obstacle GameObjects.
//  Mirrors BoardPlayerMover's animation pattern: rotate then glide, with a
//  small arrival bounce so the block feels satisfyingly physical.
//
//  Exposes the four directional methods SelectionPanelsUI expects:
//    TryMovePositiveX  ←  left  arrow key  (isometric: NW)
//    TryMoveNegativeX  ←  right arrow key  (isometric: SE)
//    TryMoveNegativeZ  ←  up    arrow key  (isometric: NE)
//    TryMovePositiveZ  ←  down  arrow key  (isometric: SW)
//
//  Attach to obstacle GameObjects alongside:
//    ObstacleBlock, BoardObject, SelectableControlObject
// ─────────────────────────────────────────────────────────────────────────────

[RequireComponent(typeof(BoardObject))]
public class GridTileMover : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────

    [Header("Slide animation")]
    [SerializeField] private float slideSpeedPerTile = 0.18f;   // seconds per tile
    [SerializeField] private float minSlideDuration  = 0.12f;
    [SerializeField] private float maxSlideDuration  = 0.55f;

    [Header("Rotation (brief tilt toward slide direction)")]
    [SerializeField] private float rotateDuration = 0.09f;

    [Header("Arrival bounce")]
    [SerializeField] private float bounceHeight   = 0.10f;
    [SerializeField] private float bounceDuration = 0.14f;

    // ── Runtime ───────────────────────────────────────────────────────────

    public bool IsMoving { get; private set; }

    private BoardObject   _boardObject;
    private BoardManager  _boardManager;
    private Coroutine     _routine;

    // ── Unity ─────────────────────────────────────────────────────────────

    private void Awake() { EnsureRefs(); }

    // ── Directional API ───────────────────────────────────────────────────
    // Method names must match what SelectionPanelsUI.TryHandleSelectedMoverKeyboard calls.

    public bool TryMovePositiveX() { return TrySlide(new Vector2Int( 1,  0)); }
    public bool TryMoveNegativeX() { return TrySlide(new Vector2Int(-1,  0)); }
    public bool TryMovePositiveZ() { return TrySlide(new Vector2Int( 0,  1)); }
    public bool TryMoveNegativeZ() { return TrySlide(new Vector2Int( 0, -1)); }

    // ── Core ──────────────────────────────────────────────────────────────

    private bool TrySlide(Vector2Int direction)
    {
        if (IsMoving) return false;

        EnsureRefs();

        if (_boardObject == null || _boardManager == null) return false;

        Vector2Int fromTile, toTile;
        if (!_boardManager.TryMoveObject(_boardObject, direction, out fromTile, out toTile))
            return false;

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(SlideRoutine(fromTile, toTile));
        return true;
    }

    // ── Animation coroutines ──────────────────────────────────────────────

    private IEnumerator SlideRoutine(Vector2Int fromTile, Vector2Int toTile)
    {
        IsMoving = true;

        Vector3 startPos  = transform.position;
        Vector3 targetPos = _boardManager.TileToWorld(toTile, startPos.y);
        Vector3 delta     = targetPos - startPos;

        // ── 1. Snap-rotate toward slide direction ──────────────────────────
        if (delta.sqrMagnitude > 0.0001f)
            yield return RotateToward(delta.normalized);

        // ── 2. Glide ───────────────────────────────────────────────────────
        float tilesDist = Vector2Int.Distance(fromTile, toTile);
        float duration  = Mathf.Clamp(tilesDist * slideSpeedPerTile,
                                      minSlideDuration, maxSlideDuration);
        float elapsed   = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        transform.position = targetPos;

        // ── 3. Arrival bounce ──────────────────────────────────────────────
        if (bounceHeight > 0f && bounceDuration > 0f)
            yield return BounceRoutine(targetPos);

        IsMoving = false;
        _routine = null;
    }

    private IEnumerator RotateToward(Vector3 direction)
    {
        Quaternion targetRot = Quaternion.LookRotation(direction, Vector3.up);

        if (Quaternion.Angle(transform.rotation, targetRot) < 1f)
        {
            transform.rotation = targetRot;
            yield break;
        }

        Quaternion startRot = transform.rotation;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, rotateDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        transform.rotation = targetRot;
    }

    private IEnumerator BounceRoutine(Vector3 basePos)
    {
        float elapsed = 0f;

        while (elapsed < bounceDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / bounceDuration;
            // Half-sine arc: rises then falls back to base
            transform.position = basePos + Vector3.up * (Mathf.Sin(t * Mathf.PI) * bounceHeight);
            yield return null;
        }

        transform.position = basePos;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private void EnsureRefs()
    {
        if (_boardObject  == null) _boardObject  = GetComponent<BoardObject>();
        if (_boardManager == null) _boardManager = FindAnyObjectByType<BoardManager>();
    }
}
