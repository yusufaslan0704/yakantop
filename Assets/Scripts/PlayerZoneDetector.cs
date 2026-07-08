using UnityEngine;

public class PlayerZoneDetector : MonoBehaviour
{
    [Header("Zone Info")]
    public string currentZone = "None";

    [Header("Rules")]
    public bool enforceZoneRules = true;

    private PlayerRole playerRole;
    private PlayerHealth playerHealth;
    private Vector3 lastAllowedPosition;

    void Awake()
    {
        playerRole = GetComponent<PlayerRole>();
        playerHealth = GetComponent<PlayerHealth>();
    }

    void Start()
    {
        lastAllowedPosition = transform.position;
    }

    void LateUpdate()
    {
        if (!enforceZoneRules) return;

        if (playerHealth != null && playerHealth.isEliminated)
        {
            return;
        }

        if (IsCurrentZoneAllowed())
        {
            lastAllowedPosition = transform.position;
        }
        else
        {
            Vector3 currentPosition = transform.position;

            transform.position = new Vector3(
                lastAllowedPosition.x,
                currentPosition.y,
                lastAllowedPosition.z
            );

            Debug.LogWarning(gameObject.name + " izin verilmeyen bölgeye girmeye çalıştı, geri alındı.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleZoneEnterOrStay(other);
    }

    private void OnTriggerStay(Collider other)
    {
        HandleZoneEnterOrStay(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("RunnerZone") || other.CompareTag("ThrowerZone"))
        {
            currentZone = "None";
        }
    }

    void HandleZoneEnterOrStay(Collider other)
    {
        if (other.CompareTag("RunnerZone"))
        {
            currentZone = "RunnerZone";
        }
        else if (other.CompareTag("ThrowerZone"))
        {
            currentZone = "ThrowerZone";
        }
    }

    bool IsCurrentZoneAllowed()
    {
        if (playerRole == null)
        {
            return true;
        }

        if (playerRole.roleType == RoleType.Runner)
        {
            return currentZone == "RunnerZone";
        }

        if (playerRole.roleType == RoleType.Saver)
        {
            return currentZone == "RunnerZone";
        }

        if (playerRole.roleType == RoleType.Thrower)
        {
            return currentZone == "ThrowerZone";
        }

        return true;
    }
}