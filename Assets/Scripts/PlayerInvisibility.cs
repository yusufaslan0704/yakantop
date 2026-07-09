using UnityEngine;

// Runner/Saver: V ile 4 sn görünmezlik.
// Atıcı kamerası layer cull ile görmez; bot hedef almaz.
// Materyal degistirilmez (URP magenta bug'ini onlemek icin).
public class PlayerInvisibility : MonoBehaviour
{
    public const string InvisibleLayerName = "InvisibleToThrower";

    [Header("Invisibility")]
    public float duration = 4f;
    public float cooldown = 12f;

    public bool IsReady => Time.unscaledTime >= nextReadyTime && !IsInvisible;
    public bool IsInvisible { get; private set; }

    public event System.Action OnInvisibilityStarted;
    public event System.Action OnInvisibilityEnded;

    static int invisibleLayer = -1;

    float nextReadyTime;
    float invisibleUntil;
    int[] originalLayers;
    Transform[] layeredTransforms;

    PlayerHealth playerHealth;
    PlayerInputHandler inputHandler;
    PlayerRole playerRole;

    void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        inputHandler = GetComponent<PlayerInputHandler>();
        playerRole = GetComponent<PlayerRole>();
        EnsureInvisibleLayer();
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

        if (playerRole != null &&
            playerRole.roleType != RoleType.Runner &&
            playerRole.roleType != RoleType.Saver)
        {
            return false;
        }

        return true;
    }

    void Activate()
    {
        IsInvisible = true;
        invisibleUntil = Time.unscaledTime + duration;
        nextReadyTime = Time.unscaledTime + cooldown;

        ApplyInvisibleLayer();
        SplitScreenManager.RefreshThrowerCulling();

        OnInvisibilityStarted?.Invoke();
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
        RestoreLayers();
        SplitScreenManager.RefreshThrowerCulling();
        OnInvisibilityEnded?.Invoke();
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

    public static bool IsPlayerInvisible(Component player)
    {
        if (player == null) return false;
        PlayerInvisibility invis = player.GetComponentInParent<PlayerInvisibility>();
        return invis != null && invis.IsInvisible;
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
            invisibleLayer = 6;
        }
    }

    void OnDisable()
    {
        if (IsInvisible)
        {
            EndInvisibility();
        }
    }

    void OnDestroy()
    {
        if (IsInvisible)
        {
            RestoreLayers();
        }
    }
}
