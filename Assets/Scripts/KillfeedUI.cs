using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Sag ust killfeed: kim kimi eledi / revive / decoy flash.
// Runtime'da kendi Canvas'ini kurar; sahne referansi gerekmez.
public class KillfeedUI : MonoBehaviour
{
    public static KillfeedUI Instance { get; private set; }

    [Header("Layout")]
    public int maxEntries = 6;
    public float entryLifetime = 4.2f;
    public float fadeOutDuration = 0.55f;
    public Vector2 panelOffset = new Vector2(-28f, -88f);
    public Vector2 entrySize = new Vector2(420f, 34f);
    public float entrySpacing = 4f;

    Canvas canvas;
    RectTransform listRoot;
    readonly List<Entry> entries = new List<Entry>();
    static Sprite whiteSprite;
    bool visible = true;

    struct Entry
    {
        public GameObject root;
        public CanvasGroup group;
        public float dieAt;
        public float bornAt;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        EnsureExists();
    }

    public static void EnsureExists()
    {
        if (Instance != null) return;

        GameObject host = GameObject.Find("KillfeedUI");
        if (host == null)
        {
            host = new GameObject("KillfeedUI");
            DontDestroyOnLoad(host);
        }

        if (host.GetComponent<KillfeedUI>() == null)
        {
            host.AddComponent<KillfeedUI>();
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildUi();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        bool shouldShow = GameManager.Instance != null &&
                          !GameManager.Instance.IsInLobby();

        if (shouldShow != visible)
        {
            visible = shouldShow;
            if (canvas != null) canvas.enabled = visible;
            if (!visible) Clear();
        }

        if (!visible) return;

        float now = Time.unscaledTime;
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            Entry e = entries[i];
            if (e.root == null)
            {
                entries.RemoveAt(i);
                continue;
            }

            float remaining = e.dieAt - now;
            if (remaining <= 0f)
            {
                Destroy(e.root);
                entries.RemoveAt(i);
                continue;
            }

            if (remaining < fadeOutDuration && e.group != null)
            {
                e.group.alpha = Mathf.Clamp01(remaining / fadeOutDuration);
            }

            // Yeni satir: hafif slide-in
            float age = now - e.bornAt;
            if (age < 0.18f && e.root.transform is RectTransform rt)
            {
                float t = age / 0.18f;
                float x = Mathf.Lerp(28f, 0f, t * t);
                rt.anchoredPosition = new Vector2(x, rt.anchoredPosition.y);
            }
        }
    }

    public static void PushKill(GameObject killer, GameObject victim)
    {
        PushKill(killer, victim, null);
    }

    public static void PushKill(GameObject killer, GameObject victim, string ballLabel)
    {
        EnsureExists();
        if (Instance == null) return;

        string a = FormatName(killer);
        string b = FormatName(victim);
        string mid = string.IsNullOrEmpty(ballLabel)
            ? ColorTag("  ●  ", UIColorPalette.Danger)
            : ColorTag("  [" + ballLabel + "]  ", UIColorPalette.Warning);

        Instance.AddLine(
            ColorTag(a, UIColorPalette.Accent) + mid + ColorTag(b, UIColorPalette.Title));
    }

    public static void PushDecoyFlash(GameObject killer, GameObject decoyOwner)
    {
        EnsureExists();
        if (Instance == null) return;

        string a = FormatName(killer);
        string b = FormatName(decoyOwner);
        Instance.AddLine(
            ColorTag(a, UIColorPalette.Accent) +
            ColorTag("  flash  ", UIColorPalette.Warning) +
            ColorTag(b + " (decoy)", UIColorPalette.Muted));
    }

    public static void PushRevive(GameObject saver, GameObject revived)
    {
        EnsureExists();
        if (Instance == null) return;

        string a = FormatName(saver);
        string b = FormatName(revived);
        Instance.AddLine(
            ColorTag(a, UIColorPalette.Success) +
            ColorTag("  ↑  ", UIColorPalette.Success) +
            ColorTag(b, UIColorPalette.Title));
    }

    public static void ClearAll()
    {
        if (Instance != null) Instance.Clear();
    }

