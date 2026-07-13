using UnityEngine;

// Atıcı nişan çözümlemesi — camera raycast / body forward / steep-down.
// PlayerThrow bu sinifi kullanir; aim mantigi tek yerde kalir.
public sealed class ThrowAimResolver
{
    public struct Context
    {
        public Transform Body;
        public Transform ThrowPoint;
        public Camera PlayerCamera;
        public PlayerInputHandler Input;
        public PlayerAirLift AirLift;
        public float AimDistance;
    }

    Context ctx;

    public void SetContext(Context context)
    {
        ctx = context;
    }

    public Vector3 GetThrowDirection()
    {
        bool useForwardAim = ctx.Input != null &&
                             ctx.Input.scheme == ControlScheme.Gamepad;

        if (useForwardAim || ctx.PlayerCamera == null)
        {
            return GetBodyForwardAim();
        }

        return GetCameraAimDirection();
    }

    public void RotateBodyToThrowDirection(Vector3 throwDirection)
    {
        if (ctx.Body == null)
        {
            return;
        }

        Vector3 lookDirection = throwDirection;
        lookDirection.y = 0f;

        if (lookDirection != Vector3.zero)
        {
            ctx.Body.rotation = Quaternion.LookRotation(lookDirection);
        }
    }

    Vector3 GetBodyForwardAim()
    {
        if (AllowSteepDownAim() && ctx.PlayerCamera != null)
        {
            return GetCameraForwardAim();
        }

        if (ctx.Body == null)
        {
            return Vector3.zero;
        }

        Vector3 forward = ctx.Body.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.0001f)
        {
            return Vector3.zero;
        }

        return (forward.normalized + Vector3.up * 0.12f).normalized;
    }

    Vector3 GetCameraAimDirection()
    {
        Ray ray = ctx.PlayerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        float defaultRange = Mathf.Min(ctx.AimDistance, 38f);
        Vector3 targetPoint = ray.GetPoint(defaultRange);

        RaycastHit[] hits = Physics.RaycastAll(ray, ctx.AimDistance, ~0, QueryTriggerInteraction.Ignore);
        float bestDist = float.MaxValue;
        bool lockedOntoPlayer = false;
        bool steepDown = AllowSteepDownAim();
        float minHitDistance = steepDown ? 1.15f : 3.5f;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null) continue;

            if (ctx.Body != null &&
                (hit.collider.transform == ctx.Body || hit.collider.transform.IsChildOf(ctx.Body)))
            {
                continue;
            }

            if (hit.distance < minHitDistance)
            {
                continue;
            }

            PlayerRole hitRole = hit.collider.GetComponentInParent<PlayerRole>();
            bool isEscapeTarget = hitRole != null &&
                                  (hitRole.roleType == RoleType.Runner || hitRole.roleType == RoleType.Saver);

            bool isFloor = hit.normal.y > 0.55f;
            Vector3 aimPoint = hit.point;
            if (isFloor && !isEscapeTarget && !steepDown)
            {
                float aimHeight = ctx.ThrowPoint != null ? ctx.ThrowPoint.position.y : 1.1f;
                aimPoint = new Vector3(hit.point.x, aimHeight, hit.point.z);
            }

            if (isEscapeTarget)
            {
                if (!lockedOntoPlayer || hit.distance < bestDist)
                {
                    lockedOntoPlayer = true;
                    bestDist = hit.distance;
                    targetPoint = aimPoint;
                }

                continue;
            }

            if (lockedOntoPlayer) continue;

            if (hit.distance < bestDist)
            {
                bestDist = hit.distance;
                targetPoint = aimPoint;
            }
        }

        if (PlayerFlash.AreThrowersBlinded())
        {
            targetPoint += Random.insideUnitSphere * 3.5f;
        }

        if (ctx.ThrowPoint == null)
        {
            return GetCameraForwardAim();
        }

        Vector3 dir = targetPoint - ctx.ThrowPoint.position;
        if (dir.sqrMagnitude < 0.01f)
        {
            return GetCameraForwardAim();
        }

        Vector3 flat = dir;
        flat.y = 0f;
        Vector3 bodyFlat = ctx.Body != null ? ctx.Body.forward : Vector3.forward;
        bodyFlat.y = 0f;

        if (!steepDown)
        {
            if (flat.sqrMagnitude < 0.35f ||
                (bodyFlat.sqrMagnitude > 0.01f && Vector3.Dot(flat.normalized, bodyFlat.normalized) < 0.15f))
            {
                return GetCameraForwardAim();
            }
        }
        else if (bodyFlat.sqrMagnitude > 0.01f &&
                 flat.sqrMagnitude > 0.35f &&
                 Vector3.Dot(flat.normalized, bodyFlat.normalized) < -0.2f)
        {
            return GetCameraForwardAim();
        }

        return ClampThrowDirection(dir.normalized, steepDown);
    }

    Vector3 GetCameraForwardAim()
    {
        Vector3 forward = ctx.PlayerCamera.transform.forward;
        return ClampThrowDirection(forward, AllowSteepDownAim());
    }

    Vector3 ClampThrowDirection(Vector3 forward, bool steepDown)
    {
        float minY = steepDown ? -0.95f : -0.35f;
        float maxY = 0.55f;
        forward.y = Mathf.Clamp(forward.y, minY, maxY);

        if (forward.sqrMagnitude < 0.0001f)
        {
            return steepDown ? Vector3.down : GetBodyForwardAimFlat();
        }

        return forward.normalized;
    }

    Vector3 GetBodyForwardAimFlat()
    {
        if (ctx.Body == null)
        {
            return Vector3.forward;
        }

        Vector3 forward = ctx.Body.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
        {
            return Vector3.forward;
        }

        return (forward.normalized + Vector3.up * 0.12f).normalized;
    }

    bool AllowSteepDownAim()
    {
        return ctx.AirLift != null && ctx.AirLift.IsElevated;
    }
}
