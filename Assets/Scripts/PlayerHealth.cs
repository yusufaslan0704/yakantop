using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public bool isEliminated = false;

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
        if (isEliminated) return;

        isEliminated = true;

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
    }

    public void Revive()
    {
        isEliminated = false;

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
    }
}