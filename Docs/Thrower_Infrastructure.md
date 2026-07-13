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
| `PlayerThrow` | façade | Charge + path draw; loadout/aim/release’e delege |
| `IsHumanControlled` | `PlayerThrow` | Tek kaynak; preview bunu kullanır |

## Mimari

```
PlayerThrow (charge / path draw / events)
  ├─ ThrowerLoadout
  ├─ ThrowAimResolver
  └─ ThrowReleaseController
         └─ ThrowBallSpawner
```

## Bilinçli olarak dokunulmayanlar (sonraki dilimler)

1. **`IThrowerAbility`** — Registry CD/busy switch’lerini component interface’e taşı
2. Planned-path sahipliği — `ThrowAimPreview.SetPlannedPath` LateUpdate mutasyonu (Play ile doğrula)
3. `CharacterModelVisual` throw windup → ince `ThrowAnimationBridge`

## Kural

Yeni yetenek: `ThrowerAbilityRegistry` + component. Slot mantığı `ThrowerLoadout`’ta kalır; `PlayerThrow` public API (`AirLiftModeSelected` vb.) geriye uyumlu kalır. Release `ThrowReleaseController` üzerinden eklenir.
