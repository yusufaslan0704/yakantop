using System.Collections.Generic;
using UnityEngine;

// Cep guvenli alan (Pocket Safe Area): icindeki kosucular topla vurulamaz.
// Kamp yapmayi onlemek icin koruma sureli calisir:
// - Iceride durdukca oyuncunun koruma butcesi tukenir.
// - Butce bitince koruma kalkar (top vurabilir), cepten cikinca butce yavasca dolar.
[RequireComponent(typeof(Collider))]
public class SafeZone : MonoBehaviour
{
    [Header("Koruma")]
    [Tooltip("Oyuncu iceride en fazla bu kadar saniye korunur.")]
    public float maxStayDuration = 3f;

    [Tooltip("Cep disindayken koruma hakki saniyede bu kadar geri dolar.")]
    public float rechargeSpeed = 1f;

    [Header("Gorsel (istege bagli)")]
    [Tooltip("Cep zemini: koruma doluyken mavi, tukenince gri olur.")]
    public Renderer floorRenderer;
    public Color chargedColor = new Color(0.16f, 0.45f, 0.80f);
    public Color depletedColor = new Color(0.35f, 0.37f, 0.40f);

    private static readonly List<SafeZone> zones = new List<SafeZone>();

    // Koruma butcesi oyuncu basina tutulur ve tum cepler icin ortaktir
    // (cepten cebe kosarak sureyi sifirlamak mumkun olmasin).
    private static readonly Dictionary<PlayerHealth, float> stayTime = new Dictionary<PlayerHealth, float>();

    private readonly HashSet<PlayerHealth> playersInside = new HashSet<PlayerHealth>();

    void OnEnable()
    {
        zones.Add(this);
    }

    void OnDisable()
    {
        zones.Remove(this);
        playersInside.Clear();
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();

        if (player != null)
        {
            playersInside.Add(player);

            if (!stayTime.ContainsKey(player))
            {
                stayTime[player] = 0f;
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();

        if (player != null)
        {
            playersInside.Remove(player);
        }
    }

    void Update()
    {
        // Iceridekilerin koruma butcesi tukenir.
        foreach (PlayerHealth player in playersInside)
        {
            if (player == null) continue;

            stayTime[player] = Mathf.Min(maxStayDuration, stayTime[player] + Time.deltaTime);
        }

        // Sarj islemini tek bir zone yurutur (cift saymayi onlemek icin).
        if (zones.Count > 0 && zones[0] == this)
        {
            RechargeOutsiders();
        }

        UpdateFloorColor();
    }

    static void RechargeOutsiders()
    {
        if (stayTime.Count == 0) return;

        List<PlayerHealth> keys = new List<PlayerHealth>(stayTime.Keys);

        foreach (PlayerHealth player in keys)
        {
            if (player == null)
            {
                stayTime.Remove(player);
                continue;
            }

            if (IsInsideAnyZone(player)) continue;

            float recharge = 0f;

            foreach (SafeZone zone in zones)
            {
                recharge = Mathf.Max(recharge, zone.rechargeSpeed);
            }

            stayTime[player] = Mathf.Max(0f, stayTime[player] - recharge * Time.deltaTime);
        }
    }

    static bool IsInsideAnyZone(PlayerHealth player)
    {
        foreach (SafeZone zone in zones)
        {
            if (zone.playersInside.Contains(player)) return true;
        }

        return false;
    }

    void UpdateFloorColor()
    {
        if (floorRenderer == null) return;

        // Iceride butcesi tukenmis biri varsa gri, yoksa mavi.
        bool anyDepleted = false;

        foreach (PlayerHealth player in playersInside)
        {
            if (player != null && stayTime.TryGetValue(player, out float time) && time >= maxStayDuration)
            {
                anyDepleted = true;
                break;
            }
        }

        Color target = anyDepleted ? depletedColor : chargedColor;

        floorRenderer.material.color = Color.Lerp(floorRenderer.material.color, target, 6f * Time.deltaTime);
    }

    // Ball.cs carpmada bunu sorar: oyuncu su an korunuyor mu?
    public static bool IsProtected(PlayerHealth player)
    {
        if (player == null) return false;

        foreach (SafeZone zone in zones)
        {
            if (!zone.playersInside.Contains(player)) continue;

            if (stayTime.TryGetValue(player, out float time) && time < zone.maxStayDuration)
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsInsideAny(PlayerHealth player)
    {
        return IsInsideAnyZone(player);
    }

    // Kalan koruma suresi (saniye). Cep disinda da butce bilgisini verir.
    public static float GetProtectionRemaining(PlayerHealth player)
    {
        if (player == null) return 0f;

        float maxDuration = GetMaxStayDuration();
        if (maxDuration <= 0f) return 0f;

        float used = 0f;
        stayTime.TryGetValue(player, out used);

        return Mathf.Max(0f, maxDuration - used);
    }

    // 1 = butce dolu, 0 = tukenmis.
    public static float GetProtectionPercent(PlayerHealth player)
    {
        float maxDuration = GetMaxStayDuration();
        if (maxDuration <= 0f) return 0f;

        return Mathf.Clamp01(GetProtectionRemaining(player) / maxDuration);
    }

    // Botlar icin: en yakin cep merkezi (yoksa false).
    public static bool TryGetNearestCenter(Vector3 from, out Vector3 center)
    {
        center = from;
        float bestSqr = float.MaxValue;
        bool found = false;

        foreach (SafeZone zone in zones)
        {
            if (zone == null) continue;

            Vector3 zoneCenter = zone.transform.position;
            float sqr = (zoneCenter - from).sqrMagnitude;

            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                center = zoneCenter;
                found = true;
            }
        }

        return found;
    }

    static float GetMaxStayDuration()
    {
        float maxDuration = 0f;

        foreach (SafeZone zone in zones)
        {
            if (zone != null)
            {
                maxDuration = Mathf.Max(maxDuration, zone.maxStayDuration);
            }
        }

        return maxDuration;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        zones.Clear();
        stayTime.Clear();
    }
}
