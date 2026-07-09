# Yakantop — Missing Assets Listesi

**Tarih:** 9 Temmuz 2026  
**Not:** “Missing” = diskte yok **veya** referans boş / kullanılamaz durumda.

---

## 1. Diskte mevcut (artık missing değil)

Önceki raporlarda “meta-only” sanılan binary’ler şu an **mevcut**:

### Audio
| Dosya | GUID | Durum |
|-------|------|--------|
| `Assets/Audio/SFX_Throw.mp3` | `37f8a371…` | OK — AudioManager bağlı |
| `Assets/Audio/SFX_Hit.mp3` | `9cdf0c7f…` | OK — AudioManager bağlı |
| `Assets/Audio/SFX_Dash.mp3` | `974a61dd…` | OK — AudioManager bağlı |
| `Assets/Audio/SFX_Revive.mp3` | `aae8b096…` | OK — AudioManager bağlı |

### Models
| Dosya | Durum |
|-------|--------|
| `Assets/Resources/Models/Idle.fbx` | OK |
| `Assets/Resources/Models/Running.fbx` | OK |
| `Assets/Resources/Models/Jump.fbx` | OK |
| `Assets/Resources/Models/Chicken Dance.fbx` | OK |
| `Assets/Resources/Models/Ymca Dance.fbx` | OK |
| `Assets/Resources/Models/Slide Hip Hop Dance.fbx` | OK |
| `Assets/Resources/Models/RunnerCharacter.fbx` | OK (ama **rig yok** → animasyon desteklemez) |

### Prefabs
| Dosya | Durum |
|-------|--------|
| `Ball_Normal/Fast/Heavy/Bouncy/Curve.prefab` | OK |
| `HitEffect.prefab` / `ImpactEffect.prefab` | OK |
| `PF_DashTrail.prefab` / `PF_ReviveComplete.prefab` | OK |

### Materials
| Dosya | Durum |
|-------|--------|
| `Mat_RunnerZone`, `Mat_Saver`, `Mat_ThrowerRed/Blue` | OK |
| `Mat_BallBouncy`, `Mat_BallCurve`, trail/hit mats | OK |

---

## 2. Gerçek eksikler / boş referanslar

### A) Per-ball SFX (boş referans)
Tüm BallData asset’lerinde:

```
throwSfx: {fileID: 0}
hitSfx: {fileID: 0}
```

| Asset | Eksik |
|-------|--------|
| `BallData_Normal.asset` | throwSfx, hitSfx |
| `BallData_Fast.asset` | throwSfx, hitSfx |
| `BallData_Heavy.asset` | throwSfx, hitSfx |
| `BallData_Bouncy.asset` | throwSfx, hitSfx |
| `BallData_Curve.asset` | throwSfx, hitSfx |

**Etki:** Oyun sessiz kalmaz (AudioManager global SFX var) ama top tipine özel ses yok.

**Öneri:** Mevcut `SFX_Throw` / `SFX_Hit`’i ata veya tip başına ayrı clip ekle.

### B) Animasyonlu custom runner
| İstenen | Durum |
|---------|--------|
| Rig’li `RunnerCharacter` (Humanoid/Generic skinned) | **Yok** — mevcut FBX static mesh |
| Skin weights / Armature | **Yok** |

**Etki:** `CharacterModelVisual` Mixamo `Models/Idle` fallback kullanır; custom görünüm kaybolur veya animasyonsuz kalır.

### C) Orphan / missing script dosyası
| Beklenen | Durum |
|----------|--------|
| `MatchLobbyUI.cs` (ayrı dosya) | Yok — bilinçli merge (`MatchLobbyManager` içinde) |
| Orphan `.meta` | Yok |

### D) UI / gameplay asset eksikleri (henüz üretilmemiş)
| Asset | Durum |
|-------|--------|
| Dodge cooldown UI prefab/element | Yok |
| SafeZone budget HUD | Yok |
| Kill feed / scoreboard UI | Yok |
| Ball trail runtime component | `BallTrail_Mat` var, script bağlı değil |

---

## 3. Missing script / missing prefab (sahne)

| Tip | Sayı |
|-----|------|
| `m_Script: {fileID: 0}` | **0** |
| Orphan meta | **0** |
| Invalid GUID meta | **0** |
| BallData → prefab GUID kırık | **0** |
| AudioManager → clip GUID kırık | **0** |

---

## 4. Öncelikli tamamlanacaklar

1. **P1:** Rig’li runner FBX (veya custom modeli Mixamo’ya upload edip re-export)  
2. **P2:** BallData SFX alanlarını doldur  
3. **P2:** Dodge / SafeZone UI asset’leri  
4. **P3:** Ball trail wiring
