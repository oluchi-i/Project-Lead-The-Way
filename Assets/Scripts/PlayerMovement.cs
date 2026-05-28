using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Walk Animation")]
    public GameObject leftArm;
    public GameObject rightArm;
    public GameObject leftLeg;
    public GameObject rightLeg;
    public float armSwingAngleLim = 30f;
    public float legSwingAngleLim = 30f;
    public float speed = 1f;

    [SerializeField] private float blendSpeed = 5f;

    private enum State
    {
        Idle,
        Walk
    }

    private State state;
    private Coroutine animationRoutine;
    private float walkWeight;
    private float animationTimer;

    private void Awake()
    {
        state = State.Idle;
    }

    public void ConfigureForBoardMovement()
    {
        SetWalking(false);
    }

    public void SetWalking(bool isWalking)
    {
        ChangeState(isWalking ? State.Walk : State.Idle);
    }

    private void ChangeState(State newState)
    {
        state = newState;

        if (animationRoutine == null)
            animationRoutine = StartCoroutine(WalkAnimation());
    }

    private IEnumerator WalkAnimation()
    {
        if (leftArm == null || rightArm == null || leftLeg == null || rightLeg == null)
        {
            animationRoutine = null;
            yield break;
        }

        while (state == State.Walk || walkWeight > 0f)
        {
            var targetWeight = state == State.Walk ? 1f : 0f;
            walkWeight = Mathf.MoveTowards(walkWeight, targetWeight, blendSpeed * Time.deltaTime);

            var swingSpeed = Mathf.Max(0.01f, speed) * 10f;
            var armSwingAngle = Mathf.Sin(animationTimer * swingSpeed) * armSwingAngleLim;
            var legSwingAngle = Mathf.Sin(animationTimer * swingSpeed) * legSwingAngleLim;

            leftArm.transform.localRotation = Quaternion.Lerp(Quaternion.identity, Quaternion.Euler(armSwingAngle, 0f, 0f), walkWeight);
            rightArm.transform.localRotation = Quaternion.Lerp(Quaternion.identity, Quaternion.Euler(-armSwingAngle, 0f, 0f), walkWeight);
            leftLeg.transform.localRotation = Quaternion.Lerp(Quaternion.identity, Quaternion.Euler(-legSwingAngle, 0f, 0f), walkWeight);
            rightLeg.transform.localRotation = Quaternion.Lerp(Quaternion.identity, Quaternion.Euler(legSwingAngle, 0f, 0f), walkWeight);

            animationTimer += Time.deltaTime;
            yield return null;
        }

        ResetLimbRotations();
        animationTimer = 0f;
        animationRoutine = null;
    }

    private void ResetLimbRotations()
    {
        if (leftArm != null)
            leftArm.transform.localRotation = Quaternion.identity;

        if (rightArm != null)
            rightArm.transform.localRotation = Quaternion.identity;

        if (leftLeg != null)
            leftLeg.transform.localRotation = Quaternion.identity;

        if (rightLeg != null)
            rightLeg.transform.localRotation = Quaternion.identity;
    }
}
