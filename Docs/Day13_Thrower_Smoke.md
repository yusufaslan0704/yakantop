# Day 13 — Thrower Smoke + Playtest

**Tarih:** 13 Temmuz 2026 (tamamlandı)  
**Unity:** 6000.5.2f1  
**Commit tabanı:** `b6b9ad7` + GameUI outline fix + smoke file trigger

---

## Windows smoke rebuild

| Kontrol | Durum |
|---------|--------|
| Batch (`-executeMethod`, Editor kapalı) | ✅ |
| Editor file trigger | ✅ `Temp/RequestWindowsSmokeBuild` |
| Menü | ✅ **Tools → Windows Smoke Build** |
| Result | ✅ `Succeeded` · errors=0 · warnings=38 · ~126 MB · ~28 sn |
| Çıktı | ✅ `Builds/WindowsSmoke/Yakantop.exe` |
| `Assembly-CSharp.dll` | ✅ 13 Temmuz 2026 13:47 |
| Exe ayakta | ✅ 14 sn crash yok |
| Player.log | ✅ NRE yok · modeller · kontrol logları |

### Tekrar

Editor **kapalı**:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.5.2f1\Editor\Unity.exe" `
  -batchmode -nographics -quit `
  -projectPath "c:\Users\yusuf\dodgeball" `
  -executeMethod WindowsBuildSmoke.Build `
  -logFile "c:\Users\yusuf\dodgeball\Logs\WindowsBuildSmoke.log"
```

Editor **açık**:

```powershell
"go" | Set-Content -Path "c:\Users\yusuf\dodgeball\Temp\RequestWindowsSmokeBuild" -Encoding ascii
```

veya **Tools → Windows Smoke Build**.

---

## Smoke launch notları

- Exe: `Builds/WindowsSmoke/Yakantop.exe`
- Player log: `%USERPROFILE%\AppData\LocalLow\DefaultCompany\dodgeball\Player.log`
- Görülenler:
  - D3D12 / RTX 3070 Ti
  - `Kontrol RunnerPlayer'a geçti`
  - Thrower / Runner modelleri
  - `Thrower bot kontrolunde`
- Düzeltilen: `GameUI.StyleText` TMP outline NRE (try/catch) — rebuild sonrası log temiz

---

## Manuel playtest checklist (Editor Play)

### Temel
- [ ] Lobby → Start, modeller görünür
- [ ] Runner dash / dodge / Flash-Shield-Invis (loadout)
- [ ] Thrower charge + yörünge önizleme + reticle

### Day 13 atıcı
- [ ] Bölge içi WASD hareket + koşu anim
- [ ] Kombo: sarı + yeşil (varsayılan seçim yok)
- [ ] Sol listede yetenek ipucu (RM/V/Q/X) + CD + durum satırı
- [ ] **7 Yüksek:** RM tut yüksel; Volley kapalı (bar `AIRLIFT`)
- [ ] **8 Görünmez:** V → kaçan kamerasında yok; atışta biter
- [ ] **9 Fake:** Q → sahte atış
- [ ] **0 Gölge:** X → blink + echo top
- [ ] Ayna top (Mirror) yörünge / hit

### Kabul
- [x] Build errors = 0  
- [x] Exe / Data güncellendi  
- [x] Exe crash etmeden açıldı  
- [x] Player.log’da GameUI NRE yok  
- [ ] Manuel Day 13 checklist (kullanıcı)
