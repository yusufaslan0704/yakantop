# Ball Select UI — Atıcı top türü seçimi

**Tarih:** 10 Temmuz 2026

## Ne değişti?
İnsan atıcı artık rastgele top atmaz. Solda dikey listeden sıradaki top türünü seçer.

## Kontroller
| Girdi | Etki |
|-------|------|
| **1–6** | Top seç (sarı / yeşil kombo) |
| **7–9 / 0** | Yetenek seç (Yüksek / Görünmez / Fake / Gölge) |
| **Mouse scroll** | Önceki / sonraki |
| **D-Pad ↑↓** (gamepad) | Önceki / sonraki |
| **Tık** (imleç serbestken) | Satıra tıkla |

Yetenek satırlarında sağda aktive tuşu (RM / V / Q / X), satırda CD dolgusu; listenin altında durum satırı. Ayrıntı: `Docs/Thrower_Combo_Select.md`.

Atıcıda 1–4 emote hotkey kapalı (emote sadece **T** çemberi). Bot atıcı hâlâ `chance` ile rastgele seçer.

## Eğri top — serbest yol çizimi
1. **Egri** seç
2. Sol tık / RT **basılı tut**
3. Mouse hareket ettir → yol **senden dışarı** çizilir (sağa-sola = zigzag, yukarı-aşağı = yükseklik)
4. Bırak → top **çizdiğin izi** takip eder

Charge sırasında kamera bakışı kilitlenir; mouse sadece yolu çizer. Gamepad: sağ stick.

Diğer toplarda hâlâ gravity yay önizlemesi + o izi takip.

## Dosyalar
- `PlayerThrow.cs` — serbest path draw + planned path
- `ThrowAimPreview.cs` — çizilen izi gösterir
- `Ball.cs` — `FollowPath`
- `CameraFollow.cs` — çizim sırasında look kilidi
- `ThrowPathBuilder.cs` — düz yay (diğer toplar)
- `BallSelectUI.cs` — sol dikey liste

## Test
1. Egri seç → basılı tut → mouse ile zigzag çiz → bırak
2. Top çizdiğin yoldan gidiyor mu?
3. Normal top → eski yay önizlemesi duruyor mu?
