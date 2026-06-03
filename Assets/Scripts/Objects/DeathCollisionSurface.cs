using UnityEngine;

[DisallowMultipleComponent]
public class DeathCollisionSurface : MonoBehaviour
{
    [SerializeField] private Collider[] colliders;

    private void Awake()
    {
        ConfigureStaticCollision();
    }

    private void OnValidate()
    {
        if (colliders == null || colliders.Length == 0)
            colliders = GetComponentsInChildren<Collider>(true);
    }

    public void Configure(Collider[] newColliders)
    {
        colliders = newColliders;
        ConfigureStaticCollision();
    }

    private void ConfigureStaticCollision()
    {
        if (colliders == null || colliders.Length == 0)
            colliders = GetComponentsInChildren<Collider>(true);

        foreach (var body in GetComponentsInChildren<Rigidbody>(true))
        {
            body.isKinematic = true;
            body.useGravity = false;
        }

        foreach (var collider in colliders)
        {
            if (collider == null)
                continue;

            collider.enabled = true;
            collider.isTrigger = false;
        }
    }
}
