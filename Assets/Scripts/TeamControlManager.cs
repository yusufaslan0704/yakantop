using UnityEngine;

public class TeamControlManager : MonoBehaviour
{
    [Header("Players")]
    public GameObject runnerPlayer;
    public GameObject saverPlayer;

    [Header("Camera")]
    public CameraFollow cameraFollow;
    public Transform mainCameraTransform;

    private PlayerHealth runnerHealth;
    private PlayerHealth saverHealth;

    private bool controllingSaver = false;

    void Start()
    {
        if (runnerPlayer != null)
        {
            runnerHealth = runnerPlayer.GetComponent<PlayerHealth>();
        }

        if (saverPlayer != null)
        {
            saverHealth = saverPlayer.GetComponent<PlayerHealth>();
        }

        SwitchToRunner();
    }

    void Update()
    {
        if (runnerHealth == null || saverHealth == null) return;

        // Runner elendiyse kontrol Saver'a geçsin.
        if (runnerHealth.isEliminated && !controllingSaver)
        {
            SwitchToSaver();
        }

        // Runner revive olduysa kontrol tekrar Runner'a dönsün.
        if (!runnerHealth.isEliminated && controllingSaver)
        {
            SwitchToRunner();
        }
    }

    void SwitchToRunner()
    {
        controllingSaver = false;

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
        controllingSaver = true;

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