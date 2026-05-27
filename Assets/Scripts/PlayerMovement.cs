using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerMovement : MonoBehaviour
{
    [Header("Walk Animation")]
    public GameObject leftArm;
    public GameObject rightArm;
    public GameObject leftLeg;
    public GameObject rightLeg;
    public float armSwingAngleLim = 30f;
    public float legSwingAngleLim = 30f;

    [Header("Other Stuff")]
    [SerializeField] private bool playDemoPathOnStart;
    public GameObject tile;
    public float speed = 1f;
    private enum State { Idle, Walk }
    private State state;
    // north +z, south -z, West -x, East +x
    private float tileSize;
    private Coroutine movementRoutine;
    // Vector3.forward, Vector3.right, Vector3.back, Vector3.left
    private readonly Queue<Vector3> movementQueue = new Queue<Vector3>();
    private float walkWeight = 0f;
    private float blendSpeed = 5f;
    private Coroutine managerRoutine;
    private float animatorTimer = 0;
    private Vector3 startPosition;
    private Vector3 targetPosition;

    void Awake()
    {
        tileSize = 1f;

        if (tile != null && tile.TryGetComponent<MeshRenderer>(out var meshRenderer))
            tileSize = meshRenderer.bounds.size.x;

        state = State.Idle;
    }

    void Start()
    {
        if (!playDemoPathOnStart)
            return;

        QueueMove(Vector3.forward * tileSize);
        QueueMove(Vector3.forward * tileSize);
        QueueMove(Vector3.right * tileSize);
        QueueMove(Vector3.right * tileSize);
    }

    public void QueueMove(Vector3 direction)
    {
        movementQueue.Enqueue(direction);
        if (movementRoutine == null)
            movementRoutine = StartCoroutine(ProcessMovement());
    }

    public void ConfigureForBoardMovement()
    {
        playDemoPathOnStart = false;
        tileSize = Mathf.Max(0.01f, tileSize);
    }

    public void SetWalking(bool isWalking)
    {
        ChangeState(isWalking ? State.Walk : State.Idle);
    }

    private IEnumerator ProcessMovement()
    {
        while (movementQueue.Count > 0)
        {
            ChangeState(State.Walk);
            Vector3 currentDirection = movementQueue.Dequeue();

            startPosition = transform.position;
            targetPosition = transform.position + currentDirection;
            transform.rotation = Quaternion.LookRotation(currentDirection);
            
            while ((transform.position - targetPosition).sqrMagnitude > 0.01f)
            {
                transform.position += transform.forward * speed * Time.deltaTime;
                yield return null;
            }

            transform.position = targetPosition;
        }
        ChangeState(State.Idle);
    }

    private void ChangeState(State newState)
    {
        state = newState;

        if (managerRoutine == null)
        {
            managerRoutine = StartCoroutine(WalkAnimation());
        }
    }

    private IEnumerator WalkAnimation()
    {
        if (leftArm == null || rightArm == null || leftLeg == null || rightLeg == null)
        {
            managerRoutine = null;
            yield break;
        }

        while (state == State.Walk || walkWeight > 0)
        {
            float targetWeight = (state == State.Walk) ? 1f : 0f;
            walkWeight = Mathf.MoveTowards(walkWeight, targetWeight, blendSpeed * Time.deltaTime);
            float armSwingAngle = Mathf.Sin(animatorTimer * speed * 10f) * armSwingAngleLim;
            float legSwingAngle = Mathf.Sin(animatorTimer * speed * 10f) * legSwingAngleLim;
            
            Quaternion walkLeftArm = Quaternion.Euler(armSwingAngle, 0, 0);
            Quaternion walkRightArm = Quaternion.Euler(-armSwingAngle, 0, 0);
            Quaternion walkLeftLeg = Quaternion.Euler(-legSwingAngle, 0, 0);
            Quaternion walkRightLeg = Quaternion.Euler(legSwingAngle, 0, 0);

            leftArm.transform.localRotation = Quaternion.Lerp(Quaternion.identity, walkLeftArm, walkWeight);
            rightArm.transform.localRotation = Quaternion.Lerp(Quaternion.identity, walkRightArm, walkWeight);
            leftLeg.transform.localRotation = Quaternion.Lerp(Quaternion.identity, walkLeftLeg, walkWeight);
            rightLeg.transform.localRotation = Quaternion.Lerp(Quaternion.identity, walkRightLeg, walkWeight);
        
            animatorTimer += Time.deltaTime;

            yield return null;
        }

        leftArm.transform.localRotation = Quaternion.identity;
        rightArm.transform.localRotation = Quaternion.identity;
        leftLeg.transform.localRotation = Quaternion.identity;
        rightLeg.transform.localRotation = Quaternion.identity;
        
        animatorTimer = 0;
        managerRoutine = null;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!playDemoPathOnStart || movementRoutine == null)
            return;

        if (collision.gameObject.CompareTag("Wall"))
        {
            targetPosition = startPosition;
            startPosition = transform.position;
            StopCoroutine(movementRoutine);
            state = State.Idle;
            QueueMoveFirst(targetPosition - startPosition);
            movementRoutine = StartCoroutine(ProcessMovement());
        }
    }

    private void QueueMoveFirst(Vector3 direction)
    {
        var pendingMoves = movementQueue.ToArray();
        movementQueue.Clear();
        movementQueue.Enqueue(direction);

        foreach (var pendingMove in pendingMoves)
            movementQueue.Enqueue(pendingMove);
    }
}
