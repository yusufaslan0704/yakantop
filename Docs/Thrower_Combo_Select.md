# Thrower Combo Select

**Tarih:** 11 Temmuz 2026 · güncelleme: Combo UI polish

## Çift seçim
Atıcı en fazla **2** özellik seçer:

| Sıra | Çerçeve | Anlam |
|------|---------|--------|
| 1. basış | **Sarı** | Primary |
| 2. basış | **Yeşil** | Combo |

Örnek: **7 (Yüksek)** sarı → **4 (Sekten)** yeşil  
→ Sağ tıkla yüksel, sol tıkla **Sekten** top at.

**8 Görünmez:** seç + **V** → kaçan seni görmez (atışta biter).  
**9 Fake:** seç + **Q** → sahte atış, dodge bait.  
**0 Golge:** seç + **X** → blink + gölgeden çapraz atış.

- Sadece Yüksek: yüksel + Fast
- Aynı tuşa tekrar: seçimi kaldır / promote
- 3. farklı tuş: yeşil slotu değiştirir
- Varsayılan top seçimi yok (boş kombo ile başlar)

## Combo UI polish
Sol listedeki yetenek satırları:

| Gösterge | Anlam |
|----------|--------|
| Sağ ipucu (RM / V / Q / X) | Aktive tuşu; seçiliyken vurgulu |
| Satır dolgu | CD (hazır = dolu); aktifken pulse |
| Alt durum satırı | `Sari + Yesil \| Volley: RM` veya Yüksek seçiliyken `RM tut=yuksel (Volley kapali)` |

**Volley vs Yüksek:** Yüksek (7) seçiliyken Volley RMB kapalı. Alt durum satırı + alttaki Volley barı `AIRLIFT / —` gösterir.

## Dosyalar
- `ThrowerAbilityRegistry.cs` — yetenek listesi, CD/status helpers
- `PlayerThrow.SelectSlot` — primary / combo
- `BallSelectUI` — sarı/yeşil Outline, activate hint, CD fill, status
- `AbilityCooldownUI` — Volley barı Yüksek’te dim
- `ResolveThrowBallData` — combo topu (yoksa Fast)
