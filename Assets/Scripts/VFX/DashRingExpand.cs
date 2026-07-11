using UnityEngine;

// Dash baslangic halkasinin hizla genisleyip solmasi.
public class DashRingExpand : MonoBehaviour
{
    public float duration = 0.18f;
    public float endScale = 3.2f;

    private Vector3 startScale;
    private float elapsed;
    private Renderer rend;
    private Material mat;

    void Awake()
    {
        startScale = transform.localScale;
        rend = GetComponent<Renderer>();

        if (rend != null)
        {
            mat = rend.material;
        }
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;

        float scale = Mathf.Lerp(1f, endScale, t);
        transform.localScale = new Vector3(startScale.x * scale, startScale.y, startScale.z * scale);

        if (mat != null)
        {
            Color color = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.color;
            color.a = Mathf.Lerp(color.a, 0f, t);
            mat.color = color;

            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", color);
            }
        }

        if (elapsed >= duration)
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (mat != null)
        {
            Destroy(mat);
        }
    }
}
