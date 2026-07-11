using UnityEngine;

// Dash afterimage / wind parcaciklarinin hizla solmasi.
public class DashGhostFade : MonoBehaviour
{
    public float duration = 0.22f;
    public float startAlpha = 0.55f;

    private Renderer[] renderers;
    private Material[] materials;
    private float elapsed;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            Destroy(gameObject);
            return;
        }

        materials = new Material[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            materials[i] = renderers[i].material;
            SetMaterialAlpha(materials[i], startAlpha);
        }
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;
        float alpha = Mathf.Lerp(startAlpha, 0f, t);

        foreach (Material mat in materials)
        {
            if (mat != null)
            {
                SetMaterialAlpha(mat, alpha);
            }
        }

        if (elapsed >= duration)
        {
            Destroy(gameObject);
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

    static void SetMaterialAlpha(Material mat, float alpha)
    {
        if (mat.HasProperty("_BaseColor"))
        {
            Color color = mat.GetColor("_BaseColor");
            color.a = alpha;
            mat.SetColor("_BaseColor", color);
        }

        if (mat.HasProperty("_Color"))
        {
            Color color = mat.GetColor("_Color");
            color.a = alpha;
            mat.SetColor("_Color", color);
        }
    }
}
