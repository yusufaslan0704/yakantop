using UnityEngine;

// Atıcı gölge kopyası: durduğu yerde hayalet; asıl atıcı atınca
// ayni hedefe çapraz ikinci top firlatir.
public class ThrowerShadowClone : MonoBehaviour
{
    public float lifetime = 5f;
    public float ghostAlpha = 0.4f;

    float dieAt;
    bool dead;
    Renderer[] ghostRenderers;
    Material[] ghostMats;

    public bool IsAlive => !dead && this != null;

    public void Init(float duration, float alpha)
    {
        lifetime = duration;
        ghostAlpha = alpha;
        dieAt = Time.unscaledTime + duration;
        ApplyGhostLook();
    }

    void Update()
    {
        if (dead) return;

        if (Time.unscaledTime >= dieAt)
        {
            Despawn();
        }
    }

    public Vector3 GetThrowOrigin()
    {
        return transform.position + Vector3.up * 1.2f + transform.forward * 0.9f;
    }

    void ApplyGhostLook()
    {
        Renderer[] all = GetComponentsInChildren<Renderer>(true);
        var list = new System.Collections.Generic.List<Renderer>();
        var mats = new System.Collections.Generic.List<Material>();

        for (int i = 0; i < all.Length; i++)
        {
            Renderer rend = all[i];
            if (rend == null) continue;
            if (!(rend is MeshRenderer || rend is SkinnedMeshRenderer)) continue;

            Material[] shared = rend.sharedMaterials;
            Material[] copies = new Material[shared.Length];
            for (int m = 0; m < shared.Length; m++)
            {
                if (shared[m] == null)
                {
                    copies[m] = null;
                    continue;
                }

                Material ghost = new Material(shared[m]);
                ghost.name = shared[m].name + " (ShadowGhost)";
                Color c = ghost.HasProperty("_BaseColor") ? ghost.GetColor("_BaseColor") : ghost.color;
                c.a = ghostAlpha;
                if (ghost.HasProperty("_BaseColor")) ghost.SetColor("_BaseColor", c);
                if (ghost.HasProperty("_Color")) ghost.SetColor("_Color", c);
                ghost.color = c;

                if (ghost.HasProperty("_Surface"))
                {
                    ghost.SetFloat("_Surface", 1f);
                    ghost.SetInt("_ZWrite", 0);
                    ghost.renderQueue = 3000;
                    ghost.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                }

                copies[m] = ghost;
                mats.Add(ghost);
            }

            rend.sharedMaterials = copies;
            list.Add(rend);
        }

        ghostRenderers = list.ToArray();
        ghostMats = mats.ToArray();
    }

    public void Despawn()
    {
        if (dead) return;
        dead = true;

        if (ghostMats != null)
        {
            for (int i = 0; i < ghostMats.Length; i++)
            {
                if (ghostMats[i] != null)
                {
                    Destroy(ghostMats[i]);
                }
            }
        }

        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (ghostMats == null) return;
        for (int i = 0; i < ghostMats.Length; i++)
        {
            if (ghostMats[i] != null)
            {
                Destroy(ghostMats[i]);
            }
        }
    }
}
