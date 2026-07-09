# Day 10 — Abilities: Flash + Shield

**Tarih:** 9 Temmuz 2026  
**Kapsam:** Runner/Saver yetenekleri — Flash (kör) + Shield (1 hit balon)

---

## Kontroller

| Yetenek | Klavye | Gamepad | CD | Süre |
|---------|--------|---------|----|------|
| **Flash** | E | Y / Triangle | 9 sn | atış + 2.4 sn kör |
| **Shield** | G | Right stick click | 11 sn | 4.5 sn / 1 hit |
| **Invis** | V | Left stick click | 12 sn | 4 sn görünmez |

---

## Flash
- **E** ile ileriye flashbang firlatilir (~0.55 sn fuse)
- Patlayinca dunya icinde mavi-beyaz yildiz burst
- Sadece atici kamerasi **tam beyaz** olur (~2.4 sn; ilk 35% tamamen kor)
- Runner kamerasi etkilenmez
- Bot aticilar: atis gecikir, aim bozulur
- CD: **9 sn**

## Shield
- Cyan balon VFX karakter etrafında
- İlk top çarpmasında balon patlar, oyuncu elenmez, top yok olur
- Süre dolarsa balon kendiliğinden iner

## Invisibility
- **V** / Left Stick Click: 4 sn görünmez
- Kendi kameranda hafif ghost; atıcı kamerasında tamamen yok
- ThrowerBot görünmez hedefi seçmez
- CD: **12 sn**

## Hit sırası
`SafeZone → Shield → Dodge → Eliminate`

## Dosyalar
- `PlayerFlash.cs`, `PlayerShield.cs`, `FlashBlindOverlay.cs`, `AbilityCooldownUI.cs`
- Prefab: `PF_Runner`, `PF_Saver`
- Canvas: 2× `AbilityCooldownUI` (FLASH / SHIELD bar)

## Test
1. Play → Runner → **E** → Tab ile atıcı kamerasına bak → beyaz flash?
2. Bot atışları flash sırasında isabetsiz mi?
3. **G** → balon görünüyor mu? Top gelince balon patlıyor, elenmiyor musun?
4. İkinci top → normal elenme?
