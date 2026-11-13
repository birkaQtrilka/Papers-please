using UnityEngine;

public class ForceApplier : MonoBehaviour
{
    public Vector3 direction;
    public float force;

    public Rigidbody[] bodies;

    public void ApplyForce()
    {
        foreach (var body in bodies)
        {
            if (body != null)
            {
                body.AddForce(direction.normalized * force, ForceMode.Impulse);
            }
        }
    }
}
