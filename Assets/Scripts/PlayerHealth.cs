using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Elenme Savrulması")]
    [Tooltip("Savrulmaya eklenen yukarı itiş.")]
    public float flingUpForce = 3.2f;

    [Tooltip("Devrilme dönüşü gücü.")]
    public float flingTorque = 7.5f;

    [Tooltip("Savrulduktan kaç saniye sonra yerde donup kalır.")]
    public float freezeDelay = 1.05f;

    public bool IsEliminated { get; private set; }

    // Diğer scriptler her kare durum kontrol etmek yerine bu olaylara abone olur.
    public event System.Action OnEliminated;
    public event System.Action OnRevived;

    private Renderer[] renderers;
    private Color[] originalColors;

    private Rigidbody rb;
    private RigidbodyConstraints originalConstraints;
    private Coroutine freezeRoutine;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        RefreshRenderers();

        if (rb != null)
        {
            // Y ekseni dahil donmeyi kilitle; yon sadece MoveRotation ile verilir.
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            originalConstraints = rb.constraints;
        }
    }

    // Gorsel model calisma aninda degisebilir (kapsul -> Mixamo modeli).
    // Renk isleri tum child renderer'lara uygulanir; model degisince yeniden taranir.
    public void RefreshRenderers()
    {
        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;

            Material mat = renderers[i].material;
            if (mat == null) continue;

            if (mat.HasProperty("_BaseColor"))
            {
                originalColors[i] = mat.GetColor("_BaseColor");
            }
            else if (mat.HasProperty("_Color"))
            {
                originalColors[i] = mat.GetColor("_Color");
            }
            else
            {
                originalColors[i] = mat.color;
            }
        }

        if (IsEliminated)
        {
            SetColor(Color.red);
        }
    }

    void SetColor(Color color)
    {
        if (renderers == null) return;

        foreach (Renderer r in renderers)
        {
            if (r == null) continue;

            Material mat = r.material;
            if (mat == null) continue;

            // URP Lit _BaseColor kullanir; eski Standard .color / _Color.
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", color);
            }

            if (mat.HasProperty("_Color"))
            {
                mat.SetColor("_Color", color);
            }

            mat.color = color;
        }
    }

    void RestoreColors()
    {
        if (renderers == null) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;

            Material mat = renderers[i].material;
            if (mat == null) continue;

            Color color = originalColors[i];

            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", color);
            }

            if (mat.HasProperty("_Color"))
            {
                mat.SetColor("_Color", color);
            }

            mat.color = color;
        }
    }

    public void Eliminate()
    {
        Eliminate(Vector3.zero);
    }

    // knockbackImpulse: topun çarpma yönü ve gücü (BallData'dan gelir).
    public void Eliminate(Vector3 knockbackImpulse)
    {
        if (IsEliminated) return;

        IsEliminated = true;

        Debug.Log(gameObject.name + " elendi!");

        SetColor(Color.red);

        // Kinematik cisimlere (örn. TargetDummy) kuvvet uygulanamaz.
        if (rb != null && !rb.isKinematic)
        {
            // Dönüş kilitlerini açıyoruz ki karakter savrulup devrilebilsin.
            rb.constraints = RigidbodyConstraints.None;

            rb.AddForce(knockbackImpulse + Vector3.up * flingUpForce, ForceMode.Impulse);
            rb.AddTorque(Random.onUnitSphere * flingTorque, ForceMode.Impulse);

            // Kısa süre sonra yerde dondur, sonsuza kadar yuvarlanmasın.
            freezeRoutine = StartCoroutine(FreezeAfterDelay());
        }

        OnEliminated?.Invoke();
    }

    IEnumerator FreezeAfterDelay()
    {
        yield return new WaitForSeconds(freezeDelay);

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        freezeRoutine = null;
    }

    public void Revive()
    {
        if (!IsEliminated) return;

        IsEliminated = false;

        Debug.Log(gameObject.name + " oyuna geri döndü!");

        if (freezeRoutine != null)
        {
            StopCoroutine(freezeRoutine);
            freezeRoutine = null;
        }

        RestoreColors();

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.constraints = originalConstraints;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Karakteri tekrar dik konuma getir (yön korunur).
        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        OnRevived?.Invoke();
    }
}
