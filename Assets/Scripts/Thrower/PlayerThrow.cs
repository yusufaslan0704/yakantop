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
    [Tooltip("Yanlis tiklamada cansiz fırlatmayi keser; guc en az bu sureye gore hesaplanir.")]
    public float minChargeTime = 0.1f;
    [Tooltip("Zaman -> guc. Bos ise Awake'de ease-in egrisi kurulur.")]
    public AnimationCurve chargeCurve;

    [Header("Throw Cooldown")]
    public float cooldown = 0.42f;

    [Header("Throw Feel")]
    public float throwShakeDuration = 0.06f;
    public float throwShakeStrength = 0.07f;
    public float throwFovPunch = 2.8f;
    [Tooltip("Charge sirasinda FOV daralmasi (derece).")]
    public float chargeFovPull = 3.2f;

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

    // Charge basladi (windup); basili tutma.
    public event System.Action OnChargeStarted;
    // Charge iptal (atis yok).
    public event System.Action OnChargeCancelled;
    // Animasyon baslar baslamaz; top henuz cikmamistir.
    public event System.Action OnThrowStarted;
    // Top gercekten spawn oldugunda.
    public event System.Action OnBallThrown;
    public event System.Action OnBallTypeChanged;

    private float chargeStartTime;
    private bool isCharging = false;
    private bool fullChargeBlipPlayed;
    private float lastReleasedChargePercent;
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

    float fakeLockUntil;
    float lastThrowForce = 28f;
    Vector3 lastAimPoint;
    readonly ThrowerLoadout loadout = new ThrowerLoadout();
    readonly ThrowAimResolver aimResolver = new ThrowAimResolver();
    ThrowReleaseController release;

    public int SelectedBallIndex => selectedBallIndex;
    public int PrimarySlotIndex => loadout.PrimarySlotIndex;
    public int ComboSlotIndex => loadout.ComboSlotIndex;

    int BallCount => ballTypes != null ? ballTypes.Length : 0;

    public int AirLiftSlotIndex => ThrowerAbilityRegistry.SlotFor(BallCount, ThrowerAbilityId.AirLift);
    public int InvisSlotIndex => ThrowerAbilityRegistry.SlotFor(BallCount, ThrowerAbilityId.Invis);
    public int FakeSlotIndex => ThrowerAbilityRegistry.SlotFor(BallCount, ThrowerAbilityId.Fake);
    public int ShadowSlotIndex => ThrowerAbilityRegistry.SlotFor(BallCount, ThrowerAbilityId.Shadow);

    public bool AirLiftModeSelected => loadout.IsAbilitySelected(BallCount, ThrowerAbilityId.AirLift);
    public bool InvisModeSelected => loadout.IsAbilitySelected(BallCount, ThrowerAbilityId.Invis);
    public bool FakeModeSelected => loadout.IsAbilitySelected(BallCount, ThrowerAbilityId.Fake);
    public bool ShadowModeSelected => loadout.IsAbilitySelected(BallCount, ThrowerAbilityId.Shadow);

    // UI geriye uyumluluk: son vurgulanan slot.
    public int SelectedSlotIndex => loadout.SelectedSlotIndex;
    public int SelectableSlotCount => BallCount + ThrowerAbilityRegistry.Count;
    public BallData[] AvailableBallTypes => ballTypes;
    public bool IsDrawingThrowPath => isCharging && drawingPath;
    public bool IsThrowBusy =>
        (release != null && release.IsBusy) || Time.time < fakeLockUntil;
    public bool IsSpawningEchoBall { get; private set; }
    public IReadOnlyList<Vector3> DrawnPathPoints => drawnPath;
    public ThrowerLoadout Loadout => loadout;

    void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerRole = GetComponent<PlayerRole>();
        inputHandler = GetComponent<PlayerInputHandler>();
        throwerBot = GetComponent<ThrowerBot>();
        airLift = GetComponent<PlayerAirLift>();

        ThrowerAbilityRegistry.EnsureComponents(gameObject);
        airLift = GetComponent<PlayerAirLift>();
        release = GetComponent<ThrowReleaseController>();
        if (release == null)
        {
            release = gameObject.AddComponent<ThrowReleaseController>();
        }

        release.Bind(this);

        EnsureChargeCurve();
        EnsureBallTypes();
        SyncSelectedFromBallData();
    }

    void EnsureChargeCurve()
    {
        if (chargeCurve != null && chargeCurve.length > 0)
        {
            return;
        }

        // Yavas basla, sonlarda dolgun his.
        chargeCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 0.35f),
            new Keyframe(0.4f, 0.25f, 0.55f, 0.55f),
            new Keyframe(0.8f, 0.72f, 1.1f, 1.1f),
            new Keyframe(1f, 1f, 0.15f, 0f));
    }

    public bool IsAbilitySelected(ThrowerAbilityId id)
    {
        return loadout.IsAbilitySelected(BallCount, id);
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

    public bool IsHumanControlled =>
        inputHandler != null && inputHandler.enabled &&
        (throwerBot == null || !throwerBot.enabled);

    bool IsHumanThrower() => IsHumanControlled;

    void SyncSelectedFromBallData()
    {
        selectedBallIndex = 0;
        loadout.Clear();

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
    public void SelectSlot(int slot)
    {
        EnsureBallTypes();
        if (!loadout.SelectSlot(slot, BallCount, SelectableSlotCount, ballTypes))
        {
            return;
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
        return loadout.GetActiveBallSlot(BallCount);
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

        CancelCharge();
        CancelPendingThrow();
        fakeLockUntil = Time.time + Mathf.Max(0.15f, lockDuration);
        OnThrowStarted?.Invoke();
        return true;
    }

    public void CycleBall(int delta)
    {
        EnsureBallTypes();
        if (!loadout.CycleBallSlots(delta, BallCount, ballTypes))
        {
            return;
        }

        SyncBallFromSlots();
        OnBallTypeChanged?.Invoke();
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
            CancelCharge();
            CancelPendingThrow();
            return;
        }

        // Elenen oyuncu top atamaz.
        if (playerHealth != null && playerHealth.IsEliminated)
        {
            CancelCharge();
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
            CancelCharge();
            return;
        }

        // Cooldown / devam eden atis / fake kilidi bitmeden yeni atis hazirlanamaz.
        if ((release != null && release.IsBusy) || Time.time < fakeLockUntil)
        {
            return;
        }

        // Yuksek atis modu: once sag tikle yuksel, birak, sonra sol tik.
        if (airLift != null && airLift.BlocksGroundThrow())
        {
            CancelCharge();
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
            TickChargeFeel();
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
        fullChargeBlipPlayed = false;
        BeginPathDrawIfNeeded();
        OnChargeStarted?.Invoke();
    }

    void CancelCharge()
    {
        if (!isCharging)
        {
            return;
        }

        isCharging = false;
        drawingPath = false;
        ClearPlannedPath();
        CameraShake.SetChargeFovPullAll(0f);
        OnChargeCancelled?.Invoke();
    }

    void TickChargeFeel()
    {
        float charge = GetChargePercent();
        CameraShake.SetChargeFovPullAll(chargeFovPull * charge);

        if (!fullChargeBlipPlayed && charge >= 0.95f)
        {
            fullChargeBlipPlayed = true;
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayThrowChargeReady();
            }
        }
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
        float speed = ThrowPhysics.PathSpeedFromForce(ForceFromChargePercent(GetChargePercent()), ballData);
        SetPlannedPath(drawnPath, speed);
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
        float chargeTime = Mathf.Max(Time.time - chargeStartTime, minChargeTime);
        float chargePercent = EvaluateCharge(chargeTime);
        lastReleasedChargePercent = chargePercent;

        isCharging = false;
        CameraShake.SetChargeFovPullAll(0f);

        // Cizim yolunu atistan once kilitle.
        if (drawingPath && drawnPath.Count >= 2)
        {
            float speed = ThrowPhysics.PathSpeedFromForce(
                ForceFromChargePercent(chargePercent), ResolveThrowBallData());
            SetPlannedPath(drawnPath, speed);
        }

        drawingPath = false;

        float finalForce = ForceFromChargePercent(chargePercent);
        BeginThrow(ResolveThrowBallData(), GetThrowDirection(), finalForce, rotateToAim: true);
    }

    float EvaluateCharge(float chargeTime)
    {
        EnsureChargeCurve();
        float t = Mathf.Clamp01(chargeTime / Mathf.Max(0.05f, maxChargeTime));
        if (chargeCurve != null && chargeCurve.length > 0)
        {
            return Mathf.Clamp01(chargeCurve.Evaluate(t));
        }

        return t;
    }

    float ForceFromChargePercent(float chargePercent)
    {
        return Mathf.Lerp(minThrowForce, maxThrowForce, Mathf.Clamp01(chargePercent));
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
        if (release != null && release.IsBusy)
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

    public void RotateToThrowDirection(Vector3 throwDirection)
    {
        RotatePlayerToThrowDirection(throwDirection);
    }

    public void NotifyThrowStarted() => OnThrowStarted?.Invoke();
    public void NotifyBallThrown() => OnBallThrown?.Invoke();
    public void NotifyChargeCancelled() => OnChargeCancelled?.Invoke();

    public void RecordThrowStats(float force, Vector3 aimPoint)
    {
        lastThrowForce = force;
        lastAimPoint = aimPoint;
    }

    public bool TryConsumePlannedPath(out Vector3[] path, out float speed)
    {
        path = null;
        speed = 0f;
        if (plannedPath == null || plannedPath.Length < 2)
        {
            return false;
        }

        path = plannedPath;
        speed = plannedPathSpeed;
        ClearPlannedPath();
        return true;
    }

    public void SetReleasedChargePercent(float percent)
    {
        lastReleasedChargePercent = Mathf.Clamp01(percent);
    }

    GameObject SpawnAndThrowBall(
        BallData dataToThrow,
        Vector3 throwDirection,
        float force,
        bool playFeedback,
        Vector3 spawnLateralOffset)
    {
        if (release == null)
        {
            return null;
        }

        return release.SpawnBall(dataToThrow, throwDirection, force, playFeedback, spawnLateralOffset);
    }

    void BeginThrow(BallData dataToThrow, Vector3 throwDirection, float force, bool rotateToAim)
    {
        if (release == null)
        {
            OnChargeCancelled?.Invoke();
            return;
        }

        release.BeginThrow(dataToThrow, throwDirection, force, rotateToAim);
    }

    void CancelPendingThrow()
    {
        if (release != null)
        {
            release.CancelPending();
        }
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
        if (release == null || release.IsBusy) return false;
        if (dataToThrow == null || dataToThrow.prefab == null || throwPoint == null) return false;

        Vector3 throwDirection = GetThrowDirection();
        if (throwDirection == Vector3.zero) return false;

        if (loft > 0f)
        {
            throwDirection = (throwDirection + Vector3.up * loft).normalized;
        }

        RotatePlayerToThrowDirection(throwDirection);
        CancelCharge();
        lastReleasedChargePercent = 0.7f;
        return release.TryBeginAbilityThrow(dataToThrow, throwDirection, force, configureBall, 0.35f);
    }

    void OnDisable()
    {
        CancelPendingThrow();
        CancelCharge();
    }

    Vector3 GetThrowDirection()
    {
        SyncAimContext();
        return aimResolver.GetThrowDirection();
    }

    void RotatePlayerToThrowDirection(Vector3 throwDirection)
    {
        SyncAimContext();
        aimResolver.RotateBodyToThrowDirection(throwDirection);
    }

    void SyncAimContext()
    {
        aimResolver.SetContext(new ThrowAimResolver.Context
        {
            Body = transform,
            ThrowPoint = throwPoint,
            PlayerCamera = playerCamera,
            Input = inputHandler,
            AirLift = airLift,
            AimDistance = aimDistance
        });
    }

    public bool IsCharging()
    {
        return isCharging;
    }

    // Yetenek: nisan etrafinda yayilan coklu top (orn. 20° / 3 top).
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
        if (release == null || release.IsBusy) return false;

        BallData dataToThrow = overrideBall != null ? overrideBall : ballData;
        if (dataToThrow == null || dataToThrow.prefab == null || throwPoint == null) return false;

        Vector3 centerDir = GetThrowDirection();
        if (centerDir == Vector3.zero) return false;

        RotatePlayerToThrowDirection(centerDir);
        CancelCharge();
        lastReleasedChargePercent = 0.7f;
        return release.TryBeginSpread(dataToThrow, centerDir, force, totalSpreadDegrees, count, loft);
    }

    public float GetChargePercent()
    {
        if (!isCharging)
        {
            return 0f;
        }

        return EvaluateCharge(Time.time - chargeStartTime);
    }

    public float LastReleasedChargePercent => lastReleasedChargePercent;

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

        force = ForceFromChargePercent(GetChargePercent());
        // SpawnAndThrowBall ile ayni offset.
        origin = throwPoint.position + direction * 0.6f;
        return true;
    }
}
