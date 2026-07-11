using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Hold T / Select to open a radial emote wheel. Aim with mouse (or left stick),
/// release to confirm. Esc / B cancels.
/// </summary>
public class EmoteWheelUI : MonoBehaviour
{
    public static EmoteWheelUI Instance { get; private set; }
    public static bool IsOpen { get; private set; }

    [Header("Layout")]
    public float wheelRadius = 118f;
    public float sliceSize = 72f;
    public float deadZone = 28f;
    public float openHoldSeconds = 0.12f;

    [Header("Colors")]
    public Color dimColor = new Color(0.08f, 0.1f, 0.14f, 0.82f);
    public Color sliceColor = new Color(0.14f, 0.18f, 0.24f, 0.92f);
    public Color highlightColor = new Color(0.22f, 0.72f, 0.95f, 0.95f);
    public Color centerColor = new Color(0.06f, 0.08f, 0.1f, 0.9f);

    RectTransform root;
    Image centerImage;
    TextMeshProUGUI centerLabel;
    TextMeshProUGUI hintLabel;
    Image[] sliceImages;
    TextMeshProUGUI[] sliceLabels;

    PlayerEmote boundEmote;
    string[] labels = System.Array.Empty<string>();
    int highlighted = -1;
    bool built;
    float holdTimer;
    bool holdArmed;

    static Sprite whiteSprite;

    void Awake()
    {
        Instance = this;
        EnsureBuilt();
        SetVisible(false);
        IsOpen = false;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (IsOpen)
        {
            IsOpen = false;
            RefreshCursor();
        }
    }

    void Update()
    {
        EnsureBuilt();
        if (!built || root == null)
        {
            return;
        }

        bool held = ReadWheelHeld();
        bool released = ReadWheelReleased();
        bool cancel = ReadCancel();

        if (!IsOpen)
        {
            if (held)
            {
                if (!holdArmed)
                {
                    holdArmed = true;
                    holdTimer = 0f;
                }

                holdTimer += Time.unscaledDeltaTime;
                if (holdTimer >= openHoldSeconds)
                {
                    TryOpen();
                }
            }
            else
            {
                holdArmed = false;
                holdTimer = 0f;
            }

            return;
        }

        if (cancel)
        {
            CloseWheel(confirm: false);
            return;
        }

        if (released || !held)
        {
            CloseWheel(confirm: true);
            return;
        }

        UpdateHighlight();
        ApplyHighlightVisuals();
    }

