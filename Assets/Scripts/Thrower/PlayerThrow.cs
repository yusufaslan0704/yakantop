using System.Collections.Generic;
using UnityEngine;

public class PlayerThrow : MonoBehaviour
{
    [Header("Ball Settings")]
    public BallData ballData;
    [Tooltip("Secilebilir top turleri. Bos ise ThrowerBot.ballTypes kopyalanir.")]
    public BallData[] ballTypes;
    public Transform throwPoint;

    [Header("Aim Settings")]
    public Camera playerCamera;
    public float aimDistance = 100f;

    [Header("Throw Force")]
    public float minThrowForce = 16f;
    public float maxThrowForce = 42f;
    public float maxChargeTime = 0.95f;

    [Header("Throw Cooldown")]
    public float cooldown = 0.42f;

    [Header("Throw Feel")]
    public float throwShakeDuration = 0.06f;
    public float throwShakeStrength = 0.07f;
    public float throwFovPunch = 2.8f;

    [Header("Animation Sync")]
    [Tooltip("Atis animasyonu basladiktan sonra topun cikacagi sure (sn). " +
             "Animasyon bu surede release pozuna yetisecek sekilde hizlanir.")]
    public float ballReleaseDelay = 0.38f;

    [Header("Curve Path Draw")]
    [Tooltip("Egri top charge: mouse ile serbest yol cizimi.")]
    public bool humanFollowsPlannedPath = true;
    [Tooltip("Pixel basina yana/yukari kayma.")]
    public float drawMouseSensitivity = 0.028f;
    public float drawStickSensitivity = 5.5f;
    [Tooltip("Yolun karsi tarafa sabit uzama hizi.")]
    public float drawAutoExtendSpeed = 7.5f;
    [Tooltip("Yeni nokta araligi. Dusuk = daha detayli cizgi.")]
    public float drawMinSegment = 0.1f;
    public float drawMaxLength = 48f;
    public int drawMaxPoints = 180;
    public float drawMinHeight = 0.35f;
    public float drawMaxHeight = 6f;

    // Animasyon baslar baslamaz; top henuz cikmamistir.
    public event System.Action OnThrowStarted;
    // Top gercekten spawn oldugunda.
    public event System.Action OnBallThrown;
    public event System.Action OnBallTypeChanged;

    private float chargeStartTime;
    private float nextThrowTime = 0f;
    private bool isCharging = false;
    private bool isThrowInProgress = false;
    private Coroutine pendingThrowRoutine;
    private int selectedBallIndex;
    private Vector3[] plannedPath;
    private float plannedPathSpeed;

    readonly List<Vector3> drawnPath = new List<Vector3>(64);
    Vector3 drawTip;
    Vector3 drawOutward;
    float drawnLength;
    bool drawingPath;

    private PlayerHealth playerHealth;
    private PlayerRole playerRole;
    private PlayerInputHandler inputHandler;
    private ThrowerBot throwerBot;
    private PlayerAirLift airLift;

    const int EmptySlot = -1;
    int primarySlot = EmptySlot;
    int comboSlot = EmptySlot;

    public int SelectedBallIndex => selectedBallIndex;
    public int PrimarySlotIndex => primarySlot;
    public int ComboSlotIndex => comboSlot;

    int BallCount => ballTypes != null ? ballTypes.Length : 0;

    public int AirLiftSlotIndex => ThrowerAbilityRegistry.SlotFor(BallCount, ThrowerAbilityId.AirLift);
    public int InvisSlotIndex => ThrowerAbilityRegistry.SlotFor(BallCount, ThrowerAbilityId.Invis);
    public int FakeSlotIndex => ThrowerAbilityRegistry.SlotFor(BallCount, ThrowerAbilityId.Fake);
    public int ShadowSlotIndex => ThrowerAbilityRegistry.SlotFor(BallCount, ThrowerAbilityId.Shadow);

    public bool AirLiftModeSelected => IsAbilitySelected(ThrowerAbilityId.AirLift);
    public bool InvisModeSelected => IsAbilitySelected(ThrowerAbilityId.Invis);
    public bool FakeModeSelected => IsAbilitySelected(ThrowerAbilityId.Fake);
    public bool ShadowModeSelected => IsAbilitySelected(ThrowerAbilityId.Shadow);

    // UI geriye uyumluluk: son vurgulanan slot.
    public int SelectedSlotIndex => comboSlot >= 0 ? comboSlot : primarySlot;
    public int SelectableSlotCount => BallCount + ThrowerAbilityRegistry.Count;
    public BallData[] AvailableBallTypes => ballTypes;
    public bool IsDrawingThrowPath => isCharging && drawingPath;
    public bool IsThrowBusy => isThrowInProgress || Time.time < nextThrowTime || Time.time < fakeLockUntil;
    public bool IsSpawningEchoBall { get; private set; }
    public IReadOnlyList<Vector3> DrawnPathPoints => drawnPath;

