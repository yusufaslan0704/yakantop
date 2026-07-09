# Yakantop — Asset Checklist (Gün 2)

**Tarih:** 9 Temmuz 2026  
**Amaç:** Binary asset’lerin diskte ve repoda gerçekten var olduğunu doğrula; meta-only / eksik / kullanılmayanları netleştir.

---

## 1. Özet karar

| Kontrol | Sonuç |
|---------|--------|
| Meta-only dosya (meta var, binary yok) | **0** |
| Binary var, meta yok | **0** |
| Mixamo FBX’ler diskte | **7/7 mevcut** (`Assets/Resources/Models/`) |
| Mixamo FBX’ler git’te tracked | **7/7** — `RunnerCharacter.fbx` staged |
| Audio MP3’ler diskte + tracked | **4/4 mevcut** |
| Texture binary (png/jpg/…) | **0** (projede texture kullanılmıyor; materyaller düz renk) |
| Boş klasör temizliği | `Assets/Animations`, `Assets/Models` silindi (içerik yoktu) |

**Kabul kriteri notu:** Diskte “kayıp binary” yok. RunnerCharacter staged; BallData SFX dolduruldu. Kalan içerik eksiği: rig’siz runner (animasyon için Mixamo fallback).

---

## 2. Mixamo / karakter FBX

Konum: `Assets/Resources/Models/`  
Kod yolu: `Resources.Load("Models/...")` → `CharacterModelVisual`

| Dosya | Boyut | GUID | Git | Kullanıldığı yer |
|-------|------:|------|-----|------------------|
| `Idle.fbx` | 3.1 MB | `a3f76b1d…` | tracked | `CharacterModelVisual.idleClipPath` / `modelResourcePath` default; sahne karakterleri |
| `Running.fbx` | 1.8 MB | `03b0d324…` | tracked | `runClipPath` |
| `Jump.fbx` | 2.0 MB | `aa8716c2…` | tracked | `jumpClipPath` |
| `Chicken Dance.fbx` | 2.2 MB | `f75718d5…` | tracked | `danceClipPaths[0]`; `MixamoAnimationTester` |
| `Ymca Dance.fbx` | 2.1 MB | `2c30668f…` | tracked | `danceClipPaths[1]` |
| `Slide Hip Hop Dance.fbx` | 3.1 MB | `ab27d274…` | tracked | `danceClipPaths[2]` |
| `RunnerCharacter.fbx` | 440 KB | `c7d8e9f0…a5f` | **staged** | Runner default model; rig’siz → animasyon fallback Idle |

**Not:** Animasyon klipleri ayrı `Assets/Animations` klasöründe değil; Mixamo FBX içinden geliyor. Boş `Assets/Animations` klasörü kaldırıldı.

---

## 3. Audio

Konum: `Assets/Audio/`

| Dosya | Boyut | GUID | Git | Kullanıldığı yer |
|-------|------:|------|-----|------------------|
| `SFX_Throw.mp3` | 35 KB | `37f8a371…` | tracked | `Yakantop.unity` → `AudioManager.throwSfx` |
| `SFX_Hit.mp3` | 65 KB | `9cdf0c7f…` | tracked | `AudioManager.hitSfx` |
| `SFX_Dash.mp3` | 13 KB | `974a61dd…` | tracked | `AudioManager.dashSfx` |
| `SFX_Revive.mp3` | 42 KB | `aae8b096…` | tracked | `AudioManager.reviveSfx` |

**BallData per-ball SFX:** tüm `BallData_*.asset` içinde `throwSfx` / `hitSfx` = null  
→ `PlayerThrow` / `Ball` önce BallData SFX bakar; null ise global AudioManager fallback’e düşer (throw/hit için).

---

## 4. Materyaller

Konum: `Assets/Materials/` — hepsi mevcut, texture slot’ları boş (düz URP Lit renk).

| Materyal | Texture | Kullanım |
|----------|---------|----------|
| `Mat_RunnerZone.mat` | yok | Sahne zone mesh |
| `Mat_Saver.mat` | yok | Saver karakter |
| `Mat_ThrowerRed.mat` / `Mat_ThrowerBlue.mat` | yok | Thrower görselleri |
| `Mat_BallBouncy.mat` / `Mat_BallCurve.mat` | yok | Top prefab’ları |
| `HitEffect_Mat.mat` / `Mat_ImpactEffect.mat` | yok | VFX prefab’ları |
| `BallTrail_Mat.mat` | yok | Tüm `Ball_*.prefab` TrailRenderer materyali |

---

## 5. Klasör temizliği (yapıldı)

| Klasör | Önceki durum | İşlem |
|--------|--------------|--------|
| `Assets/Animations/` | Boş + `.meta` | **Silindi** |
| `Assets/Models/` | Boş + `.meta` | **Silindi** (gerçek modeller `Resources/Models`) |
| `Assets/Audio/` | 4 MP3 + meta | Korundu |
| `Assets/Textures/` | Yok | Oluşturulmadı (ihtiyaç yok) |

---

## 6. Aksiyon listesi

1. `RunnerCharacter.fbx` (+ meta) → `git add` (henüz untracked)  
2. BallData SFX alanlarını `SFX_Throw` / `SFX_Hit` ile doldur (opsiyonel polish)  
3. ~~`BallTrail_Mat` kullanılmayacaksa sil veya trail script’e bağla~~ → Prefab’larda zaten bağlı; dokunulmadı  
4. (İleride) texture eklenecekse `Assets/Textures/` oluştur  

**Bu turda yapılanlar:**
- [x] `git add` RunnerCharacter FBX + meta (staged)
- [x] BallData SFX slot’ları `SFX_Throw` / `SFX_Hit` ile dolduruldu
- [x] BallTrail kullanım doğrulandı (silinmedi)
