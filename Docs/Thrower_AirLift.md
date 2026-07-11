# Thrower Yüksek Atış (7)

**Tarih:** 11 Temmuz 2026

## Akış
1. Liste → özellik seç (sarı çerçeve)
2. İkinci özellik seç (yeşil çerçeve = kombo)
3. Örn. **7 Yüksek** + **4 Sekten**: sağ tık yüksel → sol tık **Sekten** top
4. Sadece 7: yüksel + Fast

İptal: hover’da tekrar sağ tık. Aynı tuşa tekrar bas = seçimi kaldır.

## Notlar
- Mod 7 dışındaki toplarda sağ tık hâlâ **Volley**
- Gamepad: LB basılı tut = yüksel (mod 7’de)
- Havadayken kamera daha dik aşağı bakabilir; nişan zemine / oyuncuya doğru gider (Fast top aşağı atılabilir)

## Dosyalar
- `PlayerAirLift.cs`
- `PlayerThrow.cs` — AirLiftModeSelected + Fast override
- `BallSelectUI.cs` — 7. satır
- `PlayerInputHandler.cs` — VolleyHeld / Released
- `PlayerVolley.cs` — mod 7’de kapalı
