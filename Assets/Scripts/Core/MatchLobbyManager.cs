using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Oyun başlamadan önce ayarları toplar, botları yapılandırır ve maçı başlatır.
[DefaultExecutionOrder(-100)]
public class MatchLobbyManager : MonoBehaviour
{
    public static MatchLobbyManager Instance { get; private set; }

    [Header("Varsayılan Ayarlar")]
    public float roundDuration = 60f;
    public int roundsToWinMatch = 3;
    public float reviveCountdownDuration = 10f;

    [Header("Bot Ayarları")]
    public bool runnerBotEnabled = true;
    [Tooltip("Toplam atıcı sayısı (1-3). İnsan kontrol seçiliyse bir tanesi oyuncuya kalır.")]
    public int throwerCount = 1;
    public bool throwerIsHuman = false;

    [Header("Referanslar")]
    public GameObject runnerBot;
    public GameObject mainThrower;
    public ArenaBuilder arenaBuilder;

    public bool IsLobbyVisible { get; private set; }

    readonly List<GameObject> spawnedThrowers = new List<GameObject>();
    MatchLobbyUI lobbyUI;

    Canvas gameplayCanvas;

    void Awake()
    {
        Instance = this;
        IsLobbyVisible = true;
        ResolveReferences();
    }

    void Start()
    {
        lobbyUI = GetComponent<MatchLobbyUI>();
        if (lobbyUI == null)
        {
            lobbyUI = gameObject.AddComponent<MatchLobbyUI>();
        }

        lobbyUI.Initialize(this);
        ShowLobby();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void ResolveReferences()
    {
        if (runnerBot == null)
        {
            RunnerBot bot = FindFirstObjectByType<RunnerBot>();
            if (bot != null)
            {
                runnerBot = bot.gameObject;
            }
        }

        if (mainThrower == null)
        {
            foreach (PlayerRole player in PlayerManager.All)
            {
                if (player.roleType == RoleType.Thrower)
                {
                    mainThrower = player.gameObject;
                    break;
                }
            }
        }

        if (arenaBuilder == null)
        {
            arenaBuilder = FindFirstObjectByType<ArenaBuilder>();
        }

        if (gameplayCanvas == null)
        {
            GameObject canvasObject = GameObject.Find("Canvas");
            if (canvasObject != null)
            {
                gameplayCanvas = canvasObject.GetComponent<Canvas>();
            }
        }
    }

    void SetGameplayCanvasVisible(bool visible)
    {
        if (gameplayCanvas != null)
        {
            gameplayCanvas.gameObject.SetActive(visible);
        }
    }

    public void ShowLobby()
    {
        IsLobbyVisible = true;
        ResetThrowerSetup();
        SetGameplayInputEnabled(false);
        SetGameplayCanvasVisible(false);
        SetLobbyCursor(true);

        if (runnerBot != null)
        {
            runnerBot.SetActive(runnerBotEnabled);
        }

        if (lobbyUI != null)
        {
            lobbyUI.SetVisible(true);
            lobbyUI.RefreshValues();
        }
    }

    void ResetThrowerSetup()
    {
        ClearSpawnedThrowers();

        if (mainThrower != null)
        {
            ThrowerHumanControl human = mainThrower.GetComponent<ThrowerHumanControl>();
            if (human != null)
            {
                human.SetLobbyOverride(false, false);
            }
        }
    }

    public void HideLobby()
    {
        IsLobbyVisible = false;
        SetGameplayCanvasVisible(true);

        if (lobbyUI != null)
        {
            lobbyUI.SetVisible(false);
        }
    }

    public void StartMatch()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsInLobby())
        {
            return;
        }

        ApplySettings();
        ConfigureRunnerBot();
        ConfigureThrowers();
        AbilityLoadout.ApplyToAllPlayers();
        SetGameplayInputEnabled(true);
        HideLobby();
        SetLobbyCursor(false);
        GameManager.Instance.BeginMatch();

