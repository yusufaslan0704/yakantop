# Day 5 — Prefab Temizliği

**Tarih:** 9 Temmuz 2026  
**Kapsam:** Oyuncu prefabları, Ball/VFX isimlendirme, BallData uyumu, SFX prefix

---

## Yapılanlar

### 1. Oyuncu prefabları (ayrıldı)
| Prefab | Kaynak sahne objesi |
|--------|---------------------|
| `Assets/Prefabs/PF_Runner.prefab` | RunnerPlayer |
| `Assets/Prefabs/PF_Saver.prefab` | SaverPlayer |
| `Assets/Prefabs/PF_Thrower.prefab` | ThrowerPlayer |
| `Assets/Prefabs/PF_RunnerBot.prefab` | RunnerBot |

### 2. Ball prefabları + BallData
- `Ball_*` → `PF_Ball_*` (GUID korundu → BallData referansları bozulmadı)
- Her top tipi için materyal:
  - `Mat_BallNormal` / `Mat_BallFast` / `Mat_BallHeavy` (yeni)
  - `Mat_BallBouncy` / `Mat_BallCurve` (mevcut)
- Hepsi `hitEffectPrefab` → `PF_ImpactEffect`

### 3. VFX `PF_` prefix
| Eski | Yeni |
|------|------|
| HitEffect | `PF_HitEffect` |
| ImpactEffect | `PF_ImpactEffect` |
| (zaten) | `PF_DashTrail`, `PF_ReviveComplete` |

### 4. Audio `SFX_` prefix
Zaten uyumluydu: `SFX_Throw`, `SFX_Hit`, `SFX_Dash`, `SFX_Revive`, `SFX_ThrowAlt`

### 5. Editor aracı
- `Assets/Editor/Day5PrefabCleanup.cs`
- Menü: **Yakantop → Day 5 Prefab Cleanup**
- Sahne objelerini prefab instance’a bağlar (`SaveAsPrefabAssetAndConnect`)

---

## Senin yapman gereken (kabul kriteri)

Unity projeyi kilitlediği için sahne bağlantısı otomatik tamamlanamadı.

1. Unity Editor’e odaklan (script recompile olsun)
2. Menü: **Yakantop → Day 5 Prefab Cleanup**
3. Hierarchy’de `Players` altında Runner/Saver/Thrower/RunnerBot mavi prefab ikonu göstermeli
4. Console: `[Day5PrefabCleanup] Connected ...` logları

Alternatif: Unity’yi kapatıp batchmode:
```
"C:\Program Files\Unity\Hub\Editor\6000.5.2f1\Editor\Unity.exe" -batchmode -nographics -quit -projectPath "c:\Users\yusuf\dodgeball" -executeMethod Day5PrefabCleanup.Run -logFile "Logs\Day5PrefabCleanup.log"
```

---

## Play test

1. Prefab ikonları Hierarchy’de görünüyor mu?
2. Lobby → Start → roller çalışıyor mu?
3. Top atınca doğru renk/materyal + impact VFX?
4. Console’da missing prefab / null ref yok mu?
