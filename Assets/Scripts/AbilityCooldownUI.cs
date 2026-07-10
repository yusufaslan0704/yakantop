using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum AbilityBarType
{
    Flash,
    Shield,
    Invisibility,
    Decoy,
    Volley,
    Trap
}

// Flash/Shield/Invis/Decoy (kacan) + Volley (atici) cooldown barlari.
public class AbilityCooldownUI : MonoBehaviour
{
    [Header("Ability")]
    public AbilityBarType abilityType = AbilityBarType.Flash;

    [Header("UI")]
    public RectTransform widgetRoot;
    public Image barBackground;
    public Image barTrack;
    public Image barFill;
    public Image barGlow;
    public TMP_Text labelText;
    public TMP_Text keyHintText;

    [Header("Style")]
    public Color backgroundColor = new Color(0.04f, 0.06f, 0.1f, 0.72f);
    public Color trackColor = new Color(0.12f, 0.16f, 0.22f, 0.95f);
    public Color fillCooldownColor = new Color(0.55f, 0.45f, 0.85f, 0.95f);
    public Color fillReadyColor = new Color(0.85f, 0.75f, 1f, 1f);
    public Color fillActiveColor = new Color(1f, 1f, 0.9f, 1f);
    public Color glowReadyColor = new Color(0.7f, 0.55f, 1f, 0.4f);
    public Color labelReadyColor = new Color(0.92f, 0.88f, 1f, 1f);
    public Color labelCooldownColor = new Color(0.55f, 0.62f, 0.7f, 0.9f);

    [Header("Layout")]
    public Vector2 barSize = new Vector2(260f, 12f);
    public float barBottomOffset = 18f;
    public float loadoutBarBaseOffset = 18f;
    public float loadoutBarStep = 34f;
    public float fillInset = 2f;

    [Header("Animation")]
    public float fillSmoothSpeed = 16f;
    public float readyPulseSpeed = 4.5f;
    public float readyFlashDuration = 0.28f;

    float displayedFill = 1f;
    bool wasReady = true;
    float readyFlashTimer;
    static Sprite whiteSprite;

    PlayerFlash flash;
    PlayerShield shield;
    PlayerInvisibility invis;
    PlayerDecoy decoy;
    PlayerVolley volley;
    PlayerTrap trap;

    void Awake()
    {
        ApplyAbilityColors();
        EnsureWidgetRoot();
        BuildBar();
    }

    void Update()
    {
        if (!GameManager.RoundIsActive)
        {
            SetVisible(false);
            return;
        }

        ResolveActive();

        bool hasAbility = HasActiveAbility();
        if (!hasAbility)
        {
            SetVisible(false);
            return;
        }

        // Loadout: secili kacan yeteneklerini altta ust uste dizer.
        if (abilityType != AbilityBarType.Volley && abilityType != AbilityBarType.Trap)
        {
            int slot = AbilityLoadout.HudSlot(abilityType);
            if (slot < 0)
            {
                SetVisible(false);
                return;
            }

            float targetY = loadoutBarBaseOffset + slot * loadoutBarStep;
            if (Mathf.Abs(barBottomOffset - targetY) > 0.1f ||
                (widgetRoot != null && Mathf.Abs(widgetRoot.anchoredPosition.y - targetY) > 0.1f))
            {
                barBottomOffset = targetY;
                if (widgetRoot != null)
                {
                    widgetRoot.anchoredPosition = new Vector2(0f, barBottomOffset);
                }
            }
        }

        SetVisible(true);

        float targetFill = GetFill();
        displayedFill = Mathf.MoveTowards(displayedFill, targetFill, fillSmoothSpeed * Time.deltaTime);

        bool ready = GetReady();
        bool active = GetActive();

        if (ready && !wasReady)
        {
            readyFlashTimer = readyFlashDuration;
        }

        readyFlashTimer = Mathf.Max(0f, readyFlashTimer - Time.deltaTime);
        wasReady = ready;

        UpdateFill(ready, active);
        UpdateGlow(ready, active);
        UpdateLabels(ready, active);
    }