        Debug.Log("Lobiden maç başlatıldı. Loadout: " + string.Join(", ", AbilityLoadout.Picks));
    }

    // Lobby'de imlec serbest ve gorunur; mac basinda kamera tekrar kilitler.
    void SetLobbyCursor(bool lobbyMode)
    {
        if (lobbyMode)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        CameraFollow[] cameras = FindObjectsByType<CameraFollow>(FindObjectsSortMode.None);

        foreach (CameraFollow cameraFollow in cameras)
        {
            cameraFollow.RefreshCursorState();
        }
    }

    void ApplySettings()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        gm.roundDuration = roundDuration;
        gm.roundsToWinMatch = roundsToWinMatch;
        gm.reviveCountdownDuration = reviveCountdownDuration;
    }

    void ConfigureRunnerBot()
    {
        if (runnerBot == null) return;

        runnerBot.SetActive(runnerBotEnabled);
    }

    void ConfigureThrowers()
    {
        ClearSpawnedThrowers();

        int totalThrowers = Mathf.Clamp(throwerCount, 1, 3);
        Vector3 baseSpawn = arenaBuilder != null ? arenaBuilder.throwerSpawn : new Vector3(0f, 1f, -13.5f);
        float[] xOffsets = GetThrowerOffsets(totalThrowers);

        if (mainThrower != null)
        {
            mainThrower.transform.position = baseSpawn + new Vector3(xOffsets[0], 0f, 0f);
            ConfigureThrowerObject(mainThrower, throwerIsHuman);
        }

        for (int i = 1; i < totalThrowers; i++)
        {
            if (mainThrower == null) break;

            GameObject clone = Instantiate(mainThrower, baseSpawn + new Vector3(xOffsets[i], 0f, 0f), mainThrower.transform.rotation);
            clone.name = "ThrowerBot_" + i;
            SceneFolders.ParentTo(clone.transform, SceneFolders.Players);

            ThrowerHumanControl humanControl = clone.GetComponent<ThrowerHumanControl>();
            if (humanControl != null)
            {
                Destroy(humanControl);
            }

            PlayerInputHandler input = clone.GetComponent<PlayerInputHandler>();
            if (input != null)
            {
                Destroy(input);
            }

            ConfigureThrowerObject(clone, false);
            spawnedThrowers.Add(clone);
        }

        if (SplitScreenManager.Instance != null)
        {
            SplitScreenManager.Instance.RefreshLayout();
        }

        RegisterThrowerStartPositions();
    }

    void RegisterThrowerStartPositions()
    {
        if (GameManager.Instance == null) return;

        foreach (PlayerRole player in PlayerManager.All)
        {
            if (player.roleType == RoleType.Thrower)
            {
                GameManager.Instance.RegisterStartPosition(player);
            }
        }
    }

    void ConfigureThrowerObject(GameObject thrower, bool humanControl)
    {
        ThrowerBot bot = thrower.GetComponent<ThrowerBot>();
        PlayerMovement movement = thrower.GetComponent<PlayerMovement>();
        ThrowerHumanControl human = thrower.GetComponent<ThrowerHumanControl>();
        PlayerInputHandler input = thrower.GetComponent<PlayerInputHandler>();

        if (human != null)
        {
            human.SetLobbyOverride(true, humanControl);
        }
        else
        {
            if (bot != null) bot.enabled = !humanControl;
            if (movement != null) movement.enabled = humanControl;
        }

        // Gamepad yoksa klavye+fare: ok tuslari hareket, mouse atis/volley.
        if (input != null && humanControl)
        {
            bool hasPad = HasGamepad();
            input.scheme = hasPad ? ControlScheme.Gamepad : ControlScheme.KeyboardMouse;
            input.useArrowKeysForMove = !hasPad;
            if (hasPad)
            {
                input.gamepadIndex = 0;
            }
        }
        else if (input != null)
        {
            input.useArrowKeysForMove = false;
        }
    }

    float[] GetThrowerOffsets(int count)
    {
        switch (count)
        {
            case 1: return new[] { 0f };
            case 2: return new[] { -5f, 5f };
            default: return new[] { -7f, 0f, 7f };
        }
    }

    void ClearSpawnedThrowers()
    {
        foreach (GameObject thrower in spawnedThrowers)
        {
            if (thrower != null)
            {
                Destroy(thrower);
            }
        }

        spawnedThrowers.Clear();
    }

    void SetGameplayInputEnabled(bool enabled)
    {
        PlayerInputHandler[] inputs = FindObjectsByType<PlayerInputHandler>(FindObjectsSortMode.None);

        foreach (PlayerInputHandler input in inputs)
        {
            input.enabled = enabled;
        }

        PlayerThrow[] throws = FindObjectsByType<PlayerThrow>(FindObjectsSortMode.None);

        foreach (PlayerThrow throwScript in throws)
        {
            throwScript.enabled = enabled;
        }
    }

    bool HasGamepad()
    {
        return UnityEngine.InputSystem.Gamepad.all.Count > 0;
    }

    public void SetRunnerBotEnabled(bool enabled)
    {
        runnerBotEnabled = enabled;
    }

    public void SetThrowerCount(int count)
    {
        throwerCount = Mathf.Clamp(count, 1, 3);
    }

    public void SetThrowerIsHuman(bool human)
    {
        throwerIsHuman = human;
    }

    public void AdjustRoundDuration(int delta)
    {
        SetRoundDuration(roundDuration + delta);
    }

    public void AdjustRoundsToWin(int delta)
    {
        SetRoundsToWin(roundsToWinMatch + delta);
    }

    public void AdjustReviveDuration(int delta)
    {
        SetReviveDuration(reviveCountdownDuration + delta);
    }

    public void SetRoundDuration(float seconds)
    {
        roundDuration = Mathf.Clamp(seconds, 15f, 300f);
    }

    public void SetRoundsToWin(int count)
    {
        roundsToWinMatch = Mathf.Clamp(count, 1, 10);
    }

    public void SetReviveDuration(float seconds)
    {
        reviveCountdownDuration = Mathf.Clamp(seconds, 3f, 30f);
    }
}

