using UnityEngine;

public class TeamControlManager : MonoBehaviour
{
    [Header("Camera")]
    public CameraFollow cameraFollow;
    public Transform mainCameraTransform;

    private GameObject runnerPlayer;
    private GameObject saverPlayer;
    private PlayerHealth runnerHealth;

    void Start()
    {
        // Oyuncular sahne referansi yerine PlayerManager'dan bulunur.
        // Ayni rolde bot da olabilecegi icin insan kontrollu olani tercih ederiz.
        PlayerRole runner = FindHumanControlled(RoleType.Runner);
        PlayerRole saver = FindHumanControlled(RoleType.Saver);

        if (runner != null)
        {
            runnerPlayer = runner.gameObject;
            runnerHealth = runner.Health;
        }

        if (saver != null)
        {
            saverPlayer = saver.gameObject;
        }

        // Runner elendiğinde kontrol Saver'a, kurtarıldığında geri Runner'a geçer.
        if (runnerHealth != null)
        {
            runnerHealth.OnEliminated += SwitchToSaver;
            runnerHealth.OnRevived += SwitchToRunner;
        }

        SwitchToRunner();
    }

    // Bu roldeki oyunculardan girdi bileseni (PlayerInputHandler) olani sec.
    // Hicbiri insan kontrollu degilse ilk bulunan doner.
    PlayerRole FindHumanControlled(RoleType role)
    {
        PlayerRole fallback = null;

        foreach (PlayerRole player in PlayerManager.All)
        {
            if (player.roleType != role) continue;

            if (fallback == null)
            {
                fallback = player;
            }

            if (player.GetComponent<PlayerInputHandler>() != null)
            {
                return player;
            }
        }

        return fallback;
    }

    void OnDestroy()
    {
        if (runnerHealth != null)
        {
            runnerHealth.OnEliminated -= SwitchToSaver;
            runnerHealth.OnRevived -= SwitchToRunner;
        }
    }

    void SwitchToRunner()
    {
        SetControl(runnerPlayer, true);
        SetControl(saverPlayer, false);

        if (cameraFollow != null && runnerPlayer != null)
        {
            cameraFollow.target = runnerPlayer.transform;
        }

        Debug.Log("Kontrol RunnerPlayer'a geçti.");
    }

    void SwitchToSaver()
    {
        SetControl(runnerPlayer, false);
        SetControl(saverPlayer, true);

        if (cameraFollow != null && saverPlayer != null)
        {
            cameraFollow.target = saverPlayer.transform;
        }

        Debug.Log("Runner elendi. Kontrol SaverPlayer'a geçti.");
    }

    void SetControl(GameObject player, bool isControlled)
    {
        if (player == null) return;

        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        PlayerDash dash = player.GetComponent<PlayerDash>();
        PlayerThrow throwScript = player.GetComponent<PlayerThrow>();
        PlayerRevive revive = player.GetComponent<PlayerRevive>();

        if (movement != null)
        {
            movement.enabled = isControlled;

            if (mainCameraTransform != null)
            {
                movement.cameraTransform = mainCameraTransform;
            }
        }

        if (dash != null)
        {
            dash.enabled = isControlled;
        }

        // Thrower rolü olmayan karakterlerin atış yapmasını istemiyoruz.
        if (throwScript != null)
        {
            throwScript.enabled = false;
        }

        // Revive sadece Saver kontrol ediliyorken aktif olsun.
        if (revive != null)
        {
            PlayerRole role = player.GetComponent<PlayerRole>();

            revive.enabled = isControlled &&
                             role != null &&
                             role.roleType == RoleType.Saver;
        }
    }
}
