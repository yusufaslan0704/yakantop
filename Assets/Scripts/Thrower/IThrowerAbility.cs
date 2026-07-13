using UnityEngine;

// Atıcı özel yetenek runtime yüzeyi.
// Yeni yetenek: bu interface'i uygula + ThrowerAbilityRegistry metadata satırı.
public interface IThrowerAbility
{
    ThrowerAbilityId AbilityId { get; }

    // 0 = CD'de, 1 = hazir. Aktif/busy iken genelde 1.
    float GetCooldownVisual();

    // UI pulse: yetenek su an etkinken true.
    bool IsBusy { get; }
}
