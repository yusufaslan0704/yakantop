using UnityEngine;
using UnityEngine.Rendering;

// Atıcı 8. özellik: runner kamerasından görünmez.
// Kendi (atici) kamerasında ghost; runner/saver kamerası layer cull ile hiç görmez.
[DefaultExecutionOrder(41)]
public class PlayerThrowerInvis : MonoBehaviour
{
    public const string InvisibleLayerName = "InvisibleToRunner";

    [Header("Invisibility")]
    public float duration = 4f;
    public float cooldown = 12f;

    [Header("Self Feedback")]
    [Range(0.08f, 0.7f)]
    public float selfGhostAlpha = 0.28f;

    public bool IsReady => Time.unscaledTime >= nextReadyTime && !IsInvisible;
    public bool IsInvisible { get; private set; }

    static int invisibleLayer = -1;

    float nextReadyTime;
    float invisibleUntil;
    int[] originalLayers;
    Transform[] layeredTransforms;

    Renderer[] ghostRenderers;
    Material[][] originalSharedMaterials;
    Material[][] ghostSharedMaterials;

    PlayerHealth playerHealth;
    PlayerInputHandler inputHandler;
    PlayerRole playerRole;
    PlayerThrow playerThrow;

    void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        inputHandler = GetComponent<PlayerInputHandler>();
        playerRole = GetComponent<PlayerRole>();
        playerThrow = GetComponent<PlayerThrow>();
        EnsureInvisibleLayer();
    }

    void OnEnable()
    {
        if (playerThrow != null)
        {
            playerThrow.OnBallThrown += HandleBallThrown;
        }
    }

    void OnDisable()
    {
        if (playerThrow != null)
        {
            playerThrow.OnBallThrown -= HandleBallThrown;
        }

        if (IsInvisible)
        {
            EndInvisibility();
        }
    }

    void Update()
    {
        if (IsInvisible && Time.unscaledTime >= invisibleUntil)
        {
            EndInvisibility();
        }

        if (!CanUse())
        {
            return;
        }

        if (inputHandler != null && inputHandler.enabled && inputHandler.InvisibilityPressed && IsReady)
        {
            Activate();
        }
    }

    bool CanUse()
    {
        if (!isActiveAndEnabled) return false;
        if (!GameManager.RoundIsActive) return false;
        if (playerHealth != null && playerHealth.IsEliminated) return false;
        if (playerRole != null && playerRole.roleType != RoleType.Thrower) return false;
        if (playerThrow == null || !playerThrow.InvisModeSelected) return false;
        if (EmoteWheelUI.IsOpen) return false;
        return true;
    }

    void Activate()
    {
        IsInvisible = true;
        invisibleUntil = Time.unscaledTime + duration;
        nextReadyTime = Time.unscaledTime + cooldown;

        ApplyInvisibleLayer();
        ApplySelfGhost();
        EnsureRunnerCameraCulls();

        CameraShake.ShakeAll(0.05f, 0.06f);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDash();
        }
    }

    void EndInvisibility()
    {
        if (!IsInvisible) return;

        IsInvisible = false;
        invisibleUntil = 0f;
        RestoreSelfGhost();
        RestoreLayers();
        EnsureRunnerCameraCulls();
    }

    void HandleBallThrown()
    {
        if (IsInvisible)
        {
            EndInvisibility();
        }
    }

    void EnsureRunnerCameraCulls()
    {
        if (SplitScreenManager.Instance != null)
        {
            SplitScreenManager.RefreshRunnerCulling();
        }
    }

    void ApplyInvisibleLayer()
    {
        EnsureInvisibleLayer();
        if (invisibleLayer < 0) return;

        Transform[] all = GetComponentsInChildren<Transform>(true);
        layeredTransforms = all;
        originalLayers = new int[all.Length];

        for (int i = 0; i < all.Length; i++)
        {
            originalLayers[i] = all[i].gameObject.layer;
            all[i].gameObject.layer = invisibleLayer;
        }
    }

    void RestoreLayers()
    {
        if (layeredTransforms == null || originalLayers == null) return;

        for (int i = 0; i < layeredTransforms.Length; i++)
        {
            if (layeredTransforms[i] != null)
            {
                layeredTransforms[i].gameObject.layer = originalLayers[i];
            }
        }

        layeredTransforms = null;
        originalLayers = null;
    }

    void ApplySelfGhost()
    {
        RestoreSelfGhost();

        Renderer[] all = GetComponentsInChildren<Renderer>(true);
        int count = 0;
        for (int i = 0; i < all.Length; i++)
        {
            if (IsBodyRenderer(all[i])) count++;
        }

        if (count == 0) return;

        ghostRenderers = new Renderer[count];
        originalSharedMaterials = new Material[count][];
        ghostSharedMaterials = new Material[count][];

        int index = 0;
        for (int i = 0; i < all.Length; i++)
        {
            Renderer rend = all[i];
            if (!IsBodyRenderer(rend)) continue;

            ghostRenderers[index] = rend;
            Material[] shared = rend.sharedMaterials;
            originalSharedMaterials[index] = shared;

            Material[] ghosts = new Material[shared.Length];
            for (int m = 0; m < shared.Length; m++)
            {
                Material source = shared[m];
                if (source == null)
                {
                    ghosts[m] = null;
                    continue;
                }

                Material ghost = new Material(source);
                ghost.name = source.name + " (ThrowerInvisGhost)";
                Color baseColor = ReadColor(ghost);
                baseColor.a = selfGhostAlpha;
                SetupTransparent(ghost, baseColor);
                ghosts[m] = ghost;
            }

            ghostSharedMaterials[index] = ghosts;
            rend.sharedMaterials = ghosts;
            index++;
        }
    }

    void RestoreSelfGhost()
    {
        if (ghostRenderers == null) return;

        for (int i = 0; i < ghostRenderers.Length; i++)
        {
            Renderer rend = ghostRenderers[i];
            Material[] originals = originalSharedMaterials != null ? originalSharedMaterials[i] : null;
            Material[] ghosts = ghostSharedMaterials != null ? ghostSharedMaterials[i] : null;

            if (rend != null && originals != null)
            {
                rend.sharedMaterials = originals;
            }

            if (ghosts != null)
            {
                for (int m = 0; m < ghosts.Length; m++)
                {
                    if (ghosts[m] != null)
                    {
                        Destroy(ghosts[m]);
                    }
                }
            }
        }

        ghostRenderers = null;
        originalSharedMaterials = null;
        ghostSharedMaterials = null;
    }

    static bool IsBodyRenderer(Renderer rend)
    {
        if (rend == null) return false;
        return rend is MeshRenderer || rend is SkinnedMeshRenderer;
    }

    static Color ReadColor(Material mat)
    {
        if (mat.HasProperty("_BaseColor")) return mat.GetColor("_BaseColor");
        if (mat.HasProperty("_Color")) return mat.GetColor("_Color");
        return mat.color;
    }

    static void SetupTransparent(Material mat, Color color)
    {
        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        }
        else
        {
            mat.SetFloat("_Mode", 3f);
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        mat.color = color;
    }

    public float GetCooldownPercent()
    {
        if (IsInvisible)
        {
            return Mathf.Clamp01((invisibleUntil - Time.unscaledTime) / Mathf.Max(0.01f, duration));
        }

        if (cooldown <= 0f) return 1f;

        float remaining = nextReadyTime - Time.unscaledTime;
        if (remaining <= 0f) return 1f;

        return 1f - Mathf.Clamp01(remaining / cooldown);
    }

    public static int GetInvisibleLayer()
    {
        EnsureInvisibleLayer();
        return invisibleLayer;
    }

    static void EnsureInvisibleLayer()
    {
        if (invisibleLayer >= 0) return;

        invisibleLayer = LayerMask.NameToLayer(InvisibleLayerName);
        if (invisibleLayer < 0)
        {
            invisibleLayer = 7;
        }
    }

    void OnDestroy()
    {
        if (IsInvisible)
        {
            RestoreSelfGhost();
            RestoreLayers();
        }
    }
}
