using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Atıcı: solda alt alta top / yetenek listesi.
// Cift secim: 1. secim sari cerceve, 2. secim yesil cerceve (kombo).
// Yetenek satirlari ThrowerAbilityRegistry'den gelir (tus ipucu + CD).
// 1-9 / 0 / scroll / D-Pad / tikla.
public class BallSelectUI : MonoBehaviour
{
    public static BallSelectUI Instance { get; private set; }

    const float PanelWidth = 200f;
    const float RowHeight = 38f;
    const float RowGap = 5f;

    PlayerThrow trackedThrow;
    RectTransform listRoot;
    TMP_Text statusLabel;
    CanvasGroup canvasGroup;
    BallRow[] rows;
    static Sprite whiteSprite;
    bool built;
    int lastBuiltCount = -1;

    struct BallRow
    {
        public BallData data;
        public ThrowerAbilityId abilityId;
        public Image background;
        public Image cdFill;
        public Outline outline;
        public Image accent;
        public TMP_Text label;
        public TMP_Text keyHint;
        public TMP_Text activateHint;
        public Button button;
    }

    public static void EnsureExists()
    {
        if (Instance != null) return;

        BallSelectUI existing = FindFirstObjectByType<BallSelectUI>();
        if (existing != null)
        {
            Instance = existing;
            return;
        }

        GameObject host = GameObject.Find("BallSelectUI");
        if (host == null)
        {
            host = new GameObject("BallSelectUI");
            DontDestroyOnLoad(host);
        }

        Instance = host.GetComponent<BallSelectUI>();
        if (Instance == null)
        {
            Instance = host.AddComponent<BallSelectUI>();
        }
    }

    void Awake()
    {
        Instance = this;
        BuildCanvas();
        SetVisible(false);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (!ShouldShow())
        {
            SetVisible(false);
            return;
        }

        ResolveThrower();
        if (trackedThrow == null)
        {
            SetVisible(false);
            return;
        }

        EnsureRows();
        SetVisible(true);
        RefreshSelectionVisual();
        RefreshAbilityCooldown();
        RefreshStatusLine();
        ReadSelectInput();
    }

