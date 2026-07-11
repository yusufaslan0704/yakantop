using System.Collections.Generic;
using UnityEngine;

// Trap Ball yere/duvara dusunce olusan yavaslatma alani.
// Sadece Runner/Saver etkilenir; aticilar etkilenmez.
public class TrapZone : MonoBehaviour
{
    static readonly List<TrapZone> Active = new List<TrapZone>();

    public float radius = 3.2f;
    public float duration = 4.5f;
    public float speedMultiplier = 0.42f;
    public Color zoneColor = new Color(1f, 0.35f, 0.12f, 0.55f);

    float dieAt;
    Transform visual;

    public static TrapZone Spawn(Vector3 position, float radius, float duration, float speedMultiplier)
    {
        Vector3 pos = position;
        pos.y = 0.05f;

        GameObject root = new GameObject("TrapZone");
        root.transform.position = pos;
        SceneFolders.ParentTo(root.transform, SceneFolders.RuntimeSpawned);

        TrapZone zone = root.AddComponent<TrapZone>();
        zone.radius = Mathf.Max(0.5f, radius);
        zone.duration = Mathf.Max(0.5f, duration);
        zone.speedMultiplier = Mathf.Clamp(speedMultiplier, 0.15f, 0.95f);
        zone.BuildVisual();
        zone.dieAt = Time.unscaledTime + zone.duration;

        Active.Add(zone);
        return zone;
    }

    public static float GetSpeedMultiplier(Vector3 worldPosition, RoleType? role)
    {
        if (role == RoleType.Thrower) return 1f;
        if (Active.Count == 0) return 1f;

        float slowest = 1f;
        Vector2 flat = new Vector2(worldPosition.x, worldPosition.z);

        for (int i = 0; i < Active.Count; i++)
        {
            TrapZone z = Active[i];
            if (z == null) continue;

            Vector2 center = new Vector2(z.transform.position.x, z.transform.position.z);
            float r = z.radius;
            if ((flat - center).sqrMagnitude <= r * r)
            {
                slowest = Mathf.Min(slowest, z.speedMultiplier);
            }
        }

        return slowest;
    }

    public static void ClearAll()
    {
        for (int i = Active.Count - 1; i >= 0; i--)
        {
            if (Active[i] != null)
            {
                Object.Destroy(Active[i].gameObject);
            }
        }

        Active.Clear();
    }

    void BuildVisual()
    {
        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = "TrapDisc";
        disc.transform.SetParent(transform, false);
        disc.transform.localPosition = Vector3.zero;
        disc.transform.localScale = new Vector3(radius * 2f, 0.04f, radius * 2f);
        Object.Destroy(disc.GetComponent<Collider>());

        Renderer rend = disc.GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", zoneColor);
            }
            else
            {
                mat.color = zoneColor;
            }

            rend.sharedMaterial = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
        }

        visual = disc.transform;

        // Dis halka
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "TrapRing";
        ring.transform.SetParent(transform, false);
        ring.transform.localPosition = Vector3.up * 0.02f;
        ring.transform.localScale = new Vector3(radius * 2.05f, 0.02f, radius * 2.05f);
        Object.Destroy(ring.GetComponent<Collider>());

        Renderer ringRend = ring.GetComponent<Renderer>();
        if (ringRend != null)
        {
            Color edge = zoneColor;
            edge.a = 0.85f;
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", edge);
            }
            else
            {
                mat.color = edge;
            }

            ringRend.sharedMaterial = mat;
            ringRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ringRend.receiveShadows = false;
        }
    }

    void Update()
    {
        if (!GameManager.RoundIsActive || Time.unscaledTime >= dieAt)
        {
            Destroy(gameObject);
            return;
        }

        if (visual != null)
        {
            float pulse = 1f + 0.04f * Mathf.Sin(Time.unscaledTime * 6f);
            visual.localScale = new Vector3(radius * 2f * pulse, 0.04f, radius * 2f * pulse);
        }
    }

    void OnDestroy()
    {
        Active.Remove(this);
    }
}
