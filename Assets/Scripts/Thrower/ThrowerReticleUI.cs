using UnityEngine;
using UnityEngine.UI;

// Atıcı HUD nişangâhı: Image-based cross + charge'a göre sıkılaşma / ısınma.
// Eski TMP "+" yerine geçer.
public class ThrowerReticleUI : MonoBehaviour
{
    public static ThrowerReticleUI Instance { get; private set; }

    [Header("Layout")]
    public float armLength = 14f;
    public float armThickness = 3f;
    public float gap = 8f;
    public float centerDotSize = 4f;

    RectTransform root;
    Image[] arms;
    Image centerDot;
    CanvasGroup canvasGroup;
    PlayerThrow trackedThrow;
    static Sprite whiteSprite;
    bool built;

    public static void EnsureExists()
    {
        if (Instance != null) return;

        ThrowerReticleUI existing = FindFirstObjectByType<ThrowerReticleUI>();
        if (existing != null)
        {
            Instance = existing;
            return;
        }

        GameObject host = GameObject.Find("ThrowerReticleUI");
        if (host == null)
        {
            host = new GameObject("ThrowerReticleUI");
            DontDestroyOnLoad(host);
        }

        Instance = host.GetComponent<ThrowerReticleUI>();
        if (Instance == null)
        {
            Instance = host.AddComponent<ThrowerReticleUI>();
        }
    }

    void Awake()
    {
        Instance = this;
        Build();
        SetVisible(false);
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void LateUpdate()
    {
        if (!built) Build();

        if (!ShouldShow())
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);
        ResolveThrower();

        float charge = trackedThrow != null && trackedThrow.IsCharging()
            ? trackedThrow.GetChargePercent()
            : 0f;

        ApplyChargeVisual(charge);
    }

    bool ShouldShow()
    {
        if (!GameManager.RoundIsActive)
        {
            return false;
        }

        if (GameManager.Instance != null && GameManager.Instance.IsInLobby())
        {
            return false;
        }

        if (EmoteWheelUI.IsOpen)
        {
            return false;
        }

        ResolveThrower();
        return trackedThrow != null && trackedThrow.isActiveAndEnabled;
    }

    void ResolveThrower()
    {
        if (trackedThrow != null && trackedThrow.isActiveAndEnabled)
        {
            return;
        }

        trackedThrow = null;
        foreach (PlayerRole player in PlayerManager.All)
        {
            if (player == null || player.roleType != RoleType.Thrower) continue;

            PlayerThrow throwScript = player.GetComponent<PlayerThrow>();
            if (throwScript != null && throwScript.enabled)
            {
                trackedThrow = throwScript;
                return;
            }
        }
    }

    void ApplyChargeVisual(float charge)
    {
        Color color = Color.Lerp(UIColorPalette.Crosshair, UIColorPalette.AimArcHot, charge);
        float alpha = Mathf.Lerp(0.72f, 1f, charge);
        color.a = alpha;

        float gapNow = Mathf.Lerp(gap, gap * 0.45f, charge);
        float armNow = Mathf.Lerp(armLength, armLength * 0.85f, charge);
        float pulse = charge > 0.01f
            ? 1f + 0.04f * Mathf.Sin(Time.unscaledTime * 10f)
            : 1f;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        if (centerDot != null)
        {
            centerDot.color = color;
            float dot = Mathf.Lerp(centerDotSize, centerDotSize * 1.35f, charge) * pulse;
            centerDot.rectTransform.sizeDelta = new Vector2(dot, dot);
        }

        if (arms == null) return;

        // 0 up, 1 down, 2 left, 3 right
        PlaceArm(arms[0], new Vector2(0f, gapNow + armNow * 0.5f), new Vector2(armThickness, armNow), color);
        PlaceArm(arms[1], new Vector2(0f, -(gapNow + armNow * 0.5f)), new Vector2(armThickness, armNow), color);
        PlaceArm(arms[2], new Vector2(-(gapNow + armNow * 0.5f), 0f), new Vector2(armNow, armThickness), color);
        PlaceArm(arms[3], new Vector2(gapNow + armNow * 0.5f, 0f), new Vector2(armNow, armThickness), color);
    }

    static void PlaceArm(Image arm, Vector2 anchoredPos, Vector2 size, Color color)
    {
        if (arm == null) return;
        arm.color = color;
        RectTransform rt = arm.rectTransform;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
    }

    void SetVisible(bool visible)
    {
        if (root != null)
        {
            root.gameObject.SetActive(visible);
        }
    }

    void Build()
    {
        if (built) return;
        built = true;

        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>().enabled = false;
        }

        canvasGroup = gameObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        GameObject rootGo = new GameObject("ReticleRoot", typeof(RectTransform));
        rootGo.transform.SetParent(transform, false);
        root = rootGo.GetComponent<RectTransform>();
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.anchoredPosition = Vector2.zero;
        root.sizeDelta = Vector2.zero;

        arms = new Image[4];
        arms[0] = CreateArm("ArmUp");
        arms[1] = CreateArm("ArmDown");
        arms[2] = CreateArm("ArmLeft");
        arms[3] = CreateArm("ArmRight");

        GameObject dotGo = new GameObject("CenterDot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dotGo.transform.SetParent(root, false);
        centerDot = dotGo.GetComponent<Image>();
        centerDot.sprite = GetWhiteSprite();
        centerDot.raycastTarget = false;
        RectTransform dotRt = centerDot.rectTransform;
        dotRt.anchorMin = new Vector2(0.5f, 0.5f);
        dotRt.anchorMax = new Vector2(0.5f, 0.5f);
        dotRt.pivot = new Vector2(0.5f, 0.5f);
        dotRt.sizeDelta = new Vector2(centerDotSize, centerDotSize);

        ApplyChargeVisual(0f);
    }

    Image CreateArm(string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(root, false);
        Image img = go.GetComponent<Image>();
        img.sprite = GetWhiteSprite();
        img.raycastTarget = false;
        RectTransform rt = img.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        return img;
    }

    static Sprite GetWhiteSprite()
    {
        if (whiteSprite != null) return whiteSprite;

        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        whiteSprite = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        return whiteSprite;
    }
}
