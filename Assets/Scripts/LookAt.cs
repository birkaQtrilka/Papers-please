using UnityEngine;

public class LookAt : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float lerpTime = 5f;
    [SerializeField] Quaternion offsetRotation = Quaternion.identity;
    [SerializeField] Vector2 constrainZ;

    void FixedUpdate()
    {
        if (target == null) return;

        // Build target rotation without touching Euler at all
        Quaternion targetRot = Quaternion.Inverse(target.rotation) * offsetRotation;

        // Clamp Z by isolating and clamping only the Z component of the quaternion
        targetRot = ClampZ(targetRot, constrainZ.x, constrainZ.y);

        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            targetRot,
            lerpTime * Time.fixedDeltaTime
        );
    }

    Quaternion ClampZ(Quaternion q, float minDeg, float maxDeg)
    {
        q = NormalizeQuaternion(q);

        Quaternion zOnly = new Quaternion(0f, 0f, q.z, q.w);
        zOnly = NormalizeQuaternion(zOnly);

        float zAngle = 2f * Mathf.Rad2Deg * Mathf.Atan2(zOnly.z, zOnly.w);

        float clampedZ = Mathf.Clamp(zAngle, minDeg, maxDeg);

        Quaternion clampedZRot = Quaternion.AngleAxis(clampedZ, Vector3.forward);

        Quaternion withoutZ = q * Quaternion.Inverse(zOnly);
        return withoutZ * clampedZRot;
    }

    Quaternion NormalizeQuaternion(Quaternion q)
    {
        float mag = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
        if (mag < 0.0001f) return Quaternion.identity;
        return new Quaternion(q.x / mag, q.y / mag, q.z / mag, q.w / mag);
    }
}