    bool HasActiveAbility()
    {
        switch (abilityType)
        {
            case AbilityBarType.Flash: return flash != null && flash.enabled;
            case AbilityBarType.Shield: return shield != null && shield.enabled;
            case AbilityBarType.Invisibility: return invis != null && invis.enabled;
            case AbilityBarType.Decoy: return decoy != null && decoy.enabled;
            case AbilityBarType.Volley: return volley != null && volley.enabled;
            case AbilityBarType.Trap: return trap != null && trap.enabled;
            default: return false;
        }
    }

    float GetFill()
    {
        switch (abilityType)
        {
            case AbilityBarType.Flash: return flash.GetCooldownPercent();
            case AbilityBarType.Shield: return shield.GetCooldownPercent();
            case AbilityBarType.Invisibility: return invis.GetCooldownPercent();
            case AbilityBarType.Decoy: return decoy.GetCooldownPercent();
            case AbilityBarType.Volley: return volley.GetCooldownPercent();
            case AbilityBarType.Trap: return trap.GetCooldownPercent();
            default: return 0f;
        }
    }

    bool GetReady()
    {
        switch (abilityType)
        {
            case AbilityBarType.Flash: return flash.IsReady;
            case AbilityBarType.Shield: return shield.IsReady;
            case AbilityBarType.Invisibility: return invis.IsReady;
            case AbilityBarType.Decoy: return decoy.IsReady;
            case AbilityBarType.Volley: return volley.IsReady;
            case AbilityBarType.Trap: return trap.IsReady;
            default: return false;
        }
    }

    bool GetActive()
    {
        switch (abilityType)
        {
            case AbilityBarType.Flash: return flash.IsFlashing;
            case AbilityBarType.Shield: return shield.IsShieldActive;
            case AbilityBarType.Invisibility: return invis.IsInvisible;
            case AbilityBarType.Decoy: return decoy.IsDecoyActive;
            case AbilityBarType.Volley: return volley.IsFiring;
            case AbilityBarType.Trap: return trap.IsFiring;
            default: return false;
        }
    }

    void ApplyAbilityColors()
    {
        switch (abilityType)
        {
            case AbilityBarType.Flash:
                fillCooldownColor = new Color(0.85f, 0.7f, 0.2f, 0.95f);
                fillReadyColor = new Color(1f, 0.95f, 0.55f, 1f);
                fillActiveColor = new Color(1f, 1f, 0.9f, 1f);
                glowReadyColor = new Color(1f, 0.9f, 0.4f, 0.4f);
                barBottomOffset = 18f;
                break;
            case AbilityBarType.Shield:
                fillCooldownColor = new Color(0.25f, 0.55f, 0.85f, 0.95f);
                fillReadyColor = new Color(0.45f, 0.9f, 1f, 1f);
                fillActiveColor = new Color(0.7f, 0.95f, 1f, 1f);
                glowReadyColor = new Color(0.4f, 0.85f, 1f, 0.4f);
                barBottomOffset = 120f;
                break;
            case AbilityBarType.Invisibility:
                fillCooldownColor = new Color(0.45f, 0.35f, 0.65f, 0.95f);
                fillReadyColor = new Color(0.75f, 0.65f, 1f, 1f);
                fillActiveColor = new Color(0.9f, 0.85f, 1f, 1f);
                glowReadyColor = new Color(0.65f, 0.5f, 1f, 0.4f);
                barBottomOffset = 154f;
                break;
            case AbilityBarType.Decoy:
                fillCooldownColor = new Color(0.2f, 0.55f, 0.7f, 0.95f);
                fillReadyColor = new Color(0.45f, 0.9f, 1f, 1f);
                fillActiveColor = new Color(0.75f, 0.95f, 1f, 1f);
                glowReadyColor = new Color(0.4f, 0.85f, 1f, 0.4f);
                barBottomOffset = 188f;
                break;
            case AbilityBarType.Volley:
                fillCooldownColor = new Color(0.85f, 0.35f, 0.2f, 0.95f);
                fillReadyColor = new Color(1f, 0.55f, 0.3f, 1f);
                fillActiveColor = new Color(1f, 0.85f, 0.55f, 1f);
                glowReadyColor = new Color(1f, 0.45f, 0.2f, 0.4f);
                barBottomOffset = 52f;
                break;
            case AbilityBarType.Trap:
                fillCooldownColor = new Color(0.75f, 0.3f, 0.15f, 0.95f);
                fillReadyColor = new Color(1f, 0.45f, 0.2f, 1f);
                fillActiveColor = new Color(1f, 0.7f, 0.35f, 1f);
                glowReadyColor = new Color(1f, 0.4f, 0.15f, 0.4f);
                barBottomOffset = 86f;
                break;
        }
    }

