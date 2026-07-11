using System.Collections.Generic;
using UnityEngine;

// Charge onizlemesi ile atis ayni yolu paylassin diye ortak yay + egri hesabi.
public static class ThrowPathBuilder
{
    public const int DefaultMaxSteps = 28;
    public const float DefaultStepTime = 0.045f;

    public static void Build(
        Vector3 origin,
        Vector3 direction,
        float force,
        float mass,
        float curveBias,
        float curveForce,
        List<Vector3> points,
        out Vector3 impact,
        out bool hasImpact,
        Transform ignoreRoot = null,
        int maxSteps = DefaultMaxSteps,
        float stepTime = DefaultStepTime)
    {
        points.Clear();
        impact = origin;
        hasImpact = false;

        if (direction.sqrMagnitude < 0.0001f || force <= 0.01f)
        {
            points.Add(origin);
            return;
        }

        mass = Mathf.Max(0.05f, mass);
        Vector3 pos = origin;
        Vector3 vel = direction.normalized * (force / mass);
        Vector3 gravity = Physics.gravity;

        // Egri yonu: atis yonune dik yatay (mouse saga = +side).
        Vector3 flatFwd = direction;
        flatFwd.y = 0f;
        if (flatFwd.sqrMagnitude < 0.0001f)
        {
            flatFwd = Vector3.forward;
        }

        flatFwd.Normalize();
        Vector3 side = Vector3.Cross(Vector3.up, flatFwd).normalized;

        points.Add(pos);

        bool applyCurve = curveForce > 0f && Mathf.Abs(curveBias) >= 0.02f;
        float curveStrength = applyCurve
            ? curveForce * Mathf.Lerp(0.55f, 1.55f, Mathf.Abs(curveBias)) * Mathf.Sign(curveBias)
            : 0f;

        for (int i = 0; i < maxSteps; i++)
        {
            if (applyCurve)
            {
                // ForceMode.Force benzeri: a = F/m
                vel += side * (curveStrength / mass) * stepTime;
            }

            Vector3 nextVel = vel + gravity * stepTime;
            Vector3 delta = vel * stepTime;
            float dist = delta.magnitude;

            if (dist > 0.0001f &&
                Physics.Raycast(pos, delta.normalized, out RaycastHit hit, dist, ~0, QueryTriggerInteraction.Ignore))
            {
                if (!IsIgnored(hit.collider, ignoreRoot))
                {
                    points.Add(hit.point);
                    impact = hit.point;
                    hasImpact = true;
                    return;
                }
            }

            pos += delta;
            vel = nextVel;
            points.Add(pos);

            if (pos.y < -5f)
            {
                break;
            }
        }
    }

    static bool IsIgnored(Collider col, Transform ignoreRoot)
    {
        if (col == null) return true;
        if (ignoreRoot == null) return false;
        return col.transform == ignoreRoot || col.transform.IsChildOf(ignoreRoot);
    }
}
