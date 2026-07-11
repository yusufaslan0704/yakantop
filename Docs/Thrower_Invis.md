# Thrower Görünmez (8)

**Tarih:** 11 Temmuz 2026

## Kullanım
1. Liste → **8 / Gorunmez** seç (sarı veya yeşil combo)
2. **V** (gamepad: sol stick click) → 4 sn görünmez
3. Kendi kameranda ghost; kaçan kamerasında tamamen yok
4. Top atınca görünmezlik biter

Kombo örneği: **8 + 4 Sekten** → görünmez ol, seken top at.

## Notlar
- CD: 12 sn
- Layer: `InvisibleToRunner` (kaçan kamerası cull)

## Dosyalar
- `PlayerThrowerInvis.cs`
- `PlayerThrow` — InvisSlotIndex / InvisModeSelected
- `BallSelectUI` — 8. satır
- `SplitScreenManager.RefreshRunnerCulling`
