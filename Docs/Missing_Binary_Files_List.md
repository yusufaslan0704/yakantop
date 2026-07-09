# Yakantop — Missing Binary Files List (Gün 2)

**Tarih:** 9 Temmuz 2026  
**Tanım:** “Missing” = diskte yok **veya** beklenen referans boş / git’te yok / kullanılabilir değil.

---

## A) Diskte kayıp binary (meta-only)

| Dosya | Durum |
|-------|--------|
| — | **Yok.** Tüm `.meta` dosyalarının karşılığı diskte mevcut. |

---

## B) Git’te eksik / untracked binary

| Dosya | Durum |
|-------|--------|
| `RunnerCharacter.fbx` (+ meta) | **Düzeltildi** — `git add` ile staged (9 Tem 2026) |

Commit henüz yapılmadı; kullanıcı isterse commit edilir.

---

## C) Referansı boş olan ses alanları

| Durum | Not |
|-------|-----|
| **Düzeltildi (9 Tem 2026)** | 5× `BallData_*.asset` → `throwSfx` = `SFX_Throw`, `hitSfx` = `SFX_Hit` |

Eski boş slot listesi arşiv:

| Eksik referans | Asset | Kullanıldığı kod | Etki |
|----------------|-------|------------------|------|
| ~~throwSfx / hitSfx~~ | `BallData_Normal/Fast/Heavy/Bouncy/Curve` | `PlayerThrow` / `Ball` | ~~Per-ball ses yoktu~~ → artık dolu |

---

## D) Texture binary eksikleri

| Beklenen | Durum | Not |
|----------|--------|-----|
| `Assets/Textures/*` | Klasör yok | Materyaller texture kullanmıyor (`m_Texture: {fileID: 0}`) |
| Karakter albedo/normal map | Yok | Mixamo FBX kendi materyaliyle geliyor; custom texture yok |
| Ball trail texture | Yok | `BallTrail_Mat` texture’suz; zaten hiçbir yere bağlı değil |

**Sonuç:** Kırık texture referansı yok. Texture “eksikliği” tasarım tercihi (düz renk URP Lit).

---

## E) Materyal / kullanılmayan asset

| Dosya | Disk | Referans | Durum |
|-------|------|----------|--------|
| `Assets/Materials/BallTrail_Mat.mat` | Var | Tüm `Ball_*.prefab` TrailRenderer | **OK** (önceki orphan şüphesi yanlıştı) |
| Diğer `Mat_*.mat` | Var | Sahne / prefab | OK |

---

## F) Klasör yolu karışıklığı (eksik değil, dokümantasyon)

| Yanlış varsayım | Gerçek konum |
|-----------------|--------------|
| `Assets/Models/*.fbx` | `Assets/Resources/Models/*.fbx` |
| `Assets/Animations/*.anim` | Animasyonlar Mixamo FBX içinde; ayrı anim klasörü yoktu (boş klasör silindi) |

Kod `Resources.Load("Models/Idle")` kullandığı için doğru yol `Assets/Resources/Models/Idle.fbx`.

---

## G) Kalite / içerik eksikleri (binary var ama yetersiz)

| Asset | Sorun | Kullanım | Etki |
|-------|-------|----------|------|
| `RunnerCharacter.fbx` | Rig / SkinnedMesh yok (static) | Runner görseli | Animasyon oynatılamaz → Idle Mixamo fallback |

Bu “dosya yok” değil; “istenilen formatta değil”.

---

## H) Özet sayaç (aksiyon sonrası)

| Kategori | Adet |
|----------|------|
| Meta-only kayıp binary | 0 |
| Untracked kritik binary | **0** (RunnerCharacter staged) |
| Boş BallData SFX slot | **0** (dolduruldu) |
| Kırık texture GUID | 0 |
| Orphan material | **0** (BallTrail prefab’larda kullanılıyor) |
| Silinen boş klasör | 2 (`Animations`, `Models`) |

---

## I) Kurtarma checklist

- [x] `git add` RunnerCharacter FBX + meta (staged; commit kullanıcı isteğine bağlı)
- [x] BallData SFX slot’larını doldur
- [x] BallTrail kullanımını doğrula (bağlı — silinmedi)
- [ ] (İleride) Rig’li runner FBX ile `RunnerCharacter` değiştir
