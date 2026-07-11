using UnityEngine;

// Atıcı 7. özellik: sag tik basili tut = yuksel / hover.
// Birakinca sol tik ile her zaman Fast top atar; atistan sonra iner.
[DefaultExecutionOrder(40)]
[RequireComponent(typeof(Rigidbody))]
public class PlayerAirLift : MonoBehaviour
{
    public enum LiftState
    {
        Idle,
        Rising,
        Hovering,
        Descending
    }

    [Header("Lift")]
    public float riseHeight = 4.2f;
    public float riseSpeed = 7f;
    public float descendSpeed = 9f;
    public float hoverTimeout = 10f;

    [Header("Ball")]
    [Tooltip("Bu modda atilan top. Bos ise Fast BallData aranir.")]
    public BallData fastBallData;

    public LiftState State { get; private set; } = LiftState.Idle;
    public bool IsElevated => State == LiftState.Rising || State == LiftState.Hovering || State == LiftState.Descending;
    public bool IsArmedForThrow => State == LiftState.Hovering && armed;
    public bool ForcesFastBall => IsElevated || armed;

    Rigidbody rb;
    PlayerHealth playerHealth;
    PlayerInputHandler inputHandler;
    PlayerRole playerRole;
    PlayerThrow playerThrow;

    float groundY;
    float hoverY;
    float hoverArmTime;
    bool armed;
    bool wasGravity = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerHealth = GetComponent<PlayerHealth>();
        inputHandler = GetComponent<PlayerInputHandler>();
        playerRole = GetComponent<PlayerRole>();
        playerThrow = GetComponent<PlayerThrow>();
    }

    void Update()
    {
        if (!CanOperate())
        {
            if (State != LiftState.Idle)
            {
                ForceLand();
            }

            return;
        }

        bool modeSelected = playerThrow != null && playerThrow.AirLiftModeSelected;
        if (!modeSelected)
        {
            if (State != LiftState.Idle)
            {
                BeginDescend();
            }

            return;
        }

        bool held = inputHandler != null && inputHandler.VolleyHeld;
        bool released = inputHandler != null && inputHandler.VolleyReleased;

        if (State == LiftState.Idle)
        {
            if (held)
            {
                BeginRise();
            }

            return;
        }

        if (State == LiftState.Rising)
        {
            // Basili tutarken yukselir; birakilirsa hover'a gecer (atis hazir).
            if (released || !held)
            {
                EnterHover();
            }

            return;
        }

        if (State == LiftState.Hovering)
        {
            // Hover'da tekrar sag tik = iptal inis.
            if (inputHandler != null && inputHandler.VolleyPressed)
            {
                BeginDescend();
                return;
            }

            if (hoverTimeout > 0f && Time.time >= hoverArmTime + hoverTimeout)
            {
                BeginDescend();
            }
        }
    }

    void FixedUpdate()
    {
        if (State == LiftState.Idle || rb == null || rb.isKinematic)
        {
            return;
        }

        Vector3 velocity = rb.linearVelocity;
        Vector3 position = rb.position;

        if (State == LiftState.Rising)
        {
            float dy = hoverY - position.y;
            if (dy <= 0.05f)
            {
                // Zirvede bekle; silahlanma sadece sag tik birakilinca.
                position.y = hoverY;
                velocity.y = 0f;
                rb.position = position;
                rb.linearVelocity = velocity;
                return;
            }

            velocity.y = riseSpeed;
            rb.linearVelocity = velocity;
            return;
        }

        if (State == LiftState.Hovering)
        {
            position.y = hoverY;
            velocity.y = 0f;
            rb.position = position;
            rb.linearVelocity = velocity;
            return;
        }

        if (State == LiftState.Descending)
        {
            float dy = position.y - groundY;
            if (dy <= 0.08f)
            {
                Land();
                return;
            }

            velocity.y = -descendSpeed;
            rb.linearVelocity = velocity;
        }
    }

    bool CanOperate()
    {
        if (!isActiveAndEnabled) return false;
        if (!GameManager.RoundIsActive) return false;
        if (playerHealth != null && playerHealth.IsEliminated) return false;
        if (playerRole != null && playerRole.roleType != RoleType.Thrower) return false;
        if (EmoteWheelUI.IsOpen) return false;
        return true;
    }

    void BeginRise()
    {
        groundY = transform.position.y;
        hoverY = groundY + riseHeight;
        armed = false;
        State = LiftState.Rising;

        if (rb != null)
        {
            wasGravity = rb.useGravity;
            rb.useGravity = false;
            Vector3 velocity = rb.linearVelocity;
            velocity.y = riseSpeed;
            rb.linearVelocity = velocity;
        }
    }

    void EnterHover()
    {
        State = LiftState.Hovering;
        armed = true;
        hoverArmTime = Time.time;
        hoverY = Mathf.Max(transform.position.y, groundY + 0.5f);

        if (rb != null)
        {
            rb.useGravity = false;
            Vector3 velocity = rb.linearVelocity;
            velocity.y = 0f;
            rb.linearVelocity = velocity;
            Vector3 position = rb.position;
            position.y = hoverY;
            rb.position = position;
        }
    }

    void BeginDescend()
    {
        armed = false;
        State = LiftState.Descending;

        if (rb != null)
        {
            rb.useGravity = false;
            Vector3 velocity = rb.linearVelocity;
            velocity.y = -descendSpeed;
            rb.linearVelocity = velocity;
        }
    }

    void Land()
    {
        armed = false;
        State = LiftState.Idle;

        if (rb != null)
        {
            Vector3 position = rb.position;
            position.y = groundY;
            rb.position = position;

            Vector3 velocity = rb.linearVelocity;
            velocity.y = 0f;
            rb.linearVelocity = velocity;
            rb.useGravity = wasGravity;
        }
    }

    void ForceLand()
    {
        armed = false;
        State = LiftState.Idle;

        if (rb != null)
        {
            rb.useGravity = wasGravity;
            Vector3 velocity = rb.linearVelocity;
            velocity.y = 0f;
            rb.linearVelocity = velocity;
        }
    }

    // Atis tamamlaninca cagirilir.
    public void NotifyBallThrown()
    {
        if (State == LiftState.Rising || State == LiftState.Hovering)
        {
            BeginDescend();
        }
    }

    public BallData ResolveFastBall()
    {
        if (fastBallData != null)
        {
            return fastBallData;
        }

        BallData[] all = Resources.FindObjectsOfTypeAll<BallData>();
        for (int i = 0; i < all.Length; i++)
        {
            BallData d = all[i];
            if (d == null) continue;
            if (d.name.IndexOf("Fast", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                (!string.IsNullOrEmpty(d.ballName) &&
                 d.ballName.IndexOf("Fast", System.StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return d;
            }
        }

        return null;
    }

    // Yukselirken / hover hazir degilken sol tik atisini engelle.
    public bool BlocksGroundThrow()
    {
        if (playerThrow == null || !playerThrow.AirLiftModeSelected)
        {
            return false;
        }

        return State == LiftState.Idle || State == LiftState.Rising || State == LiftState.Descending;
    }
}
