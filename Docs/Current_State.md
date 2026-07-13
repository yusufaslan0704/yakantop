# Yakantop — Current State

**Güncelleme:** 11 Temmuz 2026  
**Unity:** 6000.5.2f1 · URP · tek sahne `Yakantop.unity`

---

## Proje nedir?

Yerel asimetrik dodgeball prototipi:
- **Kaçanlar:** Runner + Saver  
- **Atıcılar:** 1–3 Thrower (bot veya gamepad insan)  
- Round timer / revive countdown / best-of-N maç  
- Lobby ile ayar (süre, skor, botlar)

---

## Sağlık durumu (Audit + Asset Gün 2 sonrası)

| Alan | Durum | Not |
|------|--------|-----|
| Compile | ✅ | Güncel log’da `error CS` yok |
| Missing script | ✅ | 0 |
| Binary assets (audio/fbx/prefab) | ✅ | Diskte mevcut; meta-only **0** |
| Mixamo FBX git | ✅ | 7/7 staged/tracked (`RunnerCharacter` eklendi) |
| Canvas HUD | ✅ | Scale `(0,0,0)` → `(1,1,1)` düzeltildi |
| Lobby imleç / manuel sayı girişi | ✅ | Cursor lock fix + TMP_InputField |
| Play kabul | ⏳ | Kullanıcı Play ile teyit etmeli |
| Windows build smoke | ✅ | Day 13 rebuild · errors=0 · exe 14s ayakta · NRE yok |

**Tahmini prototip tamamlanma:** ~%70–75 (yerel oynanış)

**Aktif odak:** Yeni özellik yok — mevcut sistemlerde görsellik / oynanış / hissiyat polish (`Docs/AI_Handoff.md`).

### Asset klasör notu
- Gerçek modeller: `Assets/Resources/Models/`
- Boş `Assets/Animations` ve `Assets/Models` klasörleri **silindi**
- Detay: `Docs/Asset_Checklist.md`, `Docs/Missing_Binary_Files_List.md`

---

## Çalışan sistemler

- Lobby (`MatchLobbyManager` + runtime UI)
- Game loop (`GameManager`, `GameUI`)
- Hareket: move / dash / jump / duck / dodge
- Combat: 5 top tipi, charge throw, hit-stop, dodge deflect
- Arena: procedural Hourglass (`ArenaBuilder`) + SafeZone
- Local MP: split-screen, TeamControl Runner↔Saver
- ThrowerBot (prediction + telegraph)
- CharacterModelVisual (Playables) + Mixamo fallback
- AudioManager (4 SFX bağlı)
- Dodge cooldown HUD + SafeZone süre HUD (Day 4)
- Temiz sahne hierarchy (Managers / Cameras / Arena / Players / UI / RuntimeSpawned)
- Day 5 prefablar: `PF_Runner` / `PF_Saver` / `PF_Thrower` / `PF_RunnerBot` + `PF_Ball_*` + `PF_*` VFX
- Day 6 bot AI: RunnerBot kaçış/SafeZone + SaverBot revive
- Day 7 Windows smoke build: `Builds/WindowsSmoke/Yakantop.exe`
- Day 8 feel polish: dash/dodge/throw/hit-stop/FOV punch
- Day 9 UI/UX: ortak palet, HUD outline, end-game kart, charge/revive bar
- Day 10 yetenekler: Flash / Shield / Invis
- Day 12 arena/VFX: hourglass neon, SafeZone rim pulse, CombatVfx sparks/parry/shield/elim
- Day 13 thrower aim: charge yörünge önizlemesi, Image reticle, hedef foot-ring
- Thrower zone movement: insan atıcı bölge içinde gezer; bot atışlar arası reposition + koşu animasyonu
- Thrower yüksek atış (7): sağ tık yüksel → sol tık Fast
- Thrower combo select: 2 özellik (sarı + yeşil), örn. Yüksek + Sekten
- Combo UI polish: yetenek tuş ipucu + CD satır + durum satırı; Yüksek’te Volley kapalı (UI’da net)
- Thrower görünmez (8): V ile kaçan kamerasından gizlen
- Thrower fake (9): Q ile sahte atış, dodge bait
- Thrower gölge (10): X ile blink + gölgeden çapraz echo atış
- Script yapısı: `Core/Arena/Thrower/Runner/Combat/Shared/UI/VFX` + `ThrowerAbilityRegistry`
- Thrower charge feel: windup hold, charge curve, FOV pull-in, scaled release punch (`Docs/Thrower_Charge_Feel.md`)

---

## Bilinen zayıflıklar

1. Thrower hâlâ Mixamo Idle fallback (özel Thrower FBX yok)
2. Online multiplayer yok
3. RunnerBot / SaverBot temel AI var (Day 6); derin taktik yok
4. `InputSystem_Actions` bağlı değil — polling bilinçli (`Docs/InputSystem_Actions_Note.md`)

---

## Bu sprintte yapılan audit işleri

- Sahne + referans + meta audit  
- Canvas scale fix  
- Revive log spam temizliği  
- Dokümanlar: `Audit_Report.md`, `Bug_List.md`, `Missing_Assets_List.md`, `Current_State.md`
- Day 10 yetenekler + Runner/Saver custom model
- BallData per-ball SFX çeşitliliği (Fast/Curve → ThrowAlt)
- BUG-003/004/005 kapatıldı

---

## Hemen yapılacak doğrulama

1. Console Clear → Play  
2. Lobby → Start → modeller + atıcı kombo UI  
3. Day 13 checklist: `Docs/Day13_Thrower_Smoke.md`  
4. Smoke rebuild: **Tools → Windows Smoke Build** (veya `Temp/RequestWindowsSmokeBuild`)

---

## Doküman indeksi

| Dosya | İçerik |
|-------|--------|
| `Docs/Audit_Report.md` | Tam audit özeti |
| `Docs/Bug_List.md` | Açık / kapalı buglar |
| `Docs/Missing_Assets_List.md` | Asset eksikleri |
| `Docs/Day10_Abilities_Flash_Shield.md` | Flash / Shield / Invis |
| `Docs/Day11_Polish_Smoke.md` | BallData SFX + smoke rebuild |
| `Docs/Day12_Arena_VFX_Polish.md` | Arena neon + combat VFX |
| `Docs/Day13_Thrower_Aim_Feedback.md` | Atıcı yörünge / reticle / hedef highlight |
| `Docs/Day13_Thrower_Smoke.md` | Day 13 Windows smoke + playtest checklist |
| `Docs/Thrower_Zone_Movement.md` | Atıcı bölge içi hareket |
| `Docs/Thrower_Combo_Select.md` | Atıcı çift seçim (sarı/yeşil) |
| `Docs/Thrower_Charge_Feel.md` | Atıcı charge windup / güç eğrisi / hissiyat |
| `Docs/Script_Structure.md` | Scripts klasör yapısı + ability registry |
| `Docs/Thrower_Infrastructure.md` | ThrowPhysics / Loadout ayrıştırması + sonraki dilimler |
| `Docs/Ball_Select_UI.md` | Atıcı top türü seçim listesi |
| `Docs/InputSystem_Actions_Note.md` | Input actions bilinçli unused |
| `Docs/Current_State.md` | Bu dosya — güncel durum |
| `Docs/AI_Handoff.md` | Başka AI / oturum için durum + sıradaki işler |