    bool ShouldShow()
    {
        if (!GameManager.RoundIsActive) return false;
        if (GameManager.Instance != null && GameManager.Instance.IsInLobby()) return false;
        if (EmoteWheelUI.IsOpen) return false;
        return true;
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
            if (throwScript == null || !throwScript.enabled) continue;

            ThrowerBot bot = player.GetComponent<ThrowerBot>();
            if (bot != null && bot.enabled) continue;

            trackedThrow = throwScript;
            return;
        }
    }

    void ReadSelectInput()
    {
        if (trackedThrow == null) return;

        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (scroll > 0.1f) trackedThrow.CycleBall(-1);
            else if (scroll < -0.1f) trackedThrow.CycleBall(1);
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            TryDigitSelect(keyboard);
        }

        if (trackedThrow != null)
        {
            PlayerInputHandler input = trackedThrow.GetComponent<PlayerInputHandler>();
            if (input != null && input.scheme == ControlScheme.Gamepad &&
                input.gamepadIndex >= 0 && input.gamepadIndex < Gamepad.all.Count)
            {
                Gamepad pad = Gamepad.all[input.gamepadIndex];
                if (pad.dpad.up.wasPressedThisFrame) trackedThrow.CycleBall(-1);
                else if (pad.dpad.down.wasPressedThisFrame) trackedThrow.CycleBall(1);
            }
        }
    }

    void TryDigitSelect(Keyboard keyboard)
    {
        int digit = -1;
        if (keyboard.digit1Key.wasPressedThisFrame) digit = 1;
        else if (keyboard.digit2Key.wasPressedThisFrame) digit = 2;
        else if (keyboard.digit3Key.wasPressedThisFrame) digit = 3;
        else if (keyboard.digit4Key.wasPressedThisFrame) digit = 4;
        else if (keyboard.digit5Key.wasPressedThisFrame) digit = 5;
        else if (keyboard.digit6Key.wasPressedThisFrame) digit = 6;
        else if (keyboard.digit7Key.wasPressedThisFrame) digit = 7;
        else if (keyboard.digit8Key.wasPressedThisFrame) digit = 8;
        else if (keyboard.digit9Key.wasPressedThisFrame) digit = 9;
        else if (keyboard.digit0Key.wasPressedThisFrame) digit = 0;

        if (digit < 0 || trackedThrow == null)
        {
            return;
        }

        int slot = digit == 0 ? 9 : digit - 1;
        if (slot >= 0 && slot < trackedThrow.SelectableSlotCount)
        {
            trackedThrow.SelectSlot(slot);
        }
    }

    void EnsureRows()
    {
        BallData[] types = trackedThrow.AvailableBallTypes;
        int ballCount = types != null ? types.Length : 0;
        int count = ballCount + ThrowerAbilityRegistry.Count;
        if (built && count == lastBuiltCount && rows != null && rows.Length == count)
        {
            return;
        }

        RebuildRows(types);
    }

    void RebuildRows(BallData[] types)
    {
        if (listRoot == null) return;

        for (int i = listRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(listRoot.GetChild(i).gameObject);
        }

        int ballCount = types != null ? types.Length : 0;
        int count = ballCount + ThrowerAbilityRegistry.Count;
        rows = new BallRow[count];
        lastBuiltCount = count;
        built = true;

        float totalHeight = count * RowHeight + Mathf.Max(0, count - 1) * RowGap;
        listRoot.sizeDelta = new Vector2(PanelWidth, totalHeight);

        for (int i = 0; i < ballCount; i++)
        {
            rows[i] = CreateBallRow(types[i], i);
        }

        for (int a = 0; a < ThrowerAbilityRegistry.Count; a++)
        {
            int slot = ballCount + a;
            rows[slot] = CreateAbilityRow(ThrowerAbilityRegistry.GetByIndex(a), slot);
        }

        LayoutStatusLabel(totalHeight);
    }

    void LayoutStatusLabel(float listHeight)
    {
        if (statusLabel == null || listRoot == null) return;

        // Liste sol-orta (y=28); durum satiri listenin hemen altinda.
        float listBottom = listRoot.anchoredPosition.y - listHeight * 0.5f;
        RectTransform rt = statusLabel.rectTransform;
        rt.anchoredPosition = new Vector2(listRoot.anchoredPosition.x, listBottom - 10f);
    }

    BallRow CreateBallRow(BallData data, int index)
    {
        BallRow row = CreateRow(
            data,
            ThrowerAbilityId.None,
            index,
            "Ball_" + index,
            data != null ? ShortName(data.ballName) : "?",
            ColorForBall(data),
            activateHint: null);
        return row;
    }

    BallRow CreateAbilityRow(ThrowerAbilityInfo info, int index)
    {
        return CreateRow(
            null,
            info.Id,
            index,
            info.RowName,
            info.DisplayName,
            info.Accent,
            info.ActivateHint);
    }

    BallRow CreateRow(
        BallData data,
        ThrowerAbilityId abilityId,
        int index,
        string rowName,
        string labelText,
        Color accentColor,
        string activateHint)
    {
        BallRow row = new BallRow
        {
            data = data,
            abilityId = abilityId
        };

        GameObject go = new GameObject(
            string.IsNullOrEmpty(rowName) ? ("Slot_" + index) : rowName,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(listRoot, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, RowHeight);
        rt.anchoredPosition = new Vector2(0f, -(index * (RowHeight + RowGap)));

        row.background = go.GetComponent<Image>();
        row.background.sprite = GetWhiteSprite();
        row.background.color = UIColorPalette.CardSoft;
        row.background.raycastTarget = true;

        // CD dolgu (soldan saga, altta ince serit).
        GameObject cdGo = new GameObject("CdFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        cdGo.transform.SetParent(go.transform, false);
        row.cdFill = cdGo.GetComponent<Image>();
        row.cdFill.sprite = GetWhiteSprite();
        row.cdFill.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.35f);
        row.cdFill.raycastTarget = false;
        row.cdFill.type = Image.Type.Filled;
        row.cdFill.fillMethod = Image.FillMethod.Horizontal;
        row.cdFill.fillOrigin = 0;
        row.cdFill.fillAmount = abilityId == ThrowerAbilityId.None ? 0f : 1f;
        RectTransform cdRt = row.cdFill.rectTransform;
        cdRt.anchorMin = Vector2.zero;
        cdRt.anchorMax = Vector2.one;
        cdRt.offsetMin = Vector2.zero;
        cdRt.offsetMax = Vector2.zero;

        row.button = go.GetComponent<Button>();
        ColorBlock colors = row.button.colors;
        colors.highlightedColor = new Color(0.2f, 0.35f, 0.45f, 1f);
        colors.pressedColor = new Color(0.15f, 0.5f, 0.55f, 1f);
        colors.selectedColor = colors.highlightedColor;
        row.button.colors = colors;

        int captured = index;
        row.button.onClick.AddListener(() =>
        {
            if (trackedThrow != null)
            {
                trackedThrow.SelectSlot(captured);
            }
        });

        row.outline = go.AddComponent<Outline>();
        row.outline.effectColor = UIColorPalette.ComboPrimary;
        row.outline.effectDistance = new Vector2(3.5f, 3.5f);
        row.outline.useGraphicAlpha = true;
        row.outline.enabled = false;

        GameObject accentGo = new GameObject("Accent", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        accentGo.transform.SetParent(go.transform, false);
        row.accent = accentGo.GetComponent<Image>();
        row.accent.sprite = GetWhiteSprite();
        row.accent.raycastTarget = false;
        RectTransform accentRt = row.accent.rectTransform;
        accentRt.anchorMin = new Vector2(0f, 0f);
        accentRt.anchorMax = new Vector2(0f, 1f);
        accentRt.pivot = new Vector2(0f, 0.5f);
        accentRt.sizeDelta = new Vector2(5f, 0f);
        accentRt.anchoredPosition = Vector2.zero;
        row.accent.color = accentColor;

        string keyLabel = (index + 1) >= 10 ? "0" : (index + 1).ToString();
        row.keyHint = CreateLabel("Key", go.transform, keyLabel, 14, TextAlignmentOptions.Center);
        RectTransform keyRt = row.keyHint.rectTransform;
        keyRt.anchorMin = new Vector2(0f, 0f);
        keyRt.anchorMax = new Vector2(0f, 1f);
        keyRt.pivot = new Vector2(0f, 0.5f);
        keyRt.sizeDelta = new Vector2(26f, 0f);
        keyRt.anchoredPosition = new Vector2(10f, 0f);
        row.keyHint.color = UIColorPalette.Muted;

        row.label = CreateLabel("Name", go.transform, labelText, 14, TextAlignmentOptions.Left);
        RectTransform nameRt = row.label.rectTransform;
        nameRt.anchorMin = new Vector2(0f, 0f);
        nameRt.anchorMax = new Vector2(1f, 1f);
        nameRt.offsetMin = new Vector2(36f, 2f);
        nameRt.offsetMax = new Vector2(string.IsNullOrEmpty(activateHint) ? -8f : -36f, -2f);

        if (!string.IsNullOrEmpty(activateHint))
        {
            row.activateHint = CreateLabel("Act", go.transform, activateHint, 12, TextAlignmentOptions.Center);
            RectTransform actRt = row.activateHint.rectTransform;
            actRt.anchorMin = new Vector2(1f, 0f);
            actRt.anchorMax = new Vector2(1f, 1f);
            actRt.pivot = new Vector2(1f, 0.5f);
            actRt.sizeDelta = new Vector2(32f, 0f);
            actRt.anchoredPosition = new Vector2(-6f, 0f);
            row.activateHint.color = UIColorPalette.Muted;
        }

        return row;
    }

    void RefreshSelectionVisual()
    {
        if (rows == null || trackedThrow == null) return;

        int primary = trackedThrow.PrimarySlotIndex;
        int combo = trackedThrow.ComboSlotIndex;
        float pulse = 0.55f + 0.45f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5.5f));

        for (int i = 0; i < rows.Length; i++)
        {
            bool isPrimary = i == primary;
            bool isCombo = i == combo;
            bool on = isPrimary || isCombo;

            if (rows[i].background != null)
            {
                if (isPrimary)
                {
                    rows[i].background.color = new Color(0.28f, 0.22f, 0.06f, 0.95f);
                }
                else if (isCombo)
                {
                    rows[i].background.color = new Color(0.06f, 0.24f, 0.12f, 0.95f);
                }
                else
                {
                    rows[i].background.color = UIColorPalette.CardSoft;
                }
            }

            if (rows[i].outline != null)
            {
                if (on)
                {
                    Color baseColor = isPrimary ? UIColorPalette.ComboPrimary : UIColorPalette.ComboSecondary;
                    baseColor.a = pulse;
                    rows[i].outline.enabled = true;
                    rows[i].outline.effectColor = baseColor;
                    rows[i].outline.effectDistance = new Vector2(3.5f + pulse, 3.5f + pulse);
                }
                else
                {
                    rows[i].outline.enabled = false;
                }
            }

            if (rows[i].label != null)
            {
                if (isPrimary)
                {
                    rows[i].label.color = UIColorPalette.ComboPrimary;
                    rows[i].label.fontStyle = FontStyles.Bold;
                }
                else if (isCombo)
                {
                    rows[i].label.color = UIColorPalette.ComboSecondary;
                    rows[i].label.fontStyle = FontStyles.Bold;
                }
                else
                {
                    rows[i].label.color = UIColorPalette.Body;
                    rows[i].label.fontStyle = FontStyles.Normal;
                }
            }

            if (rows[i].keyHint != null)
            {
                if (isPrimary) rows[i].keyHint.color = UIColorPalette.ComboPrimary;
                else if (isCombo) rows[i].keyHint.color = UIColorPalette.ComboSecondary;
                else rows[i].keyHint.color = UIColorPalette.Muted;
            }

            if (rows[i].activateHint != null)
            {
                bool showAct = on;
                rows[i].activateHint.gameObject.SetActive(true);
                if (showAct)
                {
                    rows[i].activateHint.color = isPrimary
                        ? UIColorPalette.ComboPrimary
                        : UIColorPalette.ComboSecondary;
                    rows[i].activateHint.fontStyle = FontStyles.Bold;
                }
                else
                {
                    rows[i].activateHint.color = UIColorPalette.Muted;
                    rows[i].activateHint.fontStyle = FontStyles.Normal;
                }
            }
        }
    }

    void RefreshAbilityCooldown()
    {
        if (rows == null || trackedThrow == null) return;

        for (int i = 0; i < rows.Length; i++)
        {
            if (rows[i].cdFill == null) continue;

            if (rows[i].abilityId == ThrowerAbilityId.None)
            {
                rows[i].cdFill.fillAmount = 0f;
                continue;
            }

            float ready = ThrowerAbilityRegistry.GetCooldownVisual(trackedThrow, rows[i].abilityId);
            bool busy = ThrowerAbilityRegistry.IsAbilityBusy(trackedThrow, rows[i].abilityId);
            rows[i].cdFill.fillAmount = ready;

            Color accent = rows[i].accent != null ? rows[i].accent.color : UIColorPalette.Accent;
            if (busy)
            {
                float pulse = 0.45f + 0.35f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 7f));
                rows[i].cdFill.color = new Color(accent.r, accent.g, accent.b, pulse);
            }
            else
            {
                float a = ready >= 0.99f ? 0.22f : 0.4f;
                rows[i].cdFill.color = new Color(accent.r, accent.g, accent.b, a);
            }
        }
    }

    void RefreshStatusLine()
    {
        if (statusLabel == null || trackedThrow == null) return;
        statusLabel.text = ThrowerAbilityRegistry.BuildStatusLine(trackedThrow);
    }

    static string ShortName(string full)
    {
        if (string.IsNullOrEmpty(full)) return "Top";
        if (full.EndsWith(" Ball")) return full.Substring(0, full.Length - 5);
        if (full.EndsWith(" Top")) return full.Substring(0, full.Length - 4);
        return full;
    }

    static Color ColorForBall(BallData data)
    {
        if (data == null) return UIColorPalette.Muted;
        string n = data.ballName != null ? data.ballName.ToLowerInvariant() : "";
        if (n.Contains("fast")) return new Color(0.35f, 0.95f, 1f, 1f);
        if (n.Contains("heavy")) return new Color(0.95f, 0.45f, 0.35f, 1f);
        if (n.Contains("sek") || n.Contains("bounc")) return new Color(0.45f, 0.95f, 0.55f, 1f);
        if (n.Contains("egri") || n.Contains("curve")) return new Color(0.85f, 0.55f, 1f, 1f);
        if (n.Contains("ayna") || n.Contains("mirror")) return new Color(0.55f, 0.92f, 1f, 1f);
        return UIColorPalette.Accent;
    }

    void SetVisible(bool visible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.blocksRaycasts = visible;
            canvasGroup.interactable = visible;
        }

        if (listRoot != null)
        {
            listRoot.gameObject.SetActive(visible);
        }

        if (statusLabel != null)
        {
            statusLabel.gameObject.SetActive(visible);
        }
    }

    void BuildCanvas()
    {
        Canvas canvas = gameObject.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 70;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();
        }

        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
            var uiModule = es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            uiModule.enabled = true;
