using System.Collections;
using UnityEngine;

public enum StepResult { Moved, ReachedExit, Stuck, Moving }

public class CharacterMover : MonoBehaviour
{
    [Header("Speed")]
    public float moveSpeed = 5f;

    [Header("Visual bobbing")]
    public float bobHeight    = 0.08f;
    public float bobFrequency = 8f;

    public Direction  Facing  { get; private set; }
    public Vector2Int GridPos { get; private set; }

    public float StepDuration { get { return 1f / Mathf.Max(moveSpeed, 0.1f); } }

    private bool _isMoving;

    public void Initialize(Vector2Int startPos, Direction startFacing)
    {
        GridPos = startPos;
        Facing  = startFacing;
        transform.position = GridManager.Instance.GridToWorld(startPos);
        ApplyFacingRotation();
    }

    public StepResult TakeStep()
    {
        if (_isMoving) return StepResult.Moving;

        for (int attempt = 0; attempt < 4; attempt++)
        {
            Vector2Int next = GridPos + GridManager.DirToVec(Facing);

            if (GridManager.Instance.IsExit(next))
            {
                StartCoroutine(SmoothMove(next));
                GridPos = next;
                return StepResult.ReachedExit;
            }

            if (GridManager.Instance.IsWalkable(next))
            {
                StartCoroutine(SmoothMove(next));
                GridPos = next;
                return StepResult.Moved;
            }

            Facing = GridManager.RotateCW(Facing);
            ApplyFacingRotation();
        }

        return StepResult.Stuck;
    }

    IEnumerator SmoothMove(Vector2Int target)
    {
        _isMoving = true;
        Vector3 from     = transform.position;
        Vector3 to       = GridManager.Instance.GridToWorld(target);
        float   duration = StepDuration;
        float   elapsed  = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            Vector3 pos = Vector3.Lerp(from, to, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * bobHeight;
            transform.position = pos;
            yield return null;
        }

        transform.position = to;
        _isMoving = false;
    }

    void ApplyFacingRotation()
    {
        float yaw = 0f;
        if      (Facing == Direction.North) yaw =   0f;
        else if (Facing == Direction.East)  yaw = -90f;
        else if (Facing == Direction.South) yaw = 180f;
        else if (Facing == Direction.West)  yaw =  90f;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    void Update()
    {
        if (_isMoving) return;
        Vector3 pos = transform.position;
        pos.y = Mathf.Sin(Time.time * bobFrequency) * (bobHeight * 0.3f);
        transform.position = pos;
    }
}
