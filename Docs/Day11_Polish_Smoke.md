# Day 11 — Polish Cleanup + Smoke Rebuild

**Tarih:** 9 Temmuz 2026

## Yapılanlar

### BUG-004 — BallData SFX
- Tüm `BallData_*` asset’lerinde `throwSfx` / `hitSfx` dolu (zaten vardı)
- Çeşitlilik: **Fast** + **Curve** → `SFX_ThrowAlt.mp3`
- Normal / Heavy / Bouncy → `SFX_Throw.mp3`

### BUG-005 — Sahne stale alanlar
- `Yakantop.unity` taranadı: `runnerPlayer` / `saverPlayer` / `runnerTarget` / `saverTarget` **yok**
- Audit notu güncellendi (temiz)

### BUG-006 — Input actions
- Bilinçli unused olarak dokümante: `Docs/InputSystem_Actions_Note.md`

### Windows smoke rebuild
- Menü: **Tools → Windows Smoke Build** (Editor açıkken de çalışır)
- Sonuç: `Succeeded` · errors=0 · warnings=25 · ~125 MB
- Çıktı: `Builds/WindowsSmoke/Yakantop.exe`

## Not
Runner/Saver custom model + Day 10 yetenekler bu build’e dahil.
