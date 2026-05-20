using UnityEngine;
using UnityEngine.InputSystem;

public class GridTileMover : MonoBehaviour
{
    [SerializeField] private bool keyboardInputEnabled = true;
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private float moveDuration = 0.18f;

    private bool isMoving;
    private float moveElapsed;
    private Vector3 moveStart;
    private Vector3 moveTarget;

    private void Update()
    {
        if (isMoving)
        {
            UpdateMove();
            return;
        }

        if (!keyboardInputEnabled || Keyboard.current == null)
            return;

        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
            MovePositiveX();
        else if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
            MoveNegativeX();
        else if (Keyboard.current.upArrowKey.wasPressedThisFrame)
            MoveNegativeZ();
        else if (Keyboard.current.downArrowKey.wasPressedThisFrame)
            MovePositiveZ();
    }

    public void MovePositiveX()
    {
        TryMove(Vector3.right);
    }

    public void MoveNegativeX()
    {
        TryMove(Vector3.left);
    }

    public void MovePositiveZ()
    {
        TryMove(Vector3.forward);
    }

    public void MoveNegativeZ()
    {
        TryMove(Vector3.back);
    }

    private void TryMove(Vector3 direction)
    {
        if (isMoving)
            return;

        moveStart = transform.position;
        moveTarget = moveStart + direction * tileSize;
        moveElapsed = 0f;
        isMoving = true;
    }

    private void UpdateMove()
    {
        moveElapsed += Time.deltaTime;
        var duration = Mathf.Max(0.01f, moveDuration);
        var t = Mathf.Clamp01(moveElapsed / duration);
        t = Mathf.SmoothStep(0f, 1f, t);

        transform.position = Vector3.Lerp(moveStart, moveTarget, t);

        if (moveElapsed >= duration)
        {
            transform.position = moveTarget;
            isMoving = false;
        }
    }
}
