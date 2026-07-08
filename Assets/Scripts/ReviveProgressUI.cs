using UnityEngine;
using UnityEngine.UI;

public class ReviveProgressUI : MonoBehaviour
{
    public PlayerRevive playerRevive;
    public Image reviveFillImage;
    public GameObject reviveBarRoot;

    void Update()
    {
        if (playerRevive == null || reviveFillImage == null)
        {
            return;
        }

        float progress = playerRevive.GetReviveProgressPercent();

        reviveFillImage.fillAmount = progress;

        if (reviveBarRoot != null)
        {
            reviveBarRoot.SetActive(playerRevive.IsReviving());
        }
    }
}