using UnityEngine;
using UnityEngine.InputSystem;

// Gamepad bagliysa ThrowerBot'u kapatip kontrolu 2. oyuncuya verir.
// Gamepad cekilirse bot geri devralir. (Botlar test icin var.)
public class ThrowerHumanControl : MonoBehaviour
{
    private ThrowerBot bot;
    private PlayerMovement movement;
    private PlayerInputHandler inputHandler;

    private bool humanActive;
    private bool initialized;
    private bool lobbyOverrideActive;

    void Awake()
    {
        bot = GetComponent<ThrowerBot>();
        movement = GetComponent<PlayerMovement>();
        inputHandler = GetComponent<PlayerInputHandler>();
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

        if (SplitScreenManager.Instance != null)
        {
            SplitScreenManager.Instance.RefreshLayout();
        }

        Debug.Log(human
            ? "Gamepad baglandi: Thrower artik 2. oyuncunun kontrolunde."
            : "Gamepad yok: Thrower bot kontrolunde.");
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