    void EnsureWidgetRoot()
    {
        if (widgetRoot != null) return;

        string name = abilityType + "Bar_Background";
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        root.transform.SetParent(transform, false);
        widgetRoot = root.GetComponent<RectTransform>();
        widgetRoot.anchorMin = new Vector2(0.5f, 0f);
        widgetRoot.anchorMax = new Vector2(0.5f, 0f);
        widgetRoot.pivot = new Vector2(0.5f, 0.5f);
        barBackground = root.GetComponent<Image>();
    }

    void BuildBar()
    {
        if (widgetRoot == null) return;

        for (int i = widgetRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(widgetRoot.GetChild(i).gameObject);
        }

        widgetRoot.sizeDelta = barSize;
        widgetRoot.anchoredPosition = new Vector2(0f, barBottomOffset);

        if (barBackground != null)
        {
            barBackground.sprite = GetWhiteSprite();
            barBackground.color = backgroundColor;
            barBackground.raycastTarget = false;
        }

        barTrack = CreateImage("Track", widgetRoot);
        RectTransform trackRect = barTrack.rectTransform;
        trackRect.anchorMin = Vector2.zero;
        trackRect.anchorMax = Vector2.one;
        trackRect.offsetMin = new Vector2(fillInset, fillInset);
        trackRect.offsetMax = new Vector2(-fillInset, -fillInset);
        barTrack.sprite = GetWhiteSprite();
        barTrack.color = trackColor;
        barTrack.raycastTarget = false;

        barFill = CreateImage("Fill", widgetRoot);
        RectTransform fillRect = barFill.rectTransform;
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.offsetMin = new Vector2(fillInset, fillInset);
        fillRect.offsetMax = new Vector2(-fillInset, -fillInset);
        barFill.sprite = GetWhiteSprite();
        barFill.type = Image.Type.Filled;
        barFill.fillMethod = Image.FillMethod.Horizontal;
        barFill.fillOrigin = 0;
        barFill.fillAmount = 1f;
        barFill.raycastTarget = false;

        barGlow = CreateImage("Glow", widgetRoot);
        RectTransform glowRect = barGlow.rectTransform;
        glowRect.anchorMin = Vector2.zero;
        glowRect.anchorMax = Vector2.one;
        glowRect.offsetMin = new Vector2(-4f, -4f);
        glowRect.offsetMax = new Vector2(4f, 4f);
        barGlow.sprite = GetWhiteSprite();
        barGlow.color = new Color(glowReadyColor.r, glowReadyColor.g, glowReadyColor.b, 0f);
        barGlow.raycastTarget = false;
        barGlow.transform.SetAsFirstSibling();

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(widgetRoot, false);
        labelText = labelObject.GetComponent<TextMeshProUGUI>();
        RectTransform labelRect = labelText.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.pivot = new Vector2(0.5f, 0f);
        labelRect.anchoredPosition = new Vector2(0f, 2f);
        labelRect.sizeDelta = new Vector2(0f, 18f);
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.fontSize = 14f;
        labelText.text = GetLabel();
        labelText.raycastTarget = false;

        GameObject hintObject = new GameObject("KeyHint", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        hintObject.transform.SetParent(widgetRoot, false);
        keyHintText = hintObject.GetComponent<TextMeshProUGUI>();
        RectTransform hintRect = keyHintText.rectTransform;
        hintRect.anchorMin = new Vector2(1f, 0.5f);
        hintRect.anchorMax = new Vector2(1f, 0.5f);
        hintRect.pivot = new Vector2(0f, 0.5f);
        hintRect.anchoredPosition = new Vector2(8f, 0f);
        hintRect.sizeDelta = new Vector2(40f, 20f);
        keyHintText.alignment = TextAlignmentOptions.Left;
        keyHintText.fontSize = 14f;
        keyHintText.text = GetKeyHint();
        keyHintText.raycastTarget = false;
    }

    string GetLabel()
    {
        switch (abilityType)
        {
            case AbilityBarType.Flash: return "FLASH";
            case AbilityBarType.Shield: return "SHIELD";
            case AbilityBarType.Invisibility: return "INVIS";
            case AbilityBarType.Decoy: return "DECOY";
            case AbilityBarType.Volley: return "VOLLEY";
            case AbilityBarType.Trap: return "TRAP";
            default: return "ABILITY";
        }
    }

    string GetKeyHint()
    {
        switch (abilityType)
        {
            case AbilityBarType.Flash: return "E";
            case AbilityBarType.Shield: return "G";
            case AbilityBarType.Invisibility: return "V";
            case AbilityBarType.Decoy: return "X";
            case AbilityBarType.Volley: return "RM";
            case AbilityBarType.Trap: return "B";
            default: return "?";
        }
    }

    void UpdateFill(bool ready, bool active)
    {
        if (barFill == null) return;
        barFill.fillAmount = displayedFill;
        barFill.color = active ? fillActiveColor : (ready ? fillReadyColor : fillCooldownColor);
    }

    void UpdateGlow(bool ready, bool active)
    {
        if (barGlow == null) return;

        if (!ready || active)
        {
            Color c = barGlow.color;
            c.a = Mathf.MoveTowards(c.a, 0f, 4f * Time.deltaTime);
            barGlow.color = c;
            return;
        }

        float pulse = 0.55f + 0.45f * (0.5f + 0.5f * Mathf.Sin(Time.time * readyPulseSpeed));
        float flashAmt = readyFlashTimer > 0f ? readyFlashTimer / readyFlashDuration : 0f;
        barGlow.color = new Color(glowReadyColor.r, glowReadyColor.g, glowReadyColor.b, glowReadyColor.a * pulse + flashAmt * 0.35f);
    }

    void UpdateLabels(bool ready, bool active)
    {
        Color target = ready && !active ? labelReadyColor : labelCooldownColor;
        if (labelText != null) labelText.color = Color.Lerp(labelText.color, target, 10f * Time.deltaTime);
        if (keyHintText != null) keyHintText.color = Color.Lerp(keyHintText.color, target, 10f * Time.deltaTime);
    }

    void ResolveActive()
    {
        foreach (PlayerRole player in PlayerManager.All)
        {
            if (abilityType == AbilityBarType.Volley || abilityType == AbilityBarType.Trap)
            {
                if (player.roleType != RoleType.Thrower) continue;
                if (abilityType == AbilityBarType.Volley)
                {
                    PlayerVolley v = player.GetComponent<PlayerVolley>();
                    if (v != null && v.enabled)
                    {
                        volley = v;
                        return;
                    }
                }
                else
                {
                    PlayerTrap t = player.GetComponent<PlayerTrap>();
                    if (t != null && t.enabled)
                    {
                        trap = t;
                        return;
                    }
                }

                continue;
            }

            if (player.roleType != RoleType.Runner && player.roleType != RoleType.Saver) continue;

            switch (abilityType)
            {
                case AbilityBarType.Flash:
                {
                    PlayerFlash f = player.GetComponent<PlayerFlash>();
                    if (f != null && f.enabled) { flash = f; return; }
                    break;
                }
                case AbilityBarType.Shield:
                {
                    PlayerShield s = player.GetComponent<PlayerShield>();
                    if (s != null && s.enabled) { shield = s; return; }
                    break;
                }
                case AbilityBarType.Invisibility:
                {
                    PlayerInvisibility i = player.GetComponent<PlayerInvisibility>();
                    if (i != null && i.enabled) { invis = i; return; }
                    break;
                }
                case AbilityBarType.Decoy:
                {
                    PlayerDecoy d = player.GetComponent<PlayerDecoy>();
                    if (d != null && d.enabled) { decoy = d; return; }
                    break;
                }
            }
        }
    }

    void SetVisible(bool visible)
    {
        if (widgetRoot != null) widgetRoot.gameObject.SetActive(visible);
        if (labelText != null) labelText.gameObject.SetActive(visible);
        if (keyHintText != null) keyHintText.gameObject.SetActive(visible);
    }

    static Image CreateImage(string name, Transform parent)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        return imageObject.GetComponent<Image>();
    }

    static Sprite GetWhiteSprite()
    {
        if (whiteSprite != null) return whiteSprite;
        Texture2D tex = Texture2D.whiteTexture;
        whiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        return whiteSprite;
    }
}
