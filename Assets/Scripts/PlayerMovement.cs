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
        Walk,
        Death
    }

    private State state;
    private Coroutine animationRoutine;
    private float walkWeight;
    private float animationTimer;
    private Transform leftArmTransform;
    private Transform rightArmTransform;
    private Transform leftLegTransform;
    private Transform rightLegTransform;
    private Quaternion leftArmRestRotation;
    private Quaternion rightArmRestRotation;
    private Quaternion leftLegRestRotation;
    private Quaternion rightLegRestRotation;


    [Header ("Death Animation")]
    public float explosionForce;
    public float upwardForce;
    public Transform[] bodyParts;
    private bool isDead;

    private void Awake()
    {
        CacheLimbReferences();
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
        if (isDead && newState != State.Death)
            return;

        state = newState;
        if (!isActiveAndEnabled)
            return;

        if (state == State.Death)
        {
            DeathAnimation();
            return;
        }

        if (animationRoutine == null)
            animationRoutine = StartCoroutine(WalkAnimation());
    }

    public void Kill()
    {
        if (isDead)
            return;

        ChangeState(State.Death);
    }
    
    private void DeathAnimation()
    {
        isDead = true;
        SetWalking(false);

        if (transform.childCount > 0)
            ReleaseBodyPart(transform.GetChild(0));

        if (bodyParts == null)
            return;

        foreach (var part in bodyParts)
            ReleaseBodyPart(part);
    }

    private void ReleaseBodyPart(Transform part)
    {
        if (part == null || part.parent == null)
            return;

        part.SetParent(null);

        var collider = part.GetComponentInChildren<Collider>();
        if (collider != null)
            collider.enabled = true;

        var partRigidbody = part.GetComponent<Rigidbody>();
        if (partRigidbody == null)
            return;

        partRigidbody.isKinematic = false;
        partRigidbody.useGravity = true;

        var force = explosionForce > 0f ? explosionForce : 3.5f;
        var lift = upwardForce > 0f ? upwardForce : 1.2f;
        var blastDirection = new Vector3(Random.Range(-1f, 1f), Random.Range(0.2f, 1f) * lift, Random.Range(-1f, 1f)).normalized;
        partRigidbody.AddForce(blastDirection * force * Random.Range(0.5f, 1.5f), ForceMode.Impulse);

        var randomSpin = new Vector3(Random.Range(-100, 100), Random.Range(-100, 100), Random.Range(-100, 100));
        partRigidbody.AddTorque(randomSpin, ForceMode.Impulse);
    }

    private IEnumerator WalkAnimation()
    {
        if (!HasLimbReferences())
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

            leftArmTransform.localRotation = leftArmRestRotation * Quaternion.Lerp(Quaternion.identity, Quaternion.Euler(armSwingAngle, 0f, 0f), walkWeight);
            rightArmTransform.localRotation = rightArmRestRotation * Quaternion.Lerp(Quaternion.identity, Quaternion.Euler(-armSwingAngle, 0f, 0f), walkWeight);
            leftLegTransform.localRotation = leftLegRestRotation * Quaternion.Lerp(Quaternion.identity, Quaternion.Euler(-legSwingAngle, 0f, 0f), walkWeight);
            rightLegTransform.localRotation = rightLegRestRotation * Quaternion.Lerp(Quaternion.identity, Quaternion.Euler(legSwingAngle, 0f, 0f), walkWeight);

            animationTimer += Time.deltaTime;
            yield return null;
        }

        ResetLimbRotations();
        animationTimer = 0f;
        animationRoutine = null;
    }

    private void OnDisable()
    {
        if (isDead)
            return;

        state = State.Idle;
        animationRoutine = null;
        walkWeight = 0f;
        ResetLimbRotations();
    }

    private void ResetLimbRotations()
    {
        if (leftArmTransform != null)
            leftArmTransform.localRotation = leftArmRestRotation;

        if (rightArmTransform != null)
            rightArmTransform.localRotation = rightArmRestRotation;

        if (leftLegTransform != null)
            leftLegTransform.localRotation = leftLegRestRotation;

        if (rightLegTransform != null)
            rightLegTransform.localRotation = rightLegRestRotation;
    }

    private void CacheLimbReferences()
    {
        leftArmTransform = leftArm != null ? leftArm.transform : null;
        rightArmTransform = rightArm != null ? rightArm.transform : null;
        leftLegTransform = leftLeg != null ? leftLeg.transform : null;
        rightLegTransform = rightLeg != null ? rightLeg.transform : null;

        if (leftArmTransform != null)
            leftArmRestRotation = leftArmTransform.localRotation;

        if (rightArmTransform != null)
            rightArmRestRotation = rightArmTransform.localRotation;

        if (leftLegTransform != null)
            leftLegRestRotation = leftLegTransform.localRotation;

        if (rightLegTransform != null)
            rightLegRestRotation = rightLegTransform.localRotation;
    }

    private bool HasLimbReferences()
    {
        return leftArmTransform != null
            && rightArmTransform != null
            && leftLegTransform != null
            && rightLegTransform != null;
    }
}
