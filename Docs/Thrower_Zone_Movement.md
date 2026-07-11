# Thrower Zone Movement

**Tarih:** 11 Temmuz 2026

## Ne değişti?
İnsan atıcı artık kırmızı **thrower bölgesi** içinde gezinebilir; farklı X/Z noktalarından atış yapar. Runner sahasına giremez (`RoleZoneLimiter` home zone).

## Kontroller
| Durum | Hareket |
|-------|---------|
| Lobby → Atıcı **İnsan** + gamepad | Sol stick |
| Split (klavye) | **Ok tuşları** (WASD kosucu) |
| **Tab** → Thrower tam ekran | **WASD** (kosucu hareketi kilit) |

Nişan: mouse / sağ stick. Gövde nişan yönüne bakar (strafe).

## Koşu animasyonu
`Models/Running` (Runner ile aynı Mixamo klip). Hareket girdisi veya hız > eşik → koşu; hızına göre clip playback scale.

## Bot
Atışlar arasında aynı bölgede rastgele yer değiştirir (`ThrowerBot.repositionBetweenThrows`).

## Dosyalar
- `PlayerMovement.cs` — thrower hızı + nişana bakış
- `RoleZoneLimiter.cs` — home thrower zone clamp
- `SplitScreenManager.cs` — ThrowerFull WASD focus
- `PlayerInputHandler.cs` — `suppressMoveInput`
- `ThrowerBot.cs` — bölge reposition
- `CharacterModelVisual.cs` — thrower koşu tetik / hız

## Test
1. Lobby → Atıcı = İnsan → Start
2. Tab ile atıcı tam ekran → WASD ile sağa/sola yürü
3. Koşu animasyonu oynuyor mu?
4. Bölge dışına çıkamıyor musun?
5. Yeni yerden charge at → yay yeni origin’den mi çıkıyor?
