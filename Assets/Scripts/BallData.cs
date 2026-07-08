using UnityEngine;

// Bir top çeşidinin tüm ayarları tek bir asset'te toplanır.
// Yeni top tipi eklemek için kod değişikliği gerekmez:
// Project panelinde sağ tık > Create > Dodgeball > Ball Data.
[CreateAssetMenu(fileName = "BallData_", menuName = "Dodgeball/Ball Data")]
public class BallData : ScriptableObject
{
    public string ballName = "Yeni Top";
    public GameObject prefab;

    [Header("Throw")]
    public float throwForce = 28f;
    public float cooldownMultiplier = 1f;

    [Header("Selection")]
    [Range(0f, 100f)] public float chance = 33f;
}
