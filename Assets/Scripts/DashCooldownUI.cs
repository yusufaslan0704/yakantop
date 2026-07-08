using UnityEngine;
using UnityEngine.UI;

public class DashCooldownUI : MonoBehaviour
{
    public PlayerDash playerDash;
    public Image dashFillImage;

    void Update()
    {
        if (playerDash == null || dashFillImage == null)
        {
            return;
        }

        dashFillImage.fillAmount = playerDash.GetDashCooldownPercent();
    }
}