// Lobideki ayar panelini runtime'da olusturur ve MatchLobbyManager'a baglar.
public class MatchLobbyUI : MonoBehaviour
{
    struct ClickZone
    {
        public RectTransform rect;
        public Image image;
        public Color normalColor;
        public Color hoverColor;
        public System.Action action;
    }

    enum EditableSetting
    {
        RoundDuration,
        RoundsToWin,
        ReviveDuration,
        ThrowerCount
    }

    MatchLobbyManager lobby;

    GameObject panelRoot;
    Canvas lobbyCanvas;
    TMP_InputField roundDurationInput;
    TMP_InputField roundsToWinInput;
    TMP_InputField reviveDurationInput;
    TMP_InputField throwerCountInput;
    TMP_Text runnerBotStatus;
    TMP_Text throwerControlStatus;
    TMP_Text gamepadHint;
    TMP_Text loadoutHint;

    struct LoadoutChip
    {
        public LoadoutAbility ability;
        public Image background;
        public TMP_Text label;
        public Color normalColor;
        public Color selectedColor;
        public Color hoverColor;
    }

    readonly List<LoadoutChip> loadoutChips = new List<LoadoutChip>();
    readonly List<ClickZone> clickZones = new List<ClickZone>();
    readonly List<TMP_InputField> editableInputs = new List<TMP_InputField>();
    static Sprite whiteSprite;

    public void Initialize(MatchLobbyManager manager)
    {
        lobby = manager;
        EnsureEventSystem();
        BuildPanel();
        SetVisible(false);
    }

    public void SetVisible(bool visible)
    {
        if (panelRoot != null)
        {
            panelRoot.transform.root.gameObject.SetActive(visible);
        }
    }

    public void RefreshValues()
    {
        if (lobby == null) return;

        SetInputTextIfIdle(roundDurationInput, Mathf.RoundToInt(lobby.roundDuration) + " sn");
        SetInputTextIfIdle(roundsToWinInput, lobby.roundsToWinMatch.ToString());
        SetInputTextIfIdle(reviveDurationInput, Mathf.RoundToInt(lobby.reviveCountdownDuration) + " sn");
        SetInputTextIfIdle(throwerCountInput, lobby.throwerCount.ToString());

        if (runnerBotStatus != null)
        {
            runnerBotStatus.text = lobby.runnerBotEnabled ? "Acik" : "Kapali";
        }

        if (throwerControlStatus != null)
        {
            throwerControlStatus.text = lobby.throwerIsHuman ? "Insan" : "Bot";
        }

        if (gamepadHint != null)
        {
            bool hasPad = Gamepad.all.Count > 0;
            gamepadHint.text = lobby.throwerIsHuman
                ? (hasPad
                    ? "Gamepad: hareket + RT atis + LB volley + R3 trap"
                    : "Klavye: Ok tuslari | Sol tik atis | Sag tik volley | B / orta mouse trap")
                : "";
            gamepadHint.gameObject.SetActive(lobby.throwerIsHuman);
        }

        RefreshLoadoutChips();
    }

