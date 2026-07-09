# Day 4 — Scene Cleanup + Secondary UI

**Tarih:** 9 Temmuz 2026  
**Kapsam:** Stale sahne alanları + Dodge cooldown HUD + SafeZone süre HUD

---

## Yapılanlar

### 1. Sahne stale field temizliği (`Yakantop.unity`)
- `GameManager`: eski `runnerPlayer` / `saverPlayer` kaldırıldı; güncel alanlar yazıldı (`waitForLobbyStart`, `roundsToWinMatch`, `intermissionDuration`).
- `TeamControlManager`: eski serialized `runnerPlayer` / `saverPlayer` kaldırıldı (runtime `PlayerManager` ile bulunuyor).
- `ThrowerBot`: eski `runnerTarget` / `saverTarget` kaldırıldı (dinamik hedef seçimi zaten kodda).

### 2. Dodge cooldown UI
- Yeni: `Assets/Scripts/DodgeCooldownUI.cs`
- Canvas’a eklendi; runtime’da bar üretir (DASH bar’ın altında).
- Aktif Runner/Saver `PlayerDodge`’ını takip eder.
- Renk: turuncu/sarı; tuş ipucu **Q**.

### 3. SafeZone süre HUD
- `SafeZone.cs` API genişletildi:
  - `IsInsideAny`
  - `GetProtectionRemaining`
  - `GetProtectionPercent`
- Yeni: `Assets/Scripts/SafeZoneUI.cs`
- Cepteyken veya bütçe dolmuyken üstte bar + kalan saniye.
- Etiket: `SAFE` (içeride) / `RECHARGE` (dışarıda dolarken).

### 4. Hierarchy düzeni
Önerilen yapı uygulandı (`Yakantop.unity`):

```
Scene
├── Managers          (GameManager, AudioManager, Directional Light, Global Volume)
├── Cameras           (Main Camera)
├── Arena             (HourglassArena runtime'da buraya parent edilir)
├── Players           (RunnerPlayer, SaverPlayer, ThrowerPlayer, RunnerBot)
├── UI                (Canvas, EventSystem)
├── RuntimeSpawned    (toplar / geçici spawn)
└── Disabled_TestObjects  [kapalı]
    ├── TargetDummy
    ├── ArenaWalls
    ├── ArenaZones_New
    └── zemin1
```

- Yardımcı: `SceneFolders.cs`
- `ArenaBuilder` → `HourglassArena` → `Arena`
- `PlayerThrow` topları → `RuntimeSpawned`
- Ek thrower clone’ları → `Players`
- Duplicate canvas / old player / test ball yoktu; TargetDummy + legacy arena `Disabled_TestObjects` altına alındı ve deaktif edildi.

---

## Play test checklist

1. Hierarchy’de 7 root klasör görünüyor mu?
2. Lobby → Start → altta **DASH** + altında **DODGE** barları görünür mü?
3. **Q** ile dodge → bar boşalır, hazır olunca yanar mı?
4. Cep (SafeZone) içine gir → üstte **SAFE** + saniye azalır mı?
5. Cepten çık → **RECHARGE** ve bar dolar mı?
6. Atılan toplar Hierarchy’de `RuntimeSpawned` altında mı?
7. Console’da missing script / null ref yok mu?

---

## Notlar

- Custom runner rig işi bilinçli olarak ertelendi (seçenek A); Mixamo fallback devam.
- Dodge animasyonu hâlâ Dash clip fallback kullanabilir (`Dodge.fbx` yoksa).
