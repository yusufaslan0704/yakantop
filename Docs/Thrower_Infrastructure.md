# Thrower Infrastructure

**Tarih:** 13 Temmuz 2026  
**Odak:** Altyapı düzeni (test gerektirmeyen yapısal temizlik)

## Yapılan ayrıştırmalar

| Tip | Dosya | Rol |
|-----|-------|-----|
| `ThrowPhysics` | `Thrower/ThrowPhysics.cs` | Prefab mass + path speed formülü (preview = atış) |
| `ThrowerLoadout` | `Thrower/ThrowerLoadout.cs` | Sarı/yeşil slot durumu + Select/Cycle |
| `ThrowAimResolver` | `Thrower/ThrowAimResolver.cs` | Camera/body aim + steep-down |
| `ThrowBallSpawner` | `Thrower/ThrowBallSpawner.cs` | Instantiate + path/impulse + feedback |
| `ThrowReleaseController` | `Thrower/ThrowReleaseController.cs` | Cooldown, delay coroutine, spread |
| `IThrowerAbility` | `Thrower/IThrowerAbility.cs` | CD / busy runtime yüzeyi |
| `PlayerThrow` | façade | Charge + path draw; loadout/aim/release + ability cache |
| `IsHumanControlled` | `PlayerThrow` | Tek kaynak; preview bunu kullanır |

## Mimari

```
PlayerThrow (charge / path draw / planned-path sahipliği / gameplay events)
  ├─ ThrowerLoadout
  ├─ ThrowAimResolver
  ├─ IThrowerAbility[] (AirLift, Invis, Fake, Shadow)
  ├─ ThrowAnimationBridge  ← CharacterModelVisual sadece bunu dinler
  ├─ ThrowAimPreview       ← sadece çizer; LateUpdate path yazmaz
  └─ ThrowReleaseController
         └─ ThrowBallSpawner
```

## ThrowAnimationBridge

- `CharacterModelVisual` artık `PlayerThrow` tipine bağlı değil
- Charge / throw anim event’leri bridge üzerinden
- `BallReleaseDelay` gameplay’deki `PlayerThrow.ballReleaseDelay` okur
- Runner/Saver’da bridge yok → throw anim dinlenmez (doğru)

## Planned path sahipliği

- **Sahip:** `PlayerThrow` (`SetPlannedPath` / `TryConsumePlannedPath`)
- **Serbest çizim (curve):** charge sırasında `PublishDrawnPath`, release’te kilit
- **Düz yay:** `ReleaseThrow` → `LockGravityPlannedPath` (preview ile aynı `ThrowPathBuilder`)
- **`ThrowAimPreview`:** LateUpdate yalnızca LineRenderer / impact / hedef ring — `SetPlannedPath` çağırmaz

## Yeni yetenek ekleme

1. `ThrowerAbilityId` + `ThrowerAbilityRegistry.Abilities` satırı  
2. `MonoBehaviour, IThrowerAbility` component (`AbilityId`, `GetCooldownVisual`, `IsBusy`)  
3. `EnsureComponents` içine `EnsureComponent<T>`  
4. `PlayerThrow.RefreshAbilityCache` Awake’te zaten tüm `IThrowerAbility`’leri toplar  

Registry artık CD/busy için `switch` tutmaz — component kendi durumunu bildirir.

## Kural

Slot mantığı `ThrowerLoadout`’ta kalır; `PlayerThrow` public API (`AirLiftModeSelected` vb.) geriye uyumlu kalır. Release `ThrowReleaseController` üzerinden eklenir. Animasyon `ThrowAnimationBridge` üzerinden. Planned path release’te kilitlenir.
