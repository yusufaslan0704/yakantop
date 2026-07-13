using UnityEngine;

// Atis fizik yardimcilari — mass / path hizi tek yerde.
// Preview ve gercek atis ayni formulu kullanmali.
public static class ThrowPhysics
{
    public const float MinMass = 0.05f;
    public const float MinPathSpeed = 4f;

    public static float ResolvePrefabMass(BallData data)
    {
        if (data == null || data.prefab == null)
        {
            return 1f;
        }

        Rigidbody rb = data.prefab.GetComponent<Rigidbody>();
        return rb != null ? Mathf.Max(MinMass, rb.mass) : 1f;
    }

    public static float PathSpeedFromForce(float force, BallData data)
    {
        float mass = ResolvePrefabMass(data);
        return Mathf.Max(MinPathSpeed, force / Mathf.Max(MinMass, mass));
    }

    public static float PathSpeedFromForce(float force, float mass)
    {
        return Mathf.Max(MinPathSpeed, force / Mathf.Max(MinMass, mass));
    }
}