    float fakeLockUntil;
    float lastThrowForce = 28f;
    Vector3 lastAimPoint;

    void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerRole = GetComponent<PlayerRole>();
        inputHandler = GetComponent<PlayerInputHandler>();
        throwerBot = GetComponent<ThrowerBot>();
        airLift = GetComponent<PlayerAirLift>();

        ThrowerAbilityRegistry.EnsureComponents(gameObject);
        airLift = GetComponent<PlayerAirLift>();

        EnsureBallTypes();
        SyncSelectedFromBallData();
    }

    public bool IsAbilitySelected(ThrowerAbilityId id)
    {
        if (id == ThrowerAbilityId.None)
        {
            return false;
        }

        return ThrowerAbilityRegistry.AbilityAtSlot(BallCount, primarySlot) == id ||
               ThrowerAbilityRegistry.AbilityAtSlot(BallCount, comboSlot) == id;
    }

    public int GetAbilitySlot(ThrowerAbilityId id)
    {
        return ThrowerAbilityRegistry.SlotFor(BallCount, id);
    }

    void EnsureBallTypes()
    {
        if (ballTypes != null && ballTypes.Length > 0)
        {
            return;
        }

        if (throwerBot != null && throwerBot.ballTypes != null && throwerBot.ballTypes.Length > 0)
        {
            ballTypes = throwerBot.ballTypes;
        }
    }

    bool IsHumanThrower()
    {
        return inputHandler != null && inputHandler.enabled &&
               (throwerBot == null || !throwerBot.enabled);
    }

    void SyncSelectedFromBallData()
    {
        selectedBallIndex = 0;
        primarySlot = EmptySlot;
        comboSlot = EmptySlot;

        if (ballTypes == null || ballTypes.Length == 0)
        {
            return;
        }

        // Baslangicta secim yok — oyuncu komboyu kendisi belirler.
        // ballData sadece fallback referans olarak ilk gecerli toptan alinir.
        if (ballData == null)
        {
            for (int i = 0; i < ballTypes.Length; i++)
            {
                if (ballTypes[i] != null)
                {
                    ballData = ballTypes[i];
                    selectedBallIndex = i;
                    break;
                }
            }
        }
        else
        {
            for (int i = 0; i < ballTypes.Length; i++)
            {
                if (ballTypes[i] == ballData)
                {
                    selectedBallIndex = i;
                    break;
                }
            }
        }
    }

    public bool SelectBallIndex(int index)
    {
        EnsureBallTypes();
        if (ballTypes == null || ballTypes.Length == 0)
        {
            return false;
        }

        if (index < 0 || index >= ballTypes.Length || ballTypes[index] == null)
        {
            return false;
        }

        SelectSlot(index);
        return true;
    }

    public void SelectAirLiftMode()
    {
        SelectSlot(GetAbilitySlot(ThrowerAbilityId.AirLift));
    }

    // Cift secim: 1. slot sari (primary), 2. slot yesil (combo).
    // Ayni slota tekrar bas = kaldir / promote.
    public void SelectSlot(int slot)
    {
        EnsureBallTypes();
        int count = SelectableSlotCount;
        if (slot < 0 || slot >= count)
        {
            return;
        }

        int ballCount = ballTypes != null ? ballTypes.Length : 0;
        if (slot < ballCount && (ballTypes == null || ballTypes[slot] == null))
        {
            return;
        }

        if (slot == primarySlot && comboSlot < 0)
        {
            primarySlot = EmptySlot;
        }
        else if (slot == primarySlot && comboSlot >= 0)
        {
            primarySlot = comboSlot;
            comboSlot = EmptySlot;
        }
        else if (slot == comboSlot)
        {
            comboSlot = EmptySlot;
        }
        else if (primarySlot < 0)
        {
            primarySlot = slot;
        }
        else if (comboSlot < 0)
        {
            comboSlot = slot;
        }
        else
        {
            comboSlot = slot;
        }

        SyncBallFromSlots();
        OnBallTypeChanged?.Invoke();
    }

    void SyncBallFromSlots()
    {
        int ballSlot = GetActiveBallSlot();
        if (ballSlot >= 0 && ballTypes != null && ballSlot < ballTypes.Length && ballTypes[ballSlot] != null)
        {
            selectedBallIndex = ballSlot;
            ballData = ballTypes[ballSlot];
            return;
        }

        // Sadece yuksek atis: top Fast'e dusmesin diye ballData'yi koru;
        // ResolveThrowBallData Fast'i ayri cozer.
    }

    // Combo'daki top oncelikli, yoksa primary top (ozel yetenek slotlari haric).
    public int GetActiveBallSlot()
    {
        if (comboSlot >= 0 && !ThrowerAbilityRegistry.IsAbilitySlot(BallCount, comboSlot))
        {
            return comboSlot;
        }

        if (primarySlot >= 0 && !ThrowerAbilityRegistry.IsAbilitySlot(BallCount, primarySlot))
        {
            return primarySlot;
        }

        return EmptySlot;
    }

    public bool IsSpecialAbilitySlot(int slot)
    {
        return ThrowerAbilityRegistry.IsAbilitySlot(BallCount, slot);
    }

    public BallData GetResolvedThrowBall()
    {
        return ResolveThrowBallData();
    }

    public float GetLastThrowForce()
    {
        return lastThrowForce;
    }

    public Vector3 GetLastAimPoint()
    {
        return lastAimPoint;
    }

    // Golge capraz atis: cooldown/anim yok, sessiz echo.
    public GameObject SpawnEchoBall(Vector3 origin, Vector3 direction, float force, BallData data)
    {
        if (data == null || data.prefab == null) return null;
        if (direction.sqrMagnitude < 0.0001f) return null;

        IsSpawningEchoBall = true;
        Transform previousThrowPoint = throwPoint;
        GameObject tempPoint = new GameObject("EchoThrowPoint");
        tempPoint.transform.position = origin - direction.normalized * 0.6f;
        throwPoint = tempPoint.transform;

        GameObject ball = SpawnAndThrowBall(
            data, direction.normalized, force, playFeedback: false, spawnLateralOffset: Vector3.zero);

        throwPoint = previousThrowPoint;
        Destroy(tempPoint);
        IsSpawningEchoBall = false;
        return ball;
    }

    // Sahte atis: animasyon baslar, top cikmaz.
    public bool TryBeginFakeThrow(float lockDuration)
    {
        if (!GameManager.RoundIsActive) return false;
        if (playerHealth != null && playerHealth.IsEliminated) return false;
        if (playerRole != null && playerRole.roleType != RoleType.Thrower) return false;
        if (IsThrowBusy) return false;

        isCharging = false;
        CancelPendingThrow();
        fakeLockUntil = Time.time + Mathf.Max(0.15f, lockDuration);
        OnThrowStarted?.Invoke();
        return true;
    }

    public void CycleBall(int delta)
    {
        EnsureBallTypes();
        int count = SelectableSlotCount;
        if (count <= 0 || delta == 0)
        {
            return;
        }

        int current = primarySlot >= 0 ? primarySlot : 0;
        for (int step = 0; step < count; step++)
        {
            current = (current + delta) % count;
            if (current < 0) current += count;

            int ballCount = ballTypes != null ? ballTypes.Length : 0;
            if (current < ballCount && (ballTypes == null || ballTypes[current] == null))
            {
                continue;
            }

            // Scroll sadece toplar arasinda; ozel yetenekler tusla secilir.
            if (current >= ballCount)
            {
                continue;
            }

            primarySlot = current;
            comboSlot = EmptySlot;
            SyncBallFromSlots();
            OnBallTypeChanged?.Invoke();
            return;
        }
    }

    void ApplySelectedBall(bool notify)
    {
        if (ballTypes == null || ballTypes.Length == 0)
        {
            return;
        }

        selectedBallIndex = Mathf.Clamp(selectedBallIndex, 0, ballTypes.Length - 1);
        if (ballTypes[selectedBallIndex] != null)
        {
            ballData = ballTypes[selectedBallIndex];
        }

        if (notify)
        {
            OnBallTypeChanged?.Invoke();
        }
    }

    void Update()
    {
        // Round bittiyse atış yapılamaz.
        if (!GameManager.RoundIsActive)
        {
            isCharging = false;
            CancelPendingThrow();
            return;
        }

        // Elenen oyuncu top atamaz.
        if (playerHealth != null && playerHealth.IsEliminated)
        {
            isCharging = false;
            CancelPendingThrow();
            return;
        }

        // Emote cemberi acikken atis baslatma.
        if (EmoteWheelUI.IsOpen)
        {
            return;
        }

        // Mouse ile sadece Thrower rolündeki oyuncu top atabilir.
        if (playerRole != null && playerRole.roleType != RoleType.Thrower)
        {
            isCharging = false;
            return;
        }

        // Cooldown / devam eden atis / fake kilidi bitmeden yeni atis hazirlanamaz.
        if (Time.time < nextThrowTime || isThrowInProgress || Time.time < fakeLockUntil)
        {
            return;
        }

        // Yuksek atis modu: once sag tikle yuksel, birak, sonra sol tik.
        if (airLift != null && airLift.BlocksGroundThrow())
        {
            isCharging = false;
            return;
        }

        // Atis girisi: handler varsa oradan (sol tik / gamepad RT), yoksa eski usul fare.
        bool chargeStarted = inputHandler != null
            ? inputHandler.ThrowPressed
            : Input.GetMouseButtonDown(0);

        bool chargeReleased = inputHandler != null
            ? inputHandler.ThrowReleased
            : Input.GetMouseButtonUp(0);

        if (chargeStarted)
        {
            StartCharge();
        }

        if (isCharging)
        {
            TickPathDraw();
        }

        if (chargeReleased && isCharging)
        {
            ReleaseThrow();
        }
    }

    void StartCharge()
    {
        isCharging = true;
        chargeStartTime = Time.time;
        BeginPathDrawIfNeeded();
    }

    bool IsFreehandCurveBall()
    {
        if (airLift != null && airLift.IsArmedForThrow)
        {
            return false;
        }

        return ballData != null && ballData.curveForce > 0f;
    }

    void BeginPathDrawIfNeeded()
    {
        drawnPath.Clear();
        drawnLength = 0f;
        drawingPath = false;
        ClearPlannedPath();

        if (!IsFreehandCurveBall() || throwPoint == null)
        {
            return;
        }

        drawingPath = true;
        drawOutward = GetThrowDirection();
        drawOutward.y = 0f;
        if (drawOutward.sqrMagnitude < 0.0001f)
        {
            drawOutward = transform.forward;
            drawOutward.y = 0f;
        }

        drawOutward.Normalize();

        Vector3 origin = throwPoint.position + Vector3.up * 0.15f + drawOutward * 0.45f;
        drawTip = origin;
        drawnPath.Add(origin);
        // Ilk ileri nokta — tek noktali path olmasin.
        drawTip = origin + drawOutward * drawMinSegment;
        drawnPath.Add(drawTip);
        drawnLength = drawMinSegment;
        PublishDrawnPath();
    }

    void TickPathDraw()
    {
        if (!drawingPath || throwPoint == null)
        {
            return;
        }

        Vector2 mouseDelta = ReadDrawDelta();
        Vector3 side = Vector3.Cross(Vector3.up, drawOutward);
        if (side.sqrMagnitude < 0.0001f)
        {
            side = transform.right;
        }

        side.Normalize();

        // Mouse: X = zigzag (yan), Y = yukari/asagi (yukari mouse = yukari cizgi).
        drawTip += side * mouseDelta.x * drawMouseSensitivity;
        drawTip += Vector3.up * mouseDelta.y * drawMouseSensitivity;

        // Karsi tarafa sabit hizda uzar; mouse sadece sekli cizer.
        if (drawnLength < drawMaxLength)
        {
            float extend = drawAutoExtendSpeed * Time.deltaTime;
            drawTip += drawOutward * extend;
            drawnLength += extend;
        }

        drawTip.y = Mathf.Clamp(drawTip.y, drawMinHeight, drawMaxHeight);

        // Cok geriye (aticinin arkasi) kacmasin; yana/zigzag serbest.
        Vector3 fromThrow = drawTip - throwPoint.position;
        float along = Vector3.Dot(fromThrow, drawOutward);
        if (along < 0.4f)
        {
            drawTip += drawOutward * (0.4f - along);
        }

        Vector3 last = drawnPath[drawnPath.Count - 1];
        if (Vector3.Distance(last, drawTip) >= drawMinSegment)
        {
            if (drawnPath.Count >= drawMaxPoints)
            {
                // Eski noktalari seyreltmeden ucu guncelle.
                drawnPath[drawnPath.Count - 1] = drawTip;
            }
            else
            {
                drawnPath.Add(drawTip);
            }

            PublishDrawnPath();
        }
        else
        {
            // Son noktayi canli takip ettir (akici cizgi).
            drawnPath[drawnPath.Count - 1] = drawTip;
            PublishDrawnPath();
        }
    }

    Vector2 ReadDrawDelta()
    {
        if (inputHandler != null && inputHandler.scheme == ControlScheme.Gamepad)
        {
            if (inputHandler.gamepadIndex >= 0 &&
                inputHandler.gamepadIndex < UnityEngine.InputSystem.Gamepad.all.Count)
            {
                var pad = UnityEngine.InputSystem.Gamepad.all[inputHandler.gamepadIndex];
                Vector2 stick = pad.rightStick.ReadValue();
                return stick * drawStickSensitivity * 60f * Time.deltaTime;
            }

            return Vector2.zero;
        }

        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null)
        {
            return mouse.delta.ReadValue();
        }

        return new Vector2(Input.GetAxis("Mouse X") * 20f, Input.GetAxis("Mouse Y") * 20f);
    }

    void PublishDrawnPath()
    {
        float speed = Mathf.Lerp(minThrowForce, maxThrowForce, GetChargePercent());
        float mass = ResolvePrefabMass(ballData);
        SetPlannedPath(drawnPath, speed / Mathf.Max(0.05f, mass));
    }

    public void SetPlannedPath(List<Vector3> points, float travelSpeed)
    {
        if (points == null || points.Count < 2)
        {
            plannedPath = null;
            plannedPathSpeed = 0f;
            return;
        }

        plannedPath = points.ToArray();
        plannedPathSpeed = Mathf.Max(4f, travelSpeed);
    }

    public void ClearPlannedPath()
    {
        plannedPath = null;
        plannedPathSpeed = 0f;
    }

    void ReleaseThrow()
    {
        isCharging = false;

        // Cizim yolunu atistan once kilitle.
        if (drawingPath && drawnPath.Count >= 2)
        {
            PublishDrawnPath();
        }

        drawingPath = false;

        float chargeTime = Time.time - chargeStartTime;
        float chargePercent = Mathf.Clamp01(chargeTime / maxChargeTime);

        float finalForce = Mathf.Lerp(minThrowForce, maxThrowForce, chargePercent);

        BeginThrow(ResolveThrowBallData(), GetThrowDirection(), finalForce, rotateToAim: true);
    }

    void ThrowBall(float force)
    {
        BeginThrow(ResolveThrowBallData(), GetThrowDirection(), force, rotateToAim: true);
    }

    BallData ResolveThrowBallData()
    {
        int ballSlot = GetActiveBallSlot();
        if (ballSlot >= 0 && ballTypes != null && ballSlot < ballTypes.Length && ballTypes[ballSlot] != null)
        {
            return ballTypes[ballSlot];
        }

        // Sadece yuksek atis seciliyse Fast.
        if (AirLiftModeSelected && airLift != null)
        {
            BallData fast = airLift.ResolveFastBall();
            if (fast != null)
            {
                return fast;
            }
        }

        return ballData;
    }

    // Bot atışı: atış gücü ve top tipi BallData'dan gelir.
    public void BotThrowAt(Vector3 targetPosition, BallData botBallData)
    {
        // Round bittiyse bot da atış yapamaz.
        if (!GameManager.RoundIsActive)
        {
            return;
        }

        // Elenen bot top atamaz.
        if (playerHealth != null && playerHealth.IsEliminated)
        {
            return;
        }

        // Bot da sadece Thrower rolündeyse top atabilir.
        if (playerRole != null && playerRole.roleType != RoleType.Thrower)
        {
            return;
        }

        // Cooldown kontrolü.
        if (Time.time < nextThrowTime || isThrowInProgress)
        {
            return;
        }

        if (botBallData == null || botBallData.prefab == null)
        {
            Debug.LogWarning("Bot atışı için Ball Data eksik!");
            return;
        }

        if (throwPoint == null)
        {
            Debug.LogWarning("Bot atışı için Throw Point eksik!");
            return;
        }

        Vector3 throwDirection = (targetPosition - throwPoint.position).normalized;
        BeginThrow(botBallData, throwDirection, botBallData.throwForce, rotateToAim: true);
    }

    void BeginThrow(BallData dataToThrow, Vector3 throwDirection, float force, bool rotateToAim)
    {
        if (dataToThrow == null || dataToThrow.prefab == null)
        {
            Debug.LogWarning("Ball Data atanmadı veya prefab eksik!");
            return;
        }

        if (throwPoint == null)
        {
            Debug.LogWarning("Throw Point atanmadı!");
            return;
        }

        if (throwDirection == Vector3.zero)
        {
            return;
        }

        if (rotateToAim)
        {
            RotatePlayerToThrowDirection(throwDirection);
        }

        // Animasyon hemen baslar; top release aninda cikar.
        CancelPendingThrow();
        isThrowInProgress = true;
        nextThrowTime = Time.time + Mathf.Max(cooldown, ballReleaseDelay);
        OnThrowStarted?.Invoke();

        pendingThrowRoutine = StartCoroutine(ReleaseBallAfterDelay(dataToThrow, throwDirection, force, null));
    }

    // Yetenek atisi (Trap vb.): tek top + loft + spawn sonrasi ayar.
    public bool TryThrowAbilityBall(
        BallData dataToThrow,
        float force,
        float loft,
        System.Action<Ball> configureBall)
    {
        if (!GameManager.RoundIsActive) return false;
        if (playerHealth != null && playerHealth.IsEliminated) return false;
        if (playerRole != null && playerRole.roleType != RoleType.Thrower) return false;
        if (Time.time < nextThrowTime || isThrowInProgress) return false;
        if (dataToThrow == null || dataToThrow.prefab == null || throwPoint == null) return false;

        Vector3 throwDirection = GetThrowDirection();
        if (throwDirection == Vector3.zero) return false;

        if (loft > 0f)
        {
            throwDirection = (throwDirection + Vector3.up * loft).normalized;
        }

        RotatePlayerToThrowDirection(throwDirection);

        isCharging = false;
        CancelPendingThrow();
        isThrowInProgress = true;
        nextThrowTime = Time.time + Mathf.Max(cooldown, 0.35f);
        OnThrowStarted?.Invoke();

        pendingThrowRoutine = StartCoroutine(
            ReleaseBallAfterDelay(dataToThrow, throwDirection, force, configureBall));
        return true;
    }

    System.Collections.IEnumerator ReleaseBallAfterDelay(
        BallData dataToThrow,
        Vector3 throwDirection,
        float force,
        System.Action<Ball> configureBall)
    {
        float delay = Mathf.Max(0f, ballReleaseDelay);

        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        pendingThrowRoutine = null;

        if (!GameManager.RoundIsActive ||
            (playerHealth != null && playerHealth.IsEliminated))
        {
            isThrowInProgress = false;
            yield break;
        }

        GameObject ballGo = SpawnAndThrowBall(
            dataToThrow, throwDirection, force, playFeedback: true, spawnLateralOffset: Vector3.zero);

        if (configureBall != null && ballGo != null)
        {
            Ball ball = ballGo.GetComponent<Ball>();
            if (ball != null)
            {
                configureBall(ball);
            }
        }

        isThrowInProgress = false;
    }

    void CancelPendingThrow()
    {
        if (pendingThrowRoutine != null)
        {
            StopCoroutine(pendingThrowRoutine);
            pendingThrowRoutine = null;
        }

        isThrowInProgress = false;
    }

    void OnDisable()
    {
        CancelPendingThrow();
        isCharging = false;
    }

    Vector3 GetThrowDirection()
    {
        // Gamepad: karakterin baktigi yon.
        bool useForwardAim = inputHandler != null &&
                             inputHandler.scheme == ControlScheme.Gamepad;

        if (useForwardAim || playerCamera == null)
        {
            return GetBodyForwardAim();
        }

        return GetCameraAimDirection();
    }

    Vector3 GetBodyForwardAim()
    {
        // Yuksek atis: gamepad da kamera egimini kullansin (asagi atmak icin).
        if (AllowSteepDownAim() && playerCamera != null)
        {
            return GetCameraForwardAim();
        }

        Vector3 forward = transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.0001f)
        {
            return Vector3.zero;
        }

        // Hafif yukari egim: top hemen yere gomulmesin.
        return (forward.normalized + Vector3.up * 0.12f).normalized;
    }

    Vector3 GetCameraAimDirection()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        // Varsayilan: kameranin baktigi uzak nokta (yakin zemin isabetini kullanma).
        float defaultRange = Mathf.Min(aimDistance, 38f);
        Vector3 targetPoint = ray.GetPoint(defaultRange);

        RaycastHit[] hits = Physics.RaycastAll(ray, aimDistance, ~0, QueryTriggerInteraction.Ignore);
        float bestDist = float.MaxValue;
        bool lockedOntoPlayer = false;
        bool steepDown = AllowSteepDownAim();
        float minHitDistance = steepDown ? 1.15f : 3.5f;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null) continue;

            // Kendi collider / cok yakin isabet (ayak alti zemin) atisi asagi ceker.
            if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            if (hit.distance < minHitDistance)
            {
                continue;
            }

            PlayerRole hitRole = hit.collider.GetComponentInParent<PlayerRole>();
            bool isEscapeTarget = hitRole != null &&
                                  (hitRole.roleType == RoleType.Runner || hitRole.roleType == RoleType.Saver);

            // Zemin: yerdeyken Y'yi atis yuksekligine cek (uzak nisan).
            // Yuksek atista gercek zemin noktasi — asagi atilabilsin.
            bool isFloor = hit.normal.y > 0.55f;
            Vector3 aimPoint = hit.point;
            if (isFloor && !isEscapeTarget && !steepDown)
            {
                float aimHeight = throwPoint != null ? throwPoint.position.y : 1.1f;
                aimPoint = new Vector3(hit.point.x, aimHeight, hit.point.z);
            }

            // Oyuncu isabeti her zaman zemine tercih edilir.
            if (isEscapeTarget)
            {
                if (!lockedOntoPlayer || hit.distance < bestDist)
                {
                    lockedOntoPlayer = true;
                    bestDist = hit.distance;
                    targetPoint = aimPoint;
                }

                continue;
            }

            if (lockedOntoPlayer) continue;

            if (hit.distance < bestDist)
            {
                bestDist = hit.distance;
                targetPoint = aimPoint;
            }
        }

        if (PlayerFlash.AreThrowersBlinded())
        {
            targetPoint += Random.insideUnitSphere * 3.5f;
        }

        Vector3 dir = targetPoint - throwPoint.position;
        if (dir.sqrMagnitude < 0.01f)
        {
            return GetCameraForwardAim();
        }

        // Nisan neredeyse geriye veya dimdik asagiysa kamera ileri yonunu kullan.
        Vector3 flat = dir;
        flat.y = 0f;
        Vector3 bodyFlat = transform.forward;
        bodyFlat.y = 0f;

        if (!steepDown)
        {
            if (flat.sqrMagnitude < 0.35f ||
                (bodyFlat.sqrMagnitude > 0.01f && Vector3.Dot(flat.normalized, bodyFlat.normalized) < 0.15f))
            {
                return GetCameraForwardAim();
            }
        }
        else if (bodyFlat.sqrMagnitude > 0.01f &&
                 flat.sqrMagnitude > 0.35f &&
                 Vector3.Dot(flat.normalized, bodyFlat.normalized) < -0.2f)
        {
            // Tamamen geriye bakisi yine kamera forward'a dusur.
            return GetCameraForwardAim();
        }

        return ClampThrowDirection(dir.normalized, steepDown);
    }

    Vector3 GetCameraForwardAim()
    {
        Vector3 forward = playerCamera.transform.forward;
        return ClampThrowDirection(forward, AllowSteepDownAim());
    }

    Vector3 ClampThrowDirection(Vector3 forward, bool steepDown)
    {
        float minY = steepDown ? -0.95f : -0.35f;
        float maxY = 0.55f;
        forward.y = Mathf.Clamp(forward.y, minY, maxY);

        if (forward.sqrMagnitude < 0.0001f)
        {
            return steepDown ? Vector3.down : GetBodyForwardAimFlat();
        }

        return forward.normalized;
    }

    Vector3 GetBodyForwardAimFlat()
    {
        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
        {
            return Vector3.forward;
        }

        return (forward.normalized + Vector3.up * 0.12f).normalized;
    }

    bool AllowSteepDownAim()
    {
        return airLift != null && airLift.IsElevated;
    }

    // Yetenek: nisan etrafinda yayilan coklu top (orn. 20° / 3 top).
    // overrideBall: verilirse o tip kullanilir (volley = Fast).
    // loft: ekstra yukari egim (menzil).
    public bool TryThrowSpread(
        float totalSpreadDegrees,
        int count,
        float force,
        BallData overrideBall = null,
        float loft = 0f)
    {
        if (!GameManager.RoundIsActive) return false;
        if (playerHealth != null && playerHealth.IsEliminated) return false;
        if (playerRole != null && playerRole.roleType != RoleType.Thrower) return false;
        if (Time.time < nextThrowTime || isThrowInProgress) return false;

        BallData dataToThrow = overrideBall != null ? overrideBall : ballData;
        if (dataToThrow == null || dataToThrow.prefab == null || throwPoint == null) return false;

        Vector3 centerDir = GetThrowDirection();
        if (centerDir == Vector3.zero) return false;

        RotatePlayerToThrowDirection(centerDir);

        isCharging = false;
        CancelPendingThrow();
        isThrowInProgress = true;
        nextThrowTime = Time.time + Mathf.Max(cooldown, 0.35f);
        OnThrowStarted?.Invoke();

        pendingThrowRoutine = StartCoroutine(
            ReleaseSpreadAfterDelay(dataToThrow, centerDir, force, totalSpreadDegrees, count, loft));
        return true;
    }

    System.Collections.IEnumerator ReleaseSpreadAfterDelay(
        BallData dataToThrow,
        Vector3 centerDir,
        float force,
        float totalSpreadDegrees,
        int count,
        float loft)
    {
        float delay = Mathf.Min(0.18f, Mathf.Max(0f, ballReleaseDelay * 0.45f));
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        pendingThrowRoutine = null;

        if (!GameManager.RoundIsActive ||
            (playerHealth != null && playerHealth.IsEliminated))
        {
            isThrowInProgress = false;
            yield break;
        }

        count = Mathf.Max(1, count);
        float half = totalSpreadDegrees * 0.5f;

        // Yatay yayilim: up ekseni etrafinda -half .. +half
        Vector3 flat = centerDir;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.0001f)
        {
            flat = transform.forward;
            flat.y = 0f;
        }

        flat.Normalize();
        float pitchY = centerDir.y + loft;

        // Ayni volley'deki toplar birbirine carpmasin / birbirini yok etmesin.
        Collider[] spawnedColliders = new Collider[count];

        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0.5f : i / (float)(count - 1);
            float yaw = Mathf.Lerp(-half, half, t);
            Vector3 dir = Quaternion.AngleAxis(yaw, Vector3.up) * flat;
            dir = (dir + Vector3.up * pitchY).normalized;

            Vector3 lateral = Vector3.Cross(Vector3.up, flat).normalized;
            // Spawn araligi: dar acida bile collider overlap olmasin.
            Vector3 spawnOffset = lateral * Mathf.Lerp(-0.55f, 0.55f, t);

            GameObject ball = SpawnAndThrowBall(
                dataToThrow, dir, force, playFeedback: i == 0, spawnLateralOffset: spawnOffset);

            if (ball != null)
            {
                spawnedColliders[i] = ball.GetComponent<Collider>();
            }
        }

        IgnoreCollisionsBetween(spawnedColliders);

        isThrowInProgress = false;
    }

    static void IgnoreCollisionsBetween(Collider[] colliders)
    {
        if (colliders == null) return;

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] == null) continue;

            for (int j = i + 1; j < colliders.Length; j++)
            {
                if (colliders[j] == null) continue;
                Physics.IgnoreCollision(colliders[i], colliders[j], true);
            }
        }
    }

    void SpawnAndThrowBall(BallData dataToThrow, Vector3 throwDirection, float force)
    {
        SpawnAndThrowBall(dataToThrow, throwDirection, force, playFeedback: true, spawnLateralOffset: Vector3.zero);
    }

    GameObject SpawnAndThrowBall(
        BallData dataToThrow,
        Vector3 throwDirection,
        float force,
        bool playFeedback,
        Vector3 spawnLateralOffset)
    {
        Vector3 spawnPosition = throwPoint.position + throwDirection * 0.6f + spawnLateralOffset;

        GameObject ball = Instantiate(dataToThrow.prefab, spawnPosition, Quaternion.identity);
        SceneFolders.ParentTo(ball.transform, SceneFolders.RuntimeSpawned);

        Color trailColor = CombatVfx.ReadBallColor(ball, Color.white);
        CombatVfx.TintBallTrail(ball, trailColor);

        Ball ballScript = ball.GetComponent<Ball>();
        if (ballScript != null)
        {
            ballScript.SetOwner(gameObject);
            ballScript.SetData(dataToThrow);
        }

        Rigidbody ballRb = ball.GetComponent<Rigidbody>();
        bool followPath = humanFollowsPlannedPath && IsHumanThrower() &&
                          plannedPath != null && plannedPath.Length >= 2 && playFeedback;

        if (followPath && ballScript != null)
        {
            // Onizlemede cizilen iz — top birebir bu yolu izler.
            Vector3[] pathCopy = (Vector3[])plannedPath.Clone();
            pathCopy[0] = spawnPosition;
            float speed = plannedPathSpeed > 0.1f
                ? plannedPathSpeed
                : force / Mathf.Max(0.05f, ResolvePrefabMass(dataToThrow));
            ballScript.FollowPath(pathCopy, speed);

            if (ballRb != null)
            {
                ballRb.angularVelocity = Vector3.zero;
            }
        }
        else if (ballRb != null)
        {
            ballRb.linearVelocity = Vector3.zero;
            ballRb.angularVelocity = Vector3.zero;
            ballRb.AddForce(throwDirection * force, ForceMode.Impulse);

            // Bot egri: rastgele yan kuvvet (Ball Awake bias).
        }

        ClearPlannedPath();

        lastThrowForce = force;
        lastAimPoint = spawnPosition + throwDirection.normalized * 32f;

        if (playFeedback)
        {
            PlayThrowSound(dataToThrow);
            CameraShake.ShakeAll(throwShakeDuration, throwShakeStrength);
            CameraShake.PunchFovAll(throwFovPunch, 0.12f);
            OnBallThrown?.Invoke();
            if (airLift != null)
            {
                airLift.NotifyBallThrown();
            }
        }

        return ball;
    }

    static float ResolvePrefabMass(BallData data)
    {
        if (data == null || data.prefab == null) return 1f;
        Rigidbody rb = data.prefab.GetComponent<Rigidbody>();
        return rb != null ? Mathf.Max(0.05f, rb.mass) : 1f;
    }

    void PlayThrowSound(BallData dataToThrow)
    {
        if (AudioManager.Instance == null)
        {
            return;
        }

        // Topun kendi atış sesi varsa onu çal (örn. Fast Ball whoosh), yoksa varsayılanı.
        if (dataToThrow != null && dataToThrow.throwSfx != null)
        {
            AudioManager.Instance.PlayClip(dataToThrow.throwSfx, AudioManager.Instance.throwVolume);
        }
        else
        {
            AudioManager.Instance.PlayThrow();
        }
    }

    void RotatePlayerToThrowDirection(Vector3 throwDirection)
    {
        Vector3 lookDirection = throwDirection;
        lookDirection.y = 0f;

        if (lookDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection);
        }
    }

    public bool IsCharging()
    {
        return isCharging;
    }

    public float GetChargePercent()
    {
        if (!isCharging)
        {
            return 0f;
        }

        float chargeTime = Time.time - chargeStartTime;
        return Mathf.Clamp01(chargeTime / maxChargeTime);
    }

    // Aim preview / reticle: charge sirasinda gercek atisla ayni origin/dir/force.
    public bool TryGetAimPreview(out Vector3 origin, out Vector3 direction, out float force)
    {
        origin = default;
        direction = default;
        force = 0f;

        if (!isActiveAndEnabled || !isCharging)
        {
            return false;
        }

        if (!GameManager.RoundIsActive)
        {
            return false;
        }

        if (playerHealth != null && playerHealth.IsEliminated)
        {
            return false;
        }

        if (playerRole != null && playerRole.roleType != RoleType.Thrower)
        {
            return false;
        }

        if (throwPoint == null)
        {
            return false;
        }

        direction = GetThrowDirection();
        if (direction.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        force = Mathf.Lerp(minThrowForce, maxThrowForce, GetChargePercent());
        // SpawnAndThrowBall ile ayni offset.
        origin = throwPoint.position + direction * 0.6f;
        return true;
    }
}