    void RefreshLoadoutChips()
    {
        for (int i = 0; i < loadoutChips.Count; i++)
        {
            LoadoutChip chip = loadoutChips[i];
            bool selected = AbilityLoadout.IsSelected(chip.ability);

            if (chip.background != null)
            {
                chip.background.color = selected ? chip.selectedColor : chip.normalColor;
            }

            if (chip.label != null)
            {
                chip.label.color = selected ? UIColorPalette.Title : UIColorPalette.Muted;
                chip.label.text = AbilityLoadout.DisplayName(chip.ability) +
                                  "\n<size=70%>" + AbilityLoadout.KeyHint(chip.ability) +
                                  " · " + AbilityLoadout.ShortDesc(chip.ability) + "</size>";
            }

            // Hover sistemi secili rengi ezmesin.
            for (int z = 0; z < clickZones.Count; z++)
            {
                if (clickZones[z].image != chip.background) continue;

                ClickZone zone = clickZones[z];
                zone.normalColor = selected ? chip.selectedColor : chip.normalColor;
                zone.hoverColor = selected
                    ? Color.Lerp(chip.selectedColor, Color.white, 0.15f)
                    : chip.hoverColor;
                clickZones[z] = zone;
                break;
            }
        }

        if (loadoutHint != null)
        {
            loadoutHint.text = "Yetenek loadout: " + AbilityLoadout.Count + "/" + AbilityLoadout.MaxPicks +
                               "  (en fazla 2 sec)";
            loadoutHint.color = AbilityLoadout.Count >= AbilityLoadout.MaxPicks
                ? UIColorPalette.Accent
                : UIColorPalette.Muted;
        }
    }

    void SetInputTextIfIdle(TMP_InputField input, string text)
    {
        if (input == null || input.isFocused)
        {
            return;
        }

        if (input.text != text)
        {
            input.SetTextWithoutNotify(text);
        }
    }

    bool IsEditingInput()
    {
        foreach (TMP_InputField input in editableInputs)
        {
            if (input != null && input.isFocused)
            {
                return true;
            }
        }

        return false;
    }

    void Update()
    {
        if (panelRoot == null || !panelRoot.activeInHierarchy)
        {
            return;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        RefreshValues();
        UpdateHoverStates();

        if (!IsEditingInput())
        {
            HandleManualClicks();
        }
    }

    void HandleManualClicks()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
        {
            return;
        }

        Vector2 screenPos = mouse.position.ReadValue();

        if (IsPointerOverInput(screenPos))
        {
            return;
        }

        for (int i = clickZones.Count - 1; i >= 0; i--)
        {
            ClickZone zone = clickZones[i];
            if (zone.rect == null)
            {
                continue;
            }

            if (RectTransformUtility.RectangleContainsScreenPoint(zone.rect, screenPos, GetEventCamera()))
            {
                zone.action?.Invoke();
                return;
            }
        }
    }

    bool IsPointerOverInput(Vector2 screenPos)
    {
        foreach (TMP_InputField input in editableInputs)
        {
            if (input == null)
            {
                continue;
            }

            RectTransform rect = input.transform as RectTransform;
            if (rect != null && RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos, GetEventCamera()))
            {
                return true;
            }
        }

