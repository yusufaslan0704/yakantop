# Day 7 — Windows Build Smoke

**Tarih:** 9 Temmuz 2026  
**Unity:** 6000.5.2f1  
**Hedef:** StandaloneWindows64

---

## Sonuç

| Kontrol | Durum |
|---------|--------|
| Batch build | ✅ `Result=Succeeded` · errors=0 · warnings=24 |
| Çıktı | ✅ `Builds/WindowsSmoke/Yakantop.exe` (~652 KB + Data) |
| Exe açılış | ✅ 12 sn ayakta kaldı, erken crash yok |
| Player.log | ✅ sahne yüklendi, modeller geldi, lobby/kontrol logları var |

**Build süresi:** ~2 dk 43 sn  
**Build boyutu (rapor):** ~125 MB

---

## Komut (tekrar için)

Unity Editor **kapalı** olmalı:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.5.2f1\Editor\Unity.exe" `
  -batchmode -nographics -quit `
  -projectPath "c:\Users\yusuf\dodgeball" `
  -executeMethod WindowsBuildSmoke.Build `
  -logFile "c:\Users\yusuf\dodgeball\Logs\WindowsBuildSmoke.log"
```

Script: `Assets/Editor/WindowsBuildSmoke.cs`

---

## Smoke launch notları

- Exe: `Builds/WindowsSmoke/Yakantop.exe`
- Player log: `%USERPROFILE%\AppData\LocalLow\DefaultCompany\dodgeball\Player.log`
- Log’da görülenler:
  - D3D12 / RTX 3070 Ti
  - `Kontrol RunnerPlayer'a geçti`
  - Mixamo Idle modelleri yüklendi (Runner/Saver/Thrower/RunnerBot)
  - `Gamepad yok: Thrower bot kontrolunde`

Zararsız uyarı: `d3d12: failed to query info queue interface` (debug interface; crash değil).

---

## Manuel ek test (istersen)

1. `Yakantop.exe` çift tıkla  
2. Lobby görünüyor mu?  
3. Start → dash / dodge / top atış  
4. Alt+F4 ile çık  

---

## Kabul kriteri

- [x] Build errors = 0  
- [x] Exe oluştu  
- [x] Exe crash etmeden açıldı  
- [x] Player.log’da fatal exception yok  
