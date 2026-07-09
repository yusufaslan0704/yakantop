# Day 9 — UI / UX Polish

**Tarih:** 9 Temmuz 2026  
**Kapsam:** Ortak renk dili, HUD okunabilirlik, end-game kartı, charge/revive barları, CanvasScaler

---

## Yapılanlar

### 1. `UIColorPalette`
- Ortak koyu lacivert + cyan palet (`Assets/Scripts/UIColorPalette.cs`)
- Lobby / HUD / barlar aynı dili kullanır

### 2. `GameUI` HUD
- TMP outline (arena üstünde okunur)
- Timer: cyan; ≤10s kırmızı; ≤5s pulse
- Skor: `MAÇ` muted + cyan sayılar (rich text)
- Revive countdown: gold / urgency pulse
- Crosshair: yarı saydam + outline
- CanvasScaler: **1920×1080 Scale With Screen Size** (lobby ile uyumlu)

### 3. End-game panel
- Tam ekran dim backdrop
- Merkez kart (`EndGameCard`)
- Eski `RestartText` gizlendi (çelişen metin)
- Kazanan rengi: yeşil / kırmızı
- Alt satır muted talimat (`R` / `L`)

### 4. Charge + Revive barları
- `ThrowChargeUI` / `ReviveProgressUI` Dash bar diline çekildi
- Track + fill + label (`CHARGE` / `REVIVE`)
- Charge: yeşil → sarı (doluluk)
- Revive: cyan

### 5. Lobby
- Backdrop / card / title / CTA / accent → `UIColorPalette`

---

## Play test checklist

1. Lobby koyu kart + yeşil Start — öncekiyle tutarlı mı?  
2. Start → timer üstte okunuyor mu (outline)?  
3. Son 10 sn timer kırmızıya dönüyor mu?  
4. Thrower charge: ortada `CHARGE` barı, yeşil→sarı?  
5. Revive: altta `REVIVE` cyan bar?  
6. Round/maç bitince dim + kart + doğru metin?  
7. Farklı çözünürlükte HUD kaymıyor mu?

---

## Not

Türkçe karakterler TMP LiberationSans ile gelir; outline runtime’da uygulanır.
