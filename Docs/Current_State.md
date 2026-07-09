# Yakantop — Current State

**Güncelleme:** 9 Temmuz 2026 (Proje Audit sonrası)  
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
| Windows build smoke | ✅ | `Builds/WindowsSmoke/Yakantop.exe` — Day 7 |

**Tahmini prototip tamamlanma:** ~%70–75 (yerel oynanış)

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

1. Unity’de sahneyi kaydet / yeniden aç  
2. Console Clear → Play  
3. Lobby → Start → Runner + Saver modelleri görünüyor mu?  
4. Flash / Shield / Invis + top sesleri  
5. (Opsiyonel) **Tools → Windows Smoke Build**

---

## Doküman indeksi

| Dosya | İçerik |
|-------|--------|
| `Docs/Audit_Report.md` | Tam audit özeti |
| `Docs/Bug_List.md` | Açık / kapalı buglar |
| `Docs/Missing_Assets_List.md` | Asset eksikleri |
| `Docs/Day10_Abilities_Flash_Shield.md` | Flash / Shield / Invis |
| `Docs/InputSystem_Actions_Note.md` | Input actions bilinçli unused |
| `Docs/Current_State.md` | Bu dosya — güncel durum |
