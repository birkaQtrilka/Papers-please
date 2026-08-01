using UnityEngine;

public class Bomb : MonoBehaviour
{
    [Header("Explosion physics settings")]
    public float explosionForce = 500f;
    public float explosionRadius = 5f;
    public float upwardsModifier = 1f;
    [SerializeField] bool drawGizmos;

    [Header("Explosion effects settings")]
    [SerializeField] Rigidbody rb;
    [SerializeField] ParticleSystem explosionPs;
    [SerializeField] ParticleSystem fuseBurnPs;
    [SerializeField] AudioSource explosionAS;
    [SerializeField] CameraShake cameraShake;
    [SerializeField] float shakePower = 3f;
    [SerializeField] float shakeDuration = 1.5f;
    Vector3 origPos;

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        origPos = transform.position;
        rb.isKinematic = true;
    }

    public void Throw(Vector3 velocity)
    {
        transform.position = origPos;
        rb.isKinematic = false;
        rb.angularVelocity = Vector3.zero;
        rb.AddForce(velocity, ForceMode.Impulse);
        fuseBurnPs.Play();
    }
    [ContextMenu("Explode")]
    public void Explode() 
    {
        fuseBurnPs.Clear();
        fuseBurnPs.Stop();
        explosionAS.Play();
        explosionPs.Play();
        cameraShake.AddShake(shakePower, shakeDuration);
        ExplodeForce();
    }

    public void ExplodeForce()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (Collider nearbyObject in colliders)
        {
            if (!nearbyObject.TryGetComponent<Rigidbody>(out var rb) || rb.gameObject.name == "Bomb2") continue;
            rb.isKinematic = false;
            
            rb.AddExplosionForce(
                explosionForce,
                transform.position,
                explosionRadius,
                upwardsModifier,
                ForceMode.Impulse
            );

        }

    }
}
