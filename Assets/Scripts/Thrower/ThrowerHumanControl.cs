using UnityEngine;
using UnityEngine.InputSystem;

// Gamepad bagliysa ThrowerBot'u kapatip kontrolu 2. oyuncuya verir.
// Gamepad cekilirse bot geri devralir. (Botlar test icin var.)
public class ThrowerHumanControl : MonoBehaviour
{
    private ThrowerBot bot;
    private PlayerMovement movement;
    private PlayerInputHandler inputHandler;
    private PlayerVolley volley;
    private PlayerTrap trap;
    private PlayerAirLift airLift;
    private PlayerThrowerInvis throwerInvis;
    private PlayerThrowFake throwerFake;
    private PlayerThrowerShadow throwerShadow;

    private bool humanActive;
    private bool initialized;
    private bool lobbyOverrideActive;

    void Awake()
    {
        bot = GetComponent<ThrowerBot>();
        movement = GetComponent<PlayerMovement>();
        inputHandler = GetComponent<PlayerInputHandler>();
        volley = GetComponent<PlayerVolley>();
        trap = GetComponent<PlayerTrap>();
        airLift = GetComponent<PlayerAirLift>();
        throwerInvis = GetComponent<PlayerThrowerInvis>();
        throwerFake = GetComponent<PlayerThrowFake>();
        throwerShadow = GetComponent<PlayerThrowerShadow>();
    }

    void Update()
    {
        if (lobbyOverrideActive)
        {
            return;
        }

        bool gamepadConnected = HasGamepadForThisPlayer();

        if (!initialized || gamepadConnected != humanActive)
        {
            initialized = true;
            ApplyControlMode(gamepadConnected);
        }
    }

    bool HasGamepadForThisPlayer()
    {
        int index = inputHandler != null ? inputHandler.gamepadIndex : 0;

        return index >= 0 && index < Gamepad.all.Count;
    }

    void ApplyControlMode(bool human)
    {
        humanActive = human;

        if (bot != null)
        {
            bot.enabled = !human;
        }

        if (movement != null)
        {
            movement.enabled = human;
        }

        if (volley != null)
        {
            volley.enabled = human;
        }

        if (trap != null)
        {
            trap.enabled = human;
        }

        if (airLift != null)
        {
            airLift.enabled = human;
        }

        if (throwerInvis == null)
        {
            throwerInvis = GetComponent<PlayerThrowerInvis>();
        }

        if (throwerInvis != null)
        {
            throwerInvis.enabled = human;
        }

        if (throwerFake == null)
        {
            throwerFake = GetComponent<PlayerThrowFake>();
        }

        if (throwerFake != null)
        {
            throwerFake.enabled = human;
        }

        if (throwerShadow == null)
        {
            throwerShadow = GetComponent<PlayerThrowerShadow>();
        }

        if (throwerShadow != null)
        {
            throwerShadow.enabled = human;
        }

        // Lobiden insan secildiyse gamepad yokken de atis/volley acik kalsin.
        if (inputHandler != null && human)
        {
            bool hasPad = HasGamepadForThisPlayer();
            if (!hasPad)
            {
                inputHandler.scheme = ControlScheme.KeyboardMouse;
                inputHandler.useArrowKeysForMove = true;
            }
            else
            {
                inputHandler.scheme = ControlScheme.Gamepad;
                inputHandler.useArrowKeysForMove = false;
            }
        }

        if (SplitScreenManager.Instance != null)
        {
            SplitScreenManager.Instance.RefreshLayout();
        }

        Debug.Log(human
            ? "Thrower insan kontrolunde."
            : "Thrower bot kontrolunde.");
    }

    // Lobiden gelen zorunlu kontrol modu; gamepad algilamasini devre disi birakir.
    public void SetLobbyOverride(bool active, bool forceHuman)
    {
        lobbyOverrideActive = active;

        if (active)
        {
            initialized = true;
            ApplyControlMode(forceHuman);
        }
        else
        {
            initialized = false;
        }
    }
}