    bool ReadWheelHeld()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.tKey.isPressed)
        {
            return true;
        }

        Gamepad pad = Gamepad.current;
        if (pad != null && pad.selectButton.isPressed)
        {
            return true;
        }

        // Fallback: controlled player's input handler (covers remaps / multi-device).
        foreach (PlayerRole player in PlayerManager.All)
        {
            if (player == null) continue;
            PlayerInputHandler input = player.GetComponent<PlayerInputHandler>();
            if (input != null && input.enabled && input.EmoteWheelHeld)
            {
                return true;
            }
        }

        return false;
    }

    bool ReadWheelReleased()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.tKey.wasReleasedThisFrame)
        {
            return true;
        }

        Gamepad pad = Gamepad.current;
        if (pad != null && pad.selectButton.wasReleasedThisFrame)
        {
            return true;
        }

        foreach (PlayerRole player in PlayerManager.All)
        {
            if (player == null) continue;
            PlayerInputHandler input = player.GetComponent<PlayerInputHandler>();
            if (input != null && input.enabled && input.EmoteWheelReleased)
            {
                return true;
            }
        }

        return false;
    }

    static bool ReadCancel()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.qKey.wasPressedThisFrame))
        {
            return true;
        }

        Gamepad pad = Gamepad.current;
        return pad != null && pad.buttonEast.wasPressedThisFrame;
    }

    void TryOpen()
    {
        // Lobby / round arasi: tekerlek yine acilsin (sadece emote).
        if (MatchLobbyManager.Instance != null && MatchLobbyManager.Instance.IsLobbyVisible)
        {
            return;
        }

        boundEmote = FindControlledEmote();
        if (boundEmote == null || !boundEmote.isActiveAndEnabled)
        {
            Debug.LogWarning("EmoteWheel: kontrollu PlayerEmote bulunamadi.");
            return;
        }

        labels = boundEmote.GetWheelLabels();
        if (labels == null || labels.Length == 0)
        {
            Debug.LogWarning("EmoteWheel: emote listesi bos.");
            return;
        }

        EnsureSliceCount(labels.Length);
        for (int i = 0; i < labels.Length; i++)
        {
            if (sliceLabels[i] != null)
            {
                sliceLabels[i].text = labels[i];
            }
        }

        highlighted = -1;
        if (centerLabel != null)
        {
            centerLabel.text = "Emote";
        }

        IsOpen = true;
        SetVisible(true);
        ApplyHighlightVisuals();
        RefreshCursor();
    }

    void CloseWheel(bool confirm)
    {
        int pick = highlighted;
        IsOpen = false;
        SetVisible(false);
        holdArmed = false;
        holdTimer = 0f;
        RefreshCursor();

        if (confirm && pick >= 0 && boundEmote != null && boundEmote.isActiveAndEnabled)
        {
            boundEmote.PlayEmote(pick);
        }

        boundEmote = null;
        highlighted = -1;
    }

    void UpdateHighlight()
    {
        Vector2 local;
        if (!TryGetPointerLocal(out local))
        {
            highlighted = -1;
            if (centerLabel != null)
            {
                centerLabel.text = "Emote";
            }
            return;
        }

        float dist = local.magnitude;
        if (dist < deadZone)
        {
            highlighted = -1;
            if (centerLabel != null)
            {
                centerLabel.text = "Iptal";
            }
            return;
        }

        float angle = Mathf.Atan2(local.y, local.x) * Mathf.Rad2Deg;
        if (angle < 0f)
        {
            angle += 360f;
        }

        // 0° = right, slices start at top (-90° offset) going clockwise-friendly.
        float adjusted = (90f - angle + 360f) % 360f;
        float sliceAngle = 360f / Mathf.Max(1, labels.Length);
        highlighted = Mathf.Clamp(Mathf.FloorToInt(adjusted / sliceAngle), 0, labels.Length - 1);

        if (centerLabel != null)
        {
            centerLabel.text = labels[highlighted];
        }
    }

    bool TryGetPointerLocal(out Vector2 local)
    {
        local = Vector2.zero;

        Gamepad pad = Gamepad.current;
        if (pad != null)
        {
            Vector2 stick = pad.leftStick.ReadValue();
            if (stick.sqrMagnitude > 0.2f * 0.2f)
            {
                local = stick.normalized * (wheelRadius * 0.85f);
                return true;
            }
        }

        Mouse mouse = Mouse.current;
        if (mouse == null || root == null)
        {
            return false;
        }

        Camera cam = null;
        Canvas canvas = root.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            cam = canvas.worldCamera;
        }

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            root, mouse.position.ReadValue(), cam, out local);
    }

    void ApplyHighlightVisuals()
    {
        if (sliceImages == null)
        {
            return;
        }

        for (int i = 0; i < sliceImages.Length; i++)
        {
            if (sliceImages[i] == null)
            {
                continue;
            }

            bool on = i == highlighted;
            sliceImages[i].color = on ? highlightColor : sliceColor;
            sliceImages[i].rectTransform.localScale = on ? Vector3.one * 1.08f : Vector3.one;
        }
    }

    void EnsureBuilt()
    {
        if (built && root != null)
        {
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = GetComponent<Canvas>();
        }

        if (canvas == null)
        {
            // Host is DontDestroyOnLoad (not under gameplay Canvas) — own overlay.
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;

            if (GetComponent<UnityEngine.UI.CanvasScaler>() == null)
            {
                var scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
            }

            if (GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }
        }

        Transform parent = transform;

        if (root == null)
        {
            Transform existing = parent.Find("EmoteWheelRoot");
            if (existing != null)
            {
                root = existing as RectTransform;
            }
        }

        if (root == null)
        {
            GameObject rootGo = new GameObject("EmoteWheelRoot", typeof(RectTransform), typeof(Image));
            rootGo.transform.SetParent(parent, false);
            root = rootGo.GetComponent<RectTransform>();
        }

        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.sizeDelta = new Vector2(320f, 320f);
        root.anchoredPosition = Vector2.zero;

        Image dim = root.GetComponent<Image>();
        if (dim != null)
        {
            dim.sprite = GetWhiteSprite();
            dim.color = dimColor;
            dim.raycastTarget = true;
        }

        if (centerImage == null)
        {
            Transform centerT = root.Find("Center");
            if (centerT == null)
            {
                GameObject centerGo = new GameObject("Center", typeof(RectTransform), typeof(Image));
                centerGo.transform.SetParent(root, false);
                centerT = centerGo.transform;
            }

            centerImage = centerT.GetComponent<Image>();
            RectTransform centerRt = centerT as RectTransform;
            centerRt.anchorMin = centerRt.anchorMax = centerRt.pivot = new Vector2(0.5f, 0.5f);
            centerRt.sizeDelta = new Vector2(88f, 88f);
            centerRt.anchoredPosition = Vector2.zero;
            centerImage.sprite = GetWhiteSprite();
            centerImage.color = centerColor;
            centerImage.raycastTarget = false;
        }

        if (centerLabel == null)
        {
            Transform labelT = centerImage.transform.Find("Label");
            if (labelT == null)
            {
                GameObject labelGo = new GameObject("Label", typeof(RectTransform));
                labelGo.transform.SetParent(centerImage.transform, false);
                labelT = labelGo.transform;
                centerLabel = labelGo.AddComponent<TextMeshProUGUI>();
            }
            else
            {
                centerLabel = labelT.GetComponent<TextMeshProUGUI>();
            }

            RectTransform labelRt = labelT as RectTransform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            centerLabel.text = "Emote";
            centerLabel.fontSize = 18f;
            centerLabel.alignment = TextAlignmentOptions.Center;
            centerLabel.color = Color.white;
            centerLabel.raycastTarget = false;
            SafeOutline(centerLabel, 0.2f);
        }

        if (hintLabel == null)
        {
            Transform hintT = root.Find("Hint");
            if (hintT == null)
            {
                GameObject hintGo = new GameObject("Hint", typeof(RectTransform));
                hintGo.transform.SetParent(root, false);
                hintT = hintGo.transform;
                hintLabel = hintGo.AddComponent<TextMeshProUGUI>();
            }
            else
            {
                hintLabel = hintT.GetComponent<TextMeshProUGUI>();
            }

            RectTransform hintRt = hintT as RectTransform;
            hintRt.anchorMin = new Vector2(0.5f, 0f);
            hintRt.anchorMax = new Vector2(0.5f, 0f);
            hintRt.pivot = new Vector2(0.5f, 0f);
            hintRt.sizeDelta = new Vector2(280f, 28f);
            hintRt.anchoredPosition = new Vector2(0f, -8f);
            hintLabel.text = "Surukle · birak = sec  ·  Esc iptal";
            hintLabel.fontSize = 13f;
            hintLabel.alignment = TextAlignmentOptions.Center;
            hintLabel.color = UIColorPalette.Muted;
            hintLabel.raycastTarget = false;
            SafeOutline(hintLabel, 0.15f);
        }

        built = true;
    }

    void EnsureSliceCount(int count)
    {
        if (sliceImages != null && sliceImages.Length == count)
        {
            return;
        }

        // Clear old slices.
        if (root != null)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                if (child.name.StartsWith("Slice_"))
                {
                    Destroy(child.gameObject);
                }
            }
        }

        sliceImages = new Image[count];
        sliceLabels = new TextMeshProUGUI[count];

        float angleStep = 360f / count;
        for (int i = 0; i < count; i++)
        {
            float angle = -90f + i * angleStep + angleStep * 0.5f;
            float rad = angle * Mathf.Deg2Rad;
            Vector2 pos = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * wheelRadius;

            GameObject sliceGo = new GameObject($"Slice_{i}", typeof(RectTransform), typeof(Image));
            sliceGo.transform.SetParent(root, false);
            RectTransform sliceRt = sliceGo.GetComponent<RectTransform>();
            sliceRt.anchorMin = sliceRt.anchorMax = sliceRt.pivot = new Vector2(0.5f, 0.5f);
            sliceRt.sizeDelta = new Vector2(sliceSize, sliceSize);
            sliceRt.anchoredPosition = pos;

            Image img = sliceGo.GetComponent<Image>();
            img.sprite = GetWhiteSprite();
            img.color = sliceColor;
            img.raycastTarget = false;
            sliceImages[i] = img;

            GameObject textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(sliceGo.transform, false);
            RectTransform textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(4f, 4f);
            textRt.offsetMax = new Vector2(-4f, -4f);

            TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 14f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.raycastTarget = false;
            SafeOutline(tmp, 0.2f);
            sliceLabels[i] = tmp;
        }

        // Keep center / hint above slices.
        if (centerImage != null)
        {
            centerImage.transform.SetAsLastSibling();
        }

        if (hintLabel != null)
        {
            hintLabel.transform.SetAsLastSibling();
        }
    }

    static void SafeOutline(TextMeshProUGUI tmp, float width)
    {
        if (tmp == null)
        {
            return;
        }

        try
        {
            tmp.outlineWidth = width;
            tmp.outlineColor = UIColorPalette.Outline;
        }
        catch (System.Exception)
        {
            // Some TMP material setups throw on outlineWidth; skip safely.
        }
    }

    void SetVisible(bool visible)
    {
        if (root != null)
        {
            root.gameObject.SetActive(visible);
        }
    }

    static void RefreshCursor()
    {
        CameraFollow[] cams = FindObjectsByType<CameraFollow>(FindObjectsSortMode.None);
        foreach (CameraFollow cam in cams)
        {
            cam.RefreshCursorState();
        }
    }

    static PlayerEmote FindControlledEmote()
    {
        // Prefer runner-team camera target (actual controlled character).
        if (SplitScreenManager.Instance != null &&
            SplitScreenManager.Instance.runnerTeamFollow != null &&
            SplitScreenManager.Instance.runnerTeamFollow.target != null)
        {
            PlayerEmote onTarget =
                SplitScreenManager.Instance.runnerTeamFollow.target.GetComponent<PlayerEmote>();
            if (onTarget != null && onTarget.isActiveAndEnabled)
            {
                return onTarget;
            }
        }

        foreach (PlayerRole player in PlayerManager.All)
        {
            if (player == null) continue;

            PlayerInputHandler input = player.GetComponent<PlayerInputHandler>();
            PlayerEmote emote = player.GetComponent<PlayerEmote>();
            if (input != null && input.enabled && emote != null && emote.isActiveAndEnabled)
            {
                return emote;
            }
        }

        foreach (PlayerRole player in PlayerManager.All)
        {
            if (player == null) continue;

            PlayerEmote emote = player.GetComponent<PlayerEmote>();
            if (emote != null && emote.isActiveAndEnabled)
            {
                return emote;
            }
        }

        return null;
    }

    static Sprite GetWhiteSprite()
    {
        if (whiteSprite != null)
        {
            return whiteSprite;
        }

        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
        tex.Apply();
        whiteSprite = Sprite.Create(tex, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 100f);
        return whiteSprite;
    }

    void OnDisable()
    {
        IsOpen = false;
        SetVisible(false);
    }
}
