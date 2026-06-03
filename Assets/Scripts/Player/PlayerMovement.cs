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
    [SerializeField] private bool usePhysicsDeath = true;
    [SerializeField] private float deathVisualDuration = 0.45f;
    [SerializeField] private float deathTiltAngle = 72f;
    [SerializeField] private float deathSquash = 0.18f;

    private bool isDead;
    private Vector3 originalScale;
    private Quaternion originalRotation;
    private bool cachedOriginalPose;

    private void Awake()
    {
        CacheLimbReferences();
        CacheOriginalPose();
        ConfigureForBoardMovement();
        state = State.Idle;
    }

    public void ConfigureForBoardMovement()
    {
        DisableVisualPhysics();
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

        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }

        if (!usePhysicsDeath)
        {
            animationRoutine = StartCoroutine(BoardDeathAnimation());
            return;
        }

        if (transform.childCount > 0)
            ReleaseBodyPart(transform.GetChild(0));

        if (bodyParts != null)
        {
            foreach (var part in bodyParts)
                ReleaseBodyPart(part);
        }
    }

    private void ReleaseBodyPart(Transform part)
    {
        if (part == null || part.parent == null)
            return;

        part.SetParent(null);

        foreach (var collider in part.GetComponentsInChildren<Collider>())
        {
            if (collider is MeshCollider meshCollider)
                meshCollider.convex = true;

            collider.enabled = true;
        }

        var partRigidbody = part.GetComponent<Rigidbody>();
        if (partRigidbody == null)
            return;

        partRigidbody.isKinematic = false;
        partRigidbody.useGravity = true;

        var force = Mathf.Clamp(explosionForce > 0f ? explosionForce : 1.15f, 0.05f, 2.2f);
        var lift = Mathf.Clamp(upwardForce > 0f ? upwardForce : 0.08f, 0f, 0.35f);
        var horizontalDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
        if (horizontalDirection.sqrMagnitude < 0.001f)
            horizontalDirection = Vector3.right;

        var blastDirection = (horizontalDirection.normalized + Vector3.up * lift).normalized;
        partRigidbody.AddForce(blastDirection * force * Random.Range(0.55f, 0.85f), ForceMode.Impulse);

        var randomSpin = new Vector3(Random.Range(-14f, 14f), Random.Range(-18f, 18f), Random.Range(-14f, 14f));
        partRigidbody.AddTorque(randomSpin, ForceMode.Impulse);
    }

    private IEnumerator BoardDeathAnimation()
    {
        CacheOriginalPose();
        DisableVisualPhysics();

        var startScale = transform.localScale;
        var startRotation = transform.localRotation;
        var targetScale = new Vector3(
            originalScale.x,
            Mathf.Max(0.01f, originalScale.y * Mathf.Clamp01(deathSquash)),
            originalScale.z);
        var targetRotation = startRotation * Quaternion.Euler(0f, 0f, -deathTiltAngle);
        var elapsed = 0f;
        var duration = Mathf.Max(0.01f, deathVisualDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            transform.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        transform.localScale = targetScale;
        transform.localRotation = targetRotation;
        animationRoutine = null;
    }

    private void DisableVisualPhysics()
    {
        foreach (var body in GetComponentsInChildren<Rigidbody>(true))
        {
            body.isKinematic = true;
            body.useGravity = false;
        }

        foreach (var collider in GetComponentsInChildren<Collider>(true))
            collider.enabled = false;
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

    private void CacheOriginalPose()
    {
        if (cachedOriginalPose)
            return;

        originalScale = transform.localScale;
        originalRotation = transform.localRotation;
        cachedOriginalPose = true;
    }

    private bool HasLimbReferences()
    {
        return leftArmTransform != null
            && rightArmTransform != null
            && leftLegTransform != null
            && rightLegTransform != null;
    }
}