        return false;
    }

    void UpdateHoverStates()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        Vector2 screenPos = mouse.position.ReadValue();

        foreach (ClickZone zone in clickZones)
        {
            if (zone.image == null || zone.rect == null)
            {
                continue;
            }

            bool hover = RectTransformUtility.RectangleContainsScreenPoint(zone.rect, screenPos, GetEventCamera());
            zone.image.color = hover ? zone.hoverColor : zone.normalColor;
        }
    }

    Camera GetEventCamera()
    {
        if (lobbyCanvas == null || lobbyCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return lobbyCanvas.worldCamera;
    }

    void EnsureEventSystem()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }

    void BuildPanel()
    {
        clickZones.Clear();
        editableInputs.Clear();
        loadoutChips.Clear();

        GameObject canvasObject = new GameObject("LobbyCanvas");
        lobbyCanvas = canvasObject.AddComponent<Canvas>();
        lobbyCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        lobbyCanvas.sortingOrder = 500;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        panelRoot = new GameObject("LobbyPanel", typeof(RectTransform));
        panelRoot.transform.SetParent(canvasObject.transform, false);

        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        StretchFull(panelRect);

        GameObject background = CreatePanel("Background", panelRoot.transform, Vector2.zero);
        StretchFull(background.GetComponent<RectTransform>());
        Image backgroundImage = background.GetComponent<Image>();
        backgroundImage.color = UIColorPalette.Backdrop;
        backgroundImage.raycastTarget = false;

        GameObject card = CreatePanel("LobbyCard", panelRoot.transform, new Vector2(600f, 780f));
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;

        Image cardBg = card.GetComponent<Image>();
        cardBg.color = UIColorPalette.Card;
        cardBg.raycastTarget = false;

        VerticalLayoutGroup layout = card.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 20, 20);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        CreateTitle(card.transform, "OYUN LOBISI");
        CreateSubtitle(card.transform, "Degere tikla ve sayi yaz, veya +/- kullan");

        roundDurationInput = CreateEditableSettingRow(
            card.transform,
            "Round Suresi",
            EditableSetting.RoundDuration,
            () => lobby.AdjustRoundDuration(-15),
            () => lobby.AdjustRoundDuration(15));

        roundsToWinInput = CreateEditableSettingRow(
            card.transform,
            "Kazanma Skoru",
            EditableSetting.RoundsToWin,
            () => lobby.AdjustRoundsToWin(-1),
            () => lobby.AdjustRoundsToWin(1));

        reviveDurationInput = CreateEditableSettingRow(
            card.transform,
            "Kurtarma Suresi",
            EditableSetting.ReviveDuration,
            () => lobby.AdjustReviveDuration(-1),
            () => lobby.AdjustReviveDuration(1));

        CreateDivider(card.transform);

        runnerBotStatus = CreateToggleRow(card.transform, "Kosucu Botu", ToggleRunnerBot);

        throwerCountInput = CreateEditableSettingRow(
            card.transform,
            "Atici Sayisi",
            EditableSetting.ThrowerCount,
            () => lobby.SetThrowerCount(lobby.throwerCount - 1),
            () => lobby.SetThrowerCount(lobby.throwerCount + 1));

        throwerControlStatus = CreateToggleRow(card.transform, "Atici Kontrolu", ToggleThrowerControl);

        gamepadHint = CreateHint(card.transform);

        CreateDivider(card.transform);
        CreateLoadoutSection(card.transform);

        GameObject spacer = new GameObject("Spacer", typeof(RectTransform));
        spacer.transform.SetParent(card.transform, false);
        LayoutElement spacerLe = spacer.AddComponent<LayoutElement>();
        spacerLe.flexibleHeight = 1f;
        spacerLe.minHeight = 6f;

        CreateStartButton(card.transform);

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(cardRect);
        RefreshLoadoutChips();
    }

    void CreateLoadoutSection(Transform parent)
    {
        TMP_Text header = CreateText("LoadoutHeader", parent, "KACAN YETENEKLERI", 15, FontStyles.Bold);
        header.alignment = TextAlignmentOptions.Center;
        header.color = UIColorPalette.Accent;
        LayoutElement headerLe = header.gameObject.AddComponent<LayoutElement>();
        headerLe.preferredHeight = 22f;

        loadoutHint = CreateText("LoadoutHint", parent, "", 13, FontStyles.Normal);
        loadoutHint.alignment = TextAlignmentOptions.Center;
        loadoutHint.color = UIColorPalette.Muted;
        LayoutElement hintLe = loadoutHint.gameObject.AddComponent<LayoutElement>();
        hintLe.preferredHeight = 18f;

        GameObject row = new GameObject("LoadoutRow", typeof(RectTransform));
        row.transform.SetParent(parent, false);

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = false;

        LayoutElement rowLe = row.AddComponent<LayoutElement>();
        rowLe.preferredHeight = 64f;

        CreateLoadoutChip(row.transform, LoadoutAbility.Flash);
        CreateLoadoutChip(row.transform, LoadoutAbility.Shield);
        CreateLoadoutChip(row.transform, LoadoutAbility.Invisibility);
        CreateLoadoutChip(row.transform, LoadoutAbility.Decoy);
    }

    void CreateLoadoutChip(Transform parent, LoadoutAbility ability)
    {
        Color normal = new Color(0.1f, 0.14f, 0.2f, 1f);
        Color selected = new Color(0.12f, 0.32f, 0.42f, 1f);
        Color hover = new Color(0.18f, 0.26f, 0.36f, 1f);

        GameObject chip = CreatePanel("Chip_" + ability, parent, new Vector2(0f, 60f));
        Image bg = chip.GetComponent<Image>();
        bg.color = normal;
        bg.raycastTarget = false;

        LayoutElement le = chip.AddComponent<LayoutElement>();
        le.preferredHeight = 60f;
        le.flexibleWidth = 1f;
        le.minWidth = 90f;

        TMP_Text label = CreateText("Label", chip.transform, AbilityLoadout.DisplayName(ability), 13, FontStyles.Bold);
        label.alignment = TextAlignmentOptions.Center;
        label.color = UIColorPalette.Muted;
        label.enableWordWrapping = true;
        label.overflowMode = TextOverflowModes.Ellipsis;
        StretchFull(label.rectTransform);
        label.rectTransform.offsetMin = new Vector2(4f, 4f);
        label.rectTransform.offsetMax = new Vector2(-4f, -4f);

        loadoutChips.Add(new LoadoutChip
        {
            ability = ability,
            background = bg,
            label = label,
            normalColor = normal,
            selectedColor = selected,
            hoverColor = hover
        });

        LoadoutAbility captured = ability;
        RegisterClickZone(chip.GetComponent<RectTransform>(), bg, normal, hover, () => OnLoadoutChipClicked(captured));
    }

    void OnLoadoutChipClicked(LoadoutAbility ability)
    {
        if (!AbilityLoadout.TryToggle(ability))
        {
            if (loadoutHint != null)
            {
                if (AbilityLoadout.IsSelected(ability) && AbilityLoadout.Count <= AbilityLoadout.MinPicks)
                {
                    loadoutHint.text = "En az 1 yetenek secili olmali";
                    loadoutHint.color = UIColorPalette.Warning;
                }
                else
                {
                    loadoutHint.text = "En fazla 2 yetenek — once birini kaldir";
                    loadoutHint.color = UIColorPalette.Warning;
                }
            }

            return;
        }

        RefreshLoadoutChips();
    }

    void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    void CreateTitle(Transform parent, string text)
    {
        TMP_Text label = CreateText("Title", parent, text, 28, FontStyles.Bold);
        label.alignment = TextAlignmentOptions.Center;
        label.color = UIColorPalette.Title;

        LayoutElement le = label.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = 36f;
    }

    void CreateSubtitle(Transform parent, string text)
    {
        TMP_Text label = CreateText("Subtitle", parent, text, 14, FontStyles.Normal);
        label.alignment = TextAlignmentOptions.Center;
        label.color = UIColorPalette.Muted;

        LayoutElement le = label.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = 22f;
    }

    void CreateDivider(Transform parent)
    {
        GameObject line = CreatePanel("Divider", parent, new Vector2(0f, 2f));
        Image lineImage = line.GetComponent<Image>();
        lineImage.color = new Color(0.25f, 0.35f, 0.5f, 0.5f);
        lineImage.raycastTarget = false;

        LayoutElement le = line.AddComponent<LayoutElement>();
        le.preferredHeight = 2f;
    }

    TMP_InputField CreateEditableSettingRow(
        Transform parent,
        string label,
        EditableSetting setting,
        System.Action onDecrease,
        System.Action onIncrease)
    {
        GameObject row = new GameObject(label + "Row", typeof(RectTransform));
        row.transform.SetParent(parent, false);

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        LayoutElement rowLe = row.AddComponent<LayoutElement>();
        rowLe.preferredHeight = 38f;

        TMP_Text nameText = CreateText("Label", row.transform, label, 16, FontStyles.Normal);
        nameText.alignment = TextAlignmentOptions.MidlineLeft;
        nameText.color = new Color(0.75f, 0.82f, 0.9f, 1f);
        nameText.enableWordWrapping = false;
        nameText.overflowMode = TextOverflowModes.Ellipsis;

        LayoutElement nameLe = nameText.gameObject.AddComponent<LayoutElement>();
        nameLe.flexibleWidth = 1f;
        nameLe.minWidth = 120f;

        CreateSmallButton(row.transform, "-", onDecrease, 36f);
        TMP_InputField input = CreateNumberInput(row.transform, setting);
        CreateSmallButton(row.transform, "+", onIncrease, 36f);

        return input;
    }

    TMP_InputField CreateNumberInput(Transform parent, EditableSetting setting)
    {
        GameObject inputObject = CreatePanel("ValueInput", parent, new Vector2(96f, 32f));
        Image bg = inputObject.GetComponent<Image>();
        bg.color = new Color(0.12f, 0.18f, 0.26f, 1f);
        bg.raycastTarget = true;

        LayoutElement le = inputObject.AddComponent<LayoutElement>();
        le.preferredWidth = 96f;
        le.minWidth = 96f;
        le.preferredHeight = 32f;

        TMP_Text text = CreateText("Text", inputObject.transform, "0", 18, FontStyles.Bold);
        text.alignment = TextAlignmentOptions.Center;
        text.color = UIColorPalette.Accent;
        text.raycastTarget = false;
        StretchFull(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(4f, 2f);
        text.rectTransform.offsetMax = new Vector2(-4f, -2f);

        GameObject placeholderObject = new GameObject("Placeholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        placeholderObject.transform.SetParent(inputObject.transform, false);
        TMP_Text placeholder = placeholderObject.GetComponent<TextMeshProUGUI>();
        placeholder.text = "sayi";
        placeholder.fontSize = 14;
        placeholder.fontStyle = FontStyles.Italic;
        placeholder.alignment = TextAlignmentOptions.Center;
        placeholder.color = new Color(0.45f, 0.55f, 0.65f, 0.7f);
        placeholder.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null)
        {
            placeholder.font = TMP_Settings.defaultFontAsset;
        }
        StretchFull(placeholder.rectTransform);

        TMP_InputField input = inputObject.AddComponent<TMP_InputField>();
        input.textComponent = text;
        input.placeholder = placeholder;
        input.contentType = TMP_InputField.ContentType.IntegerNumber;
        input.characterLimit = 4;
        input.caretColor = UIColorPalette.Accent;
        input.selectionColor = new Color(0.2f, 0.45f, 0.65f, 0.55f);
        input.targetGraphic = bg;

        ColorBlock colors = input.colors;
        colors.normalColor = new Color(0.12f, 0.18f, 0.26f, 1f);
        colors.highlightedColor = new Color(0.16f, 0.24f, 0.34f, 1f);
        colors.pressedColor = new Color(0.1f, 0.16f, 0.24f, 1f);
        colors.selectedColor = new Color(0.18f, 0.28f, 0.4f, 1f);
        input.colors = colors;

        input.onSelect.AddListener(_ => BeginEdit(input));
        input.onEndEdit.AddListener(value => ApplyEditedValue(setting, value));

        editableInputs.Add(input);
        return input;
    }

    void BeginEdit(TMP_InputField input)
    {
        if (input == null)
        {
            return;
        }

        string digits = ExtractDigits(input.text);
        input.SetTextWithoutNotify(digits);
        input.caretPosition = input.text.Length;
    }

    void ApplyEditedValue(EditableSetting setting, string rawValue)
    {
        if (lobby == null)
        {
            return;
        }

        string digits = ExtractDigits(rawValue);
        if (!int.TryParse(digits, out int value))
        {
            RefreshValues();
            return;
        }

        switch (setting)
        {
            case EditableSetting.RoundDuration:
                lobby.SetRoundDuration(value);
                break;
            case EditableSetting.RoundsToWin:
                lobby.SetRoundsToWin(value);
                break;
            case EditableSetting.ReviveDuration:
                lobby.SetReviveDuration(value);
                break;
            case EditableSetting.ThrowerCount:
                lobby.SetThrowerCount(value);
                break;
        }

        RefreshValues();
    }

    static string ExtractDigits(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder(value.Length);

        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (c >= '0' && c <= '9')
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }

    TMP_Text CreateToggleRow(Transform parent, string label, System.Action onClick)
    {
        GameObject row = new GameObject(label + "ToggleRow", typeof(RectTransform));
        row.transform.SetParent(parent, false);

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        LayoutElement rowLe = row.AddComponent<LayoutElement>();
        rowLe.preferredHeight = 38f;

        TMP_Text nameText = CreateText("Label", row.transform, label, 16, FontStyles.Normal);
        nameText.alignment = TextAlignmentOptions.MidlineLeft;
        nameText.color = new Color(0.75f, 0.82f, 0.9f, 1f);

        LayoutElement nameLe = nameText.gameObject.AddComponent<LayoutElement>();
        nameLe.flexibleWidth = 1f;
        nameLe.minWidth = 120f;

        TMP_Text statusText = CreateText("Status", row.transform, "-", 16, FontStyles.Bold);
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.color = UIColorPalette.Accent;

        LayoutElement statusLe = statusText.gameObject.AddComponent<LayoutElement>();
        statusLe.preferredWidth = 120f;

        CreateSmallButton(row.transform, "Degistir", onClick, 96f);

        return statusText;
    }

    TMP_Text CreateHint(Transform parent)
    {
        TMP_Text hint = CreateText("GamepadHint", parent, "", 14, FontStyles.Italic);
        hint.alignment = TextAlignmentOptions.Center;
        hint.color = UIColorPalette.Warning;

        LayoutElement le = hint.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = 24f;

        return hint;
    }

    void CreateStartButton(Transform parent)
    {
        Color normal = UIColorPalette.StartButton;
        Color hover = UIColorPalette.StartButtonHover;
        CreateClickButton(parent, "StartButton", "OYUNU BASLAT", 48f, normal, hover, OnStartClicked, 0f, 20f);
    }

    void CreateClickButton(Transform parent, string objectName, string label, float height, Color normalColor, Color hoverColor, System.Action onClick, float width = 0f, float fontSize = 16f)
    {
        GameObject buttonObj = CreatePanel(objectName, parent, new Vector2(width, height));
        Image bg = buttonObj.GetComponent<Image>();
        bg.color = normalColor;
        bg.raycastTarget = false;

        TMP_Text text = CreateText("Label", buttonObj.transform, label, fontSize, FontStyles.Bold);
        text.alignment = TextAlignmentOptions.Center;
        RectTransform textRect = text.GetComponent<RectTransform>();
        StretchFull(textRect);
        text.color = Color.white;

        LayoutElement le = buttonObj.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.minHeight = height;
        if (width > 0f)
        {
            le.preferredWidth = width;
            le.minWidth = width;
        }

        RegisterClickZone(buttonObj.GetComponent<RectTransform>(), bg, normalColor, hoverColor, onClick);
    }

    void CreateSmallButton(Transform parent, string label, System.Action onClick, float width = 36f)
    {
        Color normal = new Color(0.18f, 0.24f, 0.32f, 1f);
        Color hover = new Color(0.28f, 0.36f, 0.48f, 1f);
        CreateClickButton(parent, "Btn_" + label, label, 32f, normal, hover, onClick, width);
    }

    void RegisterClickZone(RectTransform rect, Image image, Color normalColor, Color hoverColor, System.Action action)
    {
        clickZones.Add(new ClickZone
        {
            rect = rect,
            image = image,
            normalColor = normalColor,
            hoverColor = hoverColor,
            action = action
        });
    }

    void OnStartClicked()
    {
        if (lobby != null)
        {
            lobby.StartMatch();
        }
    }

    GameObject CreatePanel(string name, Transform parent, Vector2 size)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.sizeDelta = size;

        Image image = panel.GetComponent<Image>();
        image.sprite = GetWhiteSprite();
        image.type = Image.Type.Sliced;

        return panel;
    }

    TMP_Text CreateText(string name, Transform parent, string text, float size, FontStyles style)
    {
        GameObject textObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(parent, false);

        TMP_Text tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.raycastTarget = false;

        if (TMP_Settings.defaultFontAsset != null)
        {
            tmp.font = TMP_Settings.defaultFontAsset;
        }

        return tmp;
    }

    static Sprite GetWhiteSprite()
    {
        if (whiteSprite != null) return whiteSprite;

        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        whiteSprite = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));

        return whiteSprite;
    }

    void ToggleRunnerBot()
    {
        lobby.SetRunnerBotEnabled(!lobby.runnerBotEnabled);
        RefreshValues();
    }

    void ToggleThrowerControl()
    {
        lobby.SetThrowerIsHuman(!lobby.throwerIsHuman);
        RefreshValues();
    }
}
