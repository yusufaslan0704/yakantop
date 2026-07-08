using UnityEngine;

// Bir top çeşidinin tüm ayarları tek bir asset'te toplanır.
// Yeni top tipi eklemek için kod değişikliği gerekmez:
// Project panelinde sağ tık > Create > Dodgeball > Ball Data.
//
// Oyun vizyonu: her top tipi sadece sayısal değil, hissiyat olarak da
// farklı olmalı. Oyuncu topu gördüğü/duyduğu anda tanıyabilmeli.
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

    [Header("Vuruş Hissi")]
    [Tooltip("Top bir oyuncuya çarptığında kamera sarsıntısı.")]
    public float hitShakeDuration = 0.2f;
    public float hitShakeStrength = 0.25f;

    [Tooltip("Çarpılan oyuncunun savrulma gücü.")]
    public float knockbackForce = 4f;

    [Tooltip("Çarpma anında oyunun kısacık donma süresi (saniye). 0 = kapalı.")]
    public float hitStopDuration = 0.05f;

    [Header("Sesler (boş bırakılırsa varsayılan çalar)")]
    public AudioClip throwSfx;
    public AudioClip hitSfx;
}
