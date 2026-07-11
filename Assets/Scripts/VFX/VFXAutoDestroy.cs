using UnityEngine;

// Tum kisa omurlu efektler icin tek script:
// Belirli sure sonra kendini yok eder, istenirse o sirada buyur.
public class VFXAutoDestroy : MonoBehaviour
{
    public float lifeTime = 0.6f;

    [Tooltip("0 ise efekt buyumez, sadece suresi dolunca yok olur.")]
    public float growSpeed = 0f;

    [Tooltip("Aciksa olcek buyurken alpha da solar (CombatVfx burst'leri).")]
    public bool fadeAlpha;

    private Renderer[] renderers;
    private Material[] materials;
    private float elapsed;

    void Start()
    {
        if (fadeAlpha)
        {
            CacheMaterials();
        }

        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        elapsed += Time.deltaTime;

        if (growSpeed > 0f)
        {
            transform.localScale += Vector3.one * growSpeed * Time.deltaTime;
        }

        if (fadeAlpha && materials != null)
        {
            float alpha = 1f - Mathf.Clamp01(elapsed / Mathf.Max(0.01f, lifeTime));
            foreach (Material mat in materials)
            {
                if (mat == null) continue;
                SetAlpha(mat, alpha);
            }
        }
    }

    void OnDestroy()
    {
        if (materials == null) return;

        foreach (Material mat in materials)
        {
            if (mat != null)
            {
                Destroy(mat);
            }
        }
    }

    void CacheMaterials()
    {
        renderers = GetComponentsInChildren<Renderer>();
        materials = new Material[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            materials[i] = renderers[i].material;
        }
    }

    static void SetAlpha(Material mat, float alpha)
    {
        if (mat.HasProperty("_BaseColor"))
        {
            Color c = mat.GetColor("_BaseColor");
            c.a = alpha;
            mat.SetColor("_BaseColor", c);
        }

        if (mat.HasProperty("_Color"))
        {
            Color c = mat.color;
            c.a = alpha;
            mat.color = c;
        }
    }
}
