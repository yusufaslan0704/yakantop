using UnityEngine;
using UnityEngine.UI;

public class ThrowChargeUI : MonoBehaviour
{
    public PlayerThrow playerThrow;
    public Image chargeFillImage;
    public GameObject chargeBarRoot;

    void Update()
    {
        if (playerThrow == null || chargeFillImage == null)
        {
            return;
        }

        float chargePercent = playerThrow.GetChargePercent();

        chargeFillImage.fillAmount = chargePercent;

        if (chargeBarRoot != null)
        {
            chargeBarRoot.SetActive(playerThrow.IsCharging());
        }
    }
}
