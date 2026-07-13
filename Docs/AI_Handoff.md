# AI Handoff — Yakantop

**Güncelleme:** 13 Temmuz 2026  
Başka bir yapay zekaya / oturuma aktarım için durum özeti.

---

## Kimlik

- **Proje:** Yerel asimetrik dodgeball prototipi — **Yakantop**
- **Repo:** `c:\Users\yusuf\dodgeball` (git `main`)
- **Engine:** Unity **6000.5.2f1**, URP, tek sahne `Assets/Scenes/Yakantop.unity`
- **Dil / iletişim:** Kullanıcı Türkçe konuşur; kod/yorumlar karışık TR-EN
- **Referans commit’ler:**
  - `b6b9ad7` — Day 13 thrower kit (aim, zone move, combo abilities, Mirror ball, script folders, UI polish)
  - `27ab0c7` — Day 13 smoke harden (file-trigger build, GameUI TMP outline NRE guard, smoke docs)

`origin/main` geride kalabilir; push yalnızca kullanıcı isterse.

---

## Oyun özeti

- **Kaçanlar:** Runner + Saver (TeamControl ile switch)
- **Atıcılar:** 1–3 Thrower (insan veya bot)
- Lobby → round timer / revive / best-of-N
- Local split-screen; **online yok**

---

## Mimari (önemli)

Scriptler domain klasörlerinde (`Docs/Script_Structure.md`):

`Core/` · `Arena/` · `Thrower/` · `Runner/` · `Combat/` · `Shared/` · `UI/` · `VFX/`

Atıcı özel yetenekleri merkezi: `Assets/Scripts/Thrower/ThrowerAbilityRegistry.cs`  
Yeni yetenek = registry satırı + component; `PlayerThrow` / `BallSelectUI` otomatik uzar.

---

## Day 13 atıcı durumu (bitmiş özellikler)

| Slot (tipik 6 topla) | Özellik | Aktive |
|----------------------|---------|--------|
| 1–6 | Toplar (Fast/Heavy/Bouncy/Curve/Mirror + …) | seçim |
| 7 | Yüksek (AirLift) | RM tut = yüksel; LMB atış |
| 8 | Görünmez | V |
| 9 | Fake throw | Q |
| 0 | Gölge (Shadow blink + echo) | X |

- **Kombo:** max 2 seçim — 1. sarı (primary), 2. yeşil (combo). **Varsayılan top seçimi yok** (kullanıcı bilerek istemedi).
- Sol `BallSelectUI`: activate hint, CD fill, durum satırı; Yüksek seçiliyken Volley kapalı + UI’da net (`AIRLIFT`).
- Aim: charge path preview, reticle, hedef foot-ring.
- Zone movement: insan atıcı thrower zone’da gezer; bot reposition + run anim.
- Mirror ball asset/prefab eklendi.

Dokümanlar: `Docs/Thrower_*.md`, `Docs/Day13_Thrower_Aim_Feedback.md`, `Docs/Thrower_Combo_Select.md`, `Docs/Ball_Select_UI.md`, `Docs/Ayna_Top.md`.

---

## Smoke / build

- Exe: `Builds/WindowsSmoke/Yakantop.exe`
- Day 13 rebuild: **Succeeded, errors=0**, exe ~14s ayakta, Player.log’da GameUI NRE yok
- Editor kapalı: `-executeMethod WindowsBuildSmoke.Build`
- Editor açık: `Temp/RequestWindowsSmokeBuild` veya **Tools → Windows Smoke Build**
- Detay: `Docs/Day13_Thrower_Smoke.md`

---

## Bilinen zayıflıklar (özellik değil, borç)

1. Thrower özel FBX yok → Mixamo Idle fallback
2. Online yok
3. RunnerBot/SaverBot temel; derin AI yok
4. `InputSystem_Actions` bilinçli unused (polling) — `Docs/InputSystem_Actions_Note.md`

---

## Yol haritası — nerede kaldık

Yakın sprint sırası **tamamlandı**:

1. Script yapı / registry ✅
2. Combo UI polish ✅
3. Commit/docs ✅ (`b6b9ad7`, `27ab0c7`)
4. Windows smoke ✅

### Aktif öncelik (13 Temmuz 2026)

**Yeni özellik / yeni yetenek / yeni top ekleme yok.**  
Odak sırası:
1. Mevcut sistemleri **görsellik · oynanış · hissiyat** (Play ile doğrulanır)
2. Bu oturumda kullanıcı Play edemediği için: **backend altyapı** (`Docs/Thrower_Infrastructure.md`) — Loadout, Physics, AimResolver, ReleaseController, BallSpawner

Playtest checklist hâlâ faydalı (`Docs/Day13_Thrower_Smoke.md`) ama hissiyat tweak’leri Play açılınca.

### Önerilen polish backlog (öncelik sırası önerisi)

1. **Atıcı hissiyat** — charge windup / FOV / curve (**Done:** `Docs/Thrower_Charge_Feel.md`)  
2. **Top / hit feedback** — Mirror + mevcut toplar: iz, hit spark, ses/kamera punch tutarlılığı  
3. **Kaçan hissiyat** — dash/dodge/ability feedback (Day 8 zemini var; tutarlılık)  
4. **Arena / okunabilirlik** — zone, SafeZone, split-screen bilgi hiyerarşisi  
5. **UI cilası** — kombo listesi / Volley-AirLift netliği / cooldown barlar (işlev var; görsel sıkılaştırma)

İsteğe bağlı (sonra, kullanıcı seçerse): Thrower custom model, bot AI, remote push.

### Sıradaki asıl iş

1. Kullanıcıyla hangi polish dilimini başlatacağına karar ver (yukarıdaki 1–5).
2. O dilimde dar scope: feel/VFX/UX — registry’ye yeni yetenek ekleme.
3. Playtest → tweak → gerekirse smoke.

---

## Çalışma kuralları (bu kullanıcı için)

- Commit / push **yalnızca açıkça istenince**
- Markdown: kullanıcı istemeden gereksiz yeni doc üretme; mevcut `Docs/` güncelle tercih
- Scope dar tut; ilgili olmayan refactor yok
- Unity Editor açıksa batch build kilitlenir → file trigger veya menü kullan
- Script taşırken `.meta` GUID koru

---

## Hızlı doğrulama

```text
git log -3 --oneline
git status
Docs/Current_State.md
Docs/Day13_Thrower_Smoke.md
Docs/AI_Handoff.md
```

---

## İlk görev önerisi (yeni AI’ya)

**Yeni özellik ekleme yok.** Kullanıcı polish istiyor: görsellik / oynanış / hissiyat.  
Hangi dilimi (atış hissi, hit VFX, kaçan feel, arena, UI) seçeceğini sor; dar scope ile tweak et. Playtest checklist: `Docs/Day13_Thrower_Smoke.md`.

Güncel durum tablosu: `Docs/Current_State.md`.
