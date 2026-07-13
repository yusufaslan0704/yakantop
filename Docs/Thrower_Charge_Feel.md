# Thrower Charge Feel

**Tarih:** 13 Temmuz 2026  
**Odak:** Görsellik / oynanış / hissiyat — yeni özellik yok

## Model

**Basılı tut = güç topla (windup), bırak = atış.** Girdi aynı (LMB / RT).

| Faz | Ne olur |
|-----|---------|
| Charge start | Throw windup anim, FOV pull-in, bar/reticle |
| Hold | Güç `chargeCurve` ile dolar; full’de blip + UI snap |
| Release | Windup’tan release frame; top `ballReleaseDelay` sonra; shake/FOV charge’a göre |

## Güç

- `maxChargeTime` **0.95s**, force **16–42** (`PF_Thrower` senkron)
- `minChargeTime` **0.1s** — yanlış tıkta cansız fırlatmayı keser
- `AnimationCurve chargeCurve` — yavaş başlar, sonda dolgun
- Preview force = release force (`GetChargePercent` / `ForceFromChargePercent`)

## Dosyalar

- `PlayerThrow.cs` — curve, events, scaled punch, FOV pull tick
- `CharacterModelVisual.cs` — windup hold → release sync
- `CameraShake.cs` — `SetChargeFovPullAll`
- `ThrowChargeUI` / `ThrowerReticleUI` — full charge flash
- `AudioManager.PlayThrowChargeReady` — mevcut throw SFX, tiz/kısa
- `PF_Thrower.prefab` — Day 8 tunables

## Play doğrulama

1. Kısa tap vs full charge — güç ve punch farkı net
2. Hold’da karakter windup pozunda; bırakınca top zamanında çıkar
3. Aim arc gücü gerçek atışla uyumlu
4. Fake (Q) ve AirLift bozulmamış
5. Full charge’da bar/reticle flash + kısa blip
