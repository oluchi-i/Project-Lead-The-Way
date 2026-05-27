using UnityEngine;

public class LeverToggle : MonoBehaviour
{
    [SerializeField] private Transform arm;
    [SerializeField] private float toggledXRotation = 15f;
    [SerializeField] private float rotateDuration = 0.18f;
    [SerializeField] private bool startOn;

    private Quaternion offRotation;
    private Quaternion onRotation;
    private Quaternion rotateStart;
    private Quaternion rotateTarget;
    private float rotateElapsed;
    private bool isRotating;
    private bool isOn;

    private void Awake()
    {
        if (arm == null)
            arm = transform.Find("Arm") ?? transform.Find("arm");

        if (arm == null)
            return;

        offRotation = arm.localRotation;
        onRotation = offRotation * Quaternion.Euler(toggledXRotation, 0f, 0f);
        isOn = startOn;
        arm.localRotation = isOn ? onRotation : offRotation;
    }

    private void Update()
    {
        if (!isRotating || arm == null)
            return;

        rotateElapsed += Time.deltaTime;
        var duration = Mathf.Max(0.01f, rotateDuration);
        var t = Mathf.Clamp01(rotateElapsed / duration);
        t = Mathf.SmoothStep(0f, 1f, t);

        arm.localRotation = Quaternion.Slerp(rotateStart, rotateTarget, t);

        if (rotateElapsed >= duration)
        {
            arm.localRotation = rotateTarget;
            isRotating = false;
        }
    }

    public void ToggleLever()
    {
        if (arm == null)
            return;

        isOn = !isOn;
        rotateStart = arm.localRotation;
        rotateTarget = isOn ? onRotation : offRotation;
        rotateElapsed = 0f;
        isRotating = true;
    }

    public void Configure(Transform armTransform)
    {
        arm = armTransform;
    }
}