#else
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
        }

        canvasGroup = gameObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        GameObject listGo = new GameObject("BallList", typeof(RectTransform));
        listGo.transform.SetParent(transform, false);
        listRoot = listGo.GetComponent<RectTransform>();
        listRoot.anchorMin = new Vector2(0f, 0.5f);
        listRoot.anchorMax = new Vector2(0f, 0.5f);
        listRoot.pivot = new Vector2(0f, 0.5f);
        listRoot.anchoredPosition = new Vector2(24f, 28f);
        listRoot.sizeDelta = new Vector2(PanelWidth, 200f);

        statusLabel = CreateLabel("Status", transform, "Kombo sec", 13, TextAlignmentOptions.Left);
        RectTransform statusRt = statusLabel.rectTransform;
        statusRt.anchorMin = new Vector2(0f, 0.5f);
        statusRt.anchorMax = new Vector2(0f, 0.5f);
        statusRt.pivot = new Vector2(0f, 1f);
        statusRt.sizeDelta = new Vector2(320f, 40f);
        statusRt.anchoredPosition = new Vector2(24f, -90f);
        statusLabel.color = UIColorPalette.Muted;
        statusLabel.enableWordWrapping = true;
        statusLabel.overflowMode = TextOverflowModes.Ellipsis;
    }

    static TMP_Text CreateLabel(string name, Transform parent, string text, float size, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TMP_Text tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.color = UIColorPalette.Body;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        return tmp;
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
