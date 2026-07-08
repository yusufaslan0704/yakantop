using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public bool IsEliminated { get; private set; }

    // Diğer scriptler her kare durum kontrol etmek yerine bu olaylara abone olur.
    public event System.Action OnEliminated;
    public event System.Action OnRevived;

    private Renderer playerRenderer;
    private Color originalColor;

    private Rigidbody rb;

    void Awake()
    {
        playerRenderer = GetComponent<Renderer>();
        rb = GetComponent<Rigidbody>();

        if (playerRenderer != null)
        {
            originalColor = playerRenderer.material.color;
        }
    }

    public void Eliminate()
    {
        if (IsEliminated) return;

        IsEliminated = true;

        Debug.Log(gameObject.name + " elendi!");

        if (playerRenderer != null)
        {
            playerRenderer.material.color = Color.red;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        OnEliminated?.Invoke();
    }

    public void Revive()
    {
        if (!IsEliminated) return;

        IsEliminated = false;

        Debug.Log(gameObject.name + " oyuna geri döndü!");

        if (playerRenderer != null)
        {
            playerRenderer.material.color = originalColor;
        }

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        OnRevived?.Invoke();
    }
}
