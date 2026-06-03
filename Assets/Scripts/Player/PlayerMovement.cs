using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private float explosionScatter = 0.85f;
    [SerializeField] private float explosionCenterHeight = 0.35f;
    [SerializeField] private float separationOffset = 0.06f;
    [SerializeField] private float torqueForce = 32f;
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

        var explosionCenter = CalculateDeathExplosionCenter();
        foreach (var part in CollectDeathParts())
            ReleaseBodyPart(part, explosionCenter);
    }

    private List<Transform> CollectDeathParts()
    {
        var parts = new List<Transform>();
        var seen = new HashSet<Transform>();

        foreach (var body in GetComponentsInChildren<Rigidbody>(true))
        {
            if (body == null || body.transform == transform)
                continue;

            AddDeathPart(parts, seen, body.transform);
        }

        if (bodyParts != null)
        {
            foreach (var part in bodyParts)
                AddDeathPart(parts, seen, part);
        }

        if (parts.Count == 0 && transform.childCount > 0)
            AddDeathPart(parts, seen, transform.GetChild(0));

        return parts;
    }

    private static void AddDeathPart(List<Transform> parts, HashSet<Transform> seen, Transform part)
    {
        if (part == null || seen.Contains(part))
            return;

        seen.Add(part);
        parts.Add(part);
    }

    private Vector3 CalculateDeathExplosionCenter()
    {
        var renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return transform.position + Vector3.up * explosionCenterHeight;

        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        var center = bounds.center;
        center.y = bounds.min.y + Mathf.Max(0f, explosionCenterHeight);
        return center;
    }

    private void ReleaseBodyPart(Transform part, Vector3 explosionCenter)
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
        partRigidbody.linearVelocity = Vector3.zero;
        partRigidbody.angularVelocity = Vector3.zero;

        var force = Mathf.Max(0.05f, explosionForce > 0f ? explosionForce : 1.15f);
        var lift = Mathf.Max(0f, upwardForce);
        var radialDirection = part.position - explosionCenter;
        radialDirection.y = 0f;
        if (radialDirection.sqrMagnitude < 0.001f)
            radialDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));

        partRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        partRigidbody.interpolation = RigidbodyInterpolation.Interpolate;

        var randomDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
        var scatter = Mathf.Max(0f, explosionScatter);
        var blastDirection = (radialDirection.normalized + randomDirection.normalized * scatter).normalized;
        if (blastDirection.sqrMagnitude < 0.001f)
            blastDirection = Vector3.right;

        part.position += blastDirection * separationOffset * Random.Range(0.65f, 1.25f);

        var sidewaysImpulse = blastDirection * force * Random.Range(0.65f, 1.35f);
        var upwardImpulse = Vector3.up * lift * Random.Range(0.55f, 1.35f);
        partRigidbody.AddForce(sidewaysImpulse + upwardImpulse, ForceMode.Impulse);

        var spinScale = Mathf.Max(0f, torqueForce) * Random.Range(0.7f, 1.45f);
        var randomSpin = new Vector3(Random.Range(-spinScale, spinScale), Random.Range(-spinScale, spinScale), Random.Range(-spinScale, spinScale));
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
