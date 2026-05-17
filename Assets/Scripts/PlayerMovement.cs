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
    public GameObject tile;
    public float speed = 1f;
    private enum State{Idle, Walk, WalkToIdle, Turn};
    State state;
    // north +z, south -z, West -x, East +x
    private enum Direction{North, South, West, East};
    Direction direction;
    private Rigidbody rb;
    private float tileSize;
    private Coroutine movementRoutine;
    // Vector3.forward, Vector3.right, Vector3.back, Vector3.left
    private Queue<Vector3> movementQueue = new Queue<Vector3>();
    private float walkWeight = 0f;
    private float blendSpeed = 5f;
    private Coroutine managerRoutine;
    private float animatorTimer = 0;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        MeshRenderer meshRenderer = tile.GetComponent<MeshRenderer>();
        tileSize = meshRenderer.bounds.size.x;

        state = State.Idle;
        direction = Direction.North;
    }

    void Start()
    {
        QueueMove(Vector3.forward);
        QueueMove(Vector3.forward);
        QueueMove(Vector3.right);
        QueueMove(Vector3.right);
    }

    void Update()
    {
        
    }
    public void QueueMove(Vector3 direction)
    {
        movementQueue.Enqueue(direction);
        if (movementRoutine == null)
            movementRoutine = StartCoroutine(ProcessMovement());
    }

    private IEnumerator ProcessMovement()
    {
        while (movementQueue.Count > 0)
        {
            ChangeState(State.Walk);
            Vector3 currentDirection = movementQueue.Dequeue();

            Vector3 startPosition = transform.position;
            Vector3 targetPosition = transform.position + (currentDirection * tileSize);
            transform.rotation = Quaternion.LookRotation(currentDirection);

            float distanceTravel = 0f;
            
            while (distanceTravel < tileSize)
            {
                transform.position += transform.forward * speed * Time.deltaTime;
                distanceTravel += speed * Time.deltaTime;
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
}
