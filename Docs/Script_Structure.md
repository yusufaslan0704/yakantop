# Script Structure

**Tarih:** 11 Temmuz 2026 · güncelleme: 13 Temmuz 2026

`Assets/Scripts/` artık domain klasörlerine ayrıldı. Unity GUID’ler (`.meta`) korundu — prefab referansları bozulmamalı.

| Klasör | İçerik |
|--------|--------|
| `Core/` | Game loop, lobby, roller, split-screen, loadout |
| `Arena/` | ArenaBuilder, zone limiter, SafeZone, TrapZone |
| `Thrower/` | Atış, toplar UI, bot, yetenekler, registry |
| `Runner/` | Dash/dodge/abilities, decoy, bots, revive |
| `Combat/` | `Ball`, `BallData` |
| `Shared/` | Movement, model/anim, input, kamera, audio |
| `UI/` | HUD, cooldown barlar, palet, emote wheel |
| `VFX/` | Combat VFX, dash ghost/ring |

## ThrowerAbilityRegistry

Özel atıcı yetenekleri tek yerde:

`Assets/Scripts/Thrower/ThrowerAbilityRegistry.cs`

- Yeni yetenek: registry’ye satır + component ekle
- `PlayerThrow` / `BallSelectUI` slot sayısı otomatik uzar
- Geriye uyumlu API: `AirLiftModeSelected`, `FakeSlotIndex`, vb. duruyor

## Thrower altyapı parçaları

| Tip | Dosya |
|-----|-------|
| Fizik formülleri | `ThrowPhysics.cs` |
| Kombo slot state | `ThrowerLoadout.cs` |
| Nişan | `ThrowAimResolver.cs` |
| Release / spawn | `ThrowReleaseController.cs` + `ThrowBallSpawner.cs` |
| Yetenek runtime | `IThrowerAbility.cs` |
| Anim köprüsü | `ThrowAnimationBridge.cs` |
| Detay | `Docs/Thrower_Infrastructure.md` |

## Not
Klasör taşıması sonrası Unity’de bir kez reimport / domain reload beklenir.