    void Clear()
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].root != null) Destroy(entries[i].root);
        }

        entries.Clear();
    }

    void AddLine(string richText)
    {
        if (listRoot == null) BuildUi();

        while (entries.Count >= maxEntries)
        {
            if (entries[0].root != null) Destroy(entries[0].root);
            entries.RemoveAt(0);
        }

        GameObject row = new GameObject("KillfeedEntry", typeof(RectTransform));
        row.transform.SetParent(listRoot, false);

        RectTransform rt = row.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = entrySize;
        rt.anchoredPosition = new Vector2(28f, 0f);

        Image bg = row.AddComponent<Image>();
        bg.sprite = GetWhiteSprite();
        bg.color = new Color(0.04f, 0.06f, 0.1f, 0.72f);
        bg.raycastTarget = false;

        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(12, 14, 4, 4);
        layout.childAlignment = TextAnchor.MiddleRight;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        GameObject textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(row.transform, false);
        TMP_Text tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = richText;
        tmp.fontSize = 18f;
        tmp.alignment = TextAlignmentOptions.MidlineRight;
        tmp.color = UIColorPalette.Body;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        ApplyOutline(tmp);

        CanvasGroup group = row.AddComponent<CanvasGroup>();
        group.alpha = 1f;
        group.blocksRaycasts = false;
        group.interactable = false;

        entries.Add(new Entry
        {
            root = row,
            group = group,
            bornAt = Time.unscaledTime,
            dieAt = Time.unscaledTime + entryLifetime
        });

        Relayout();
    }

    void Relayout()
    {
        float y = 0f;
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            if (entries[i].root == null) continue;
            RectTransform rt = entries[i].root.transform as RectTransform;
            if (rt == null) continue;

            float x = rt.anchoredPosition.x;
            // Slide bitmisse x=0 tut
            if (Time.unscaledTime - entries[i].bornAt >= 0.18f) x = 0f;
            rt.anchoredPosition = new Vector2(x, y);
            y -= entrySize.y + entrySpacing;
        }
    }

    void BuildUi()
    {
        if (canvas != null) return;

        GameObject canvasGo = new GameObject("KillfeedCanvas", typeof(RectTransform));
        canvasGo.transform.SetParent(transform, false);

        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        GameObject listGo = new GameObject("List", typeof(RectTransform));
        listGo.transform.SetParent(canvasGo.transform, false);
        listRoot = listGo.GetComponent<RectTransform>();
        listRoot.anchorMin = new Vector2(1f, 1f);
        listRoot.anchorMax = new Vector2(1f, 1f);
        listRoot.pivot = new Vector2(1f, 1f);
        listRoot.anchoredPosition = panelOffset;
        listRoot.sizeDelta = new Vector2(entrySize.x, maxEntries * (entrySize.y + entrySpacing));
    }

    public static string FormatName(GameObject go)
    {
        if (go == null) return "?";

        PlayerRole role = go.GetComponent<PlayerRole>();
        PlayerInputHandler input = go.GetComponent<PlayerInputHandler>();
        bool human = input != null && input.enabled;

        string label;
        if (role != null)
        {
            switch (role.roleType)
            {
                case RoleType.Thrower:
                    label = human ? "ATICI" : "ATICI BOT";
                    break;
                case RoleType.Runner:
                    label = human ? "KAÇAN" : "KAÇAN BOT";
                    break;
                case RoleType.Saver:
                    label = human ? "SAVER" : "SAVER BOT";
                    break;
                default:
                    label = CleanObjectName(go.name);
                    break;
            }
        }
        else
        {
            label = CleanObjectName(go.name);
        }

        // Ayni rolden birden fazla varsa ayirt et.
        int index = RoleIndex(go, role);
        if (index > 1)
        {
            label += " " + index;
        }

        return label;
    }

    static int RoleIndex(GameObject go, PlayerRole role)
    {
        if (role == null) return 1;

        int count = 0;
        foreach (PlayerRole p in PlayerManager.All)
        {
            if (p == null || p.roleType != role.roleType) continue;
            count++;
            if (p.gameObject == go) return count;
        }

        return 1;
    }

    static string CleanObjectName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "Oyuncu";
        return name.Replace("(Clone)", "").Replace("PF_", "").Trim();
    }

    static string ColorTag(string text, Color c)
    {
        return "<color=#" + ColorUtility.ToHtmlStringRGBA(c) + ">" + text + "</color>";
    }

    static void ApplyOutline(TMP_Text tmp)
    {
        if (tmp == null) return;
        tmp.outlineWidth = 0.22f;
        tmp.outlineColor = UIColorPalette.Outline;
    }

    static Sprite GetWhiteSprite()
    {
        if (whiteSprite != null) return whiteSprite;

        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
        return whiteSprite;
    }
}
