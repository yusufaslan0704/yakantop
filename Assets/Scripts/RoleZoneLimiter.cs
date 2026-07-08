using UnityEngine;

public class RoleZoneLimiter : MonoBehaviour
{
    [Header("Allowed Zone Colliders")]
    public Collider runnerZone;
    public Collider throwerZoneA;
    public Collider throwerZoneB;

    [Header("Settings")]
    public float boundaryPadding = 0.4f;

    private PlayerRole playerRole;
    private PlayerHealth playerHealth;

    void Awake()
    {
        playerRole = GetComponent<PlayerRole>();
        playerHealth = GetComponent<PlayerHealth>();
    }

    void LateUpdate()
    {
        if (playerRole == null) return;

        if (playerHealth != null && playerHealth.IsEliminated)
        {
            return;
        }

        if (playerRole.roleType == RoleType.Runner || playerRole.roleType == RoleType.Saver)
        {
            KeepInsideSingleZone(runnerZone);
        }
        else if (playerRole.roleType == RoleType.Thrower)
        {
            KeepInsideClosestThrowerZone();
        }
    }

    void KeepInsideSingleZone(Collider zone)
    {
        if (zone == null) return;

        Bounds bounds = zone.bounds;

        Vector3 pos = transform.position;

        float minX = bounds.min.x + boundaryPadding;
        float maxX = bounds.max.x - boundaryPadding;
        float minZ = bounds.min.z + boundaryPadding;
        float maxZ = bounds.max.z - boundaryPadding;

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.z = Mathf.Clamp(pos.z, minZ, maxZ);

        transform.position = pos;
    }

    void KeepInsideClosestThrowerZone()
    {
        if (throwerZoneA == null && throwerZoneB == null) return;

        if (throwerZoneA != null && IsInsideXZ(throwerZoneA.bounds, transform.position))
        {
            return;
        }

        if (throwerZoneB != null && IsInsideXZ(throwerZoneB.bounds, transform.position))
        {
            return;
        }

        Collider closestZone = GetClosestThrowerZone();

        KeepInsideSingleZone(closestZone);
    }

    Collider GetClosestThrowerZone()
    {
        if (throwerZoneA == null) return throwerZoneB;
        if (throwerZoneB == null) return throwerZoneA;

        float distanceA = Vector3.Distance(transform.position, throwerZoneA.bounds.center);
        float distanceB = Vector3.Distance(transform.position, throwerZoneB.bounds.center);

        return distanceA <= distanceB ? throwerZoneA : throwerZoneB;
    }

    bool IsInsideXZ(Bounds bounds, Vector3 position)
    {
        return position.x >= bounds.min.x + boundaryPadding &&
               position.x <= bounds.max.x - boundaryPadding &&
               position.z >= bounds.min.z + boundaryPadding &&
               position.z <= bounds.max.z - boundaryPadding;
    }
}