# Rig’li Custom Runner — Nasıl Yapılır

**Durum (9 Tem 2026):**  
`Assets/Resources/Models/RunnerCharacter.fbx` şu an **statik mesh** (kemik/skin yok).  
Bu yüzden oyun Idle Mixamo modeline fallback ediyor; Idle/Run/Throw/Dash/Hit animasyonları custom görünümde oynamıyor.

Kod tarafı hazır: rig’li FBX gelince otomatik Humanoid import + animasyon bağlanır.

**Roller:**
| Rol | Dosya |
|-----|--------|
| Runner | `Assets/Resources/Models/RunnerCharacter.fbx` |
| Saver | `Assets/Resources/Models/SaverCharacter.fbx` |
| Thrower | `Assets/Resources/Models/ThrowerCharacter.fbx` |

---

## En kolay yol: Mixamo Auto-Rigger

1. [mixamo.com](https://www.mixamo.com) → giriş yap  
2. **Upload Character** → kendi modelini yükle (OBJ/FBX, T-pose veya A-pose tercih)  
3. Auto-Rigger ile kemikleri onayla  
4. İstersen bir Idle animasyonuyla birlikte indir  
5. İndirdiğin FBX’i şu isimle koy:

```
Assets/Resources/Models/RunnerCharacter.fbx
```

(Eski dosyanın üzerine yaz.)

6. Unity’ye dön → import bitsin  
7. Play → Runner’da **senin modelin** + Idle/Run/Jump/Throw/Dash/Hit oynamalı

### Mixamo indirme ayarları (önerilen)
- Format: **FBX for Unity**
- Skin: **With Skin**
- Pose: **T-pose** (sadece karakter) veya animasyonlu paket

---

## Alternatif: Blender’da rig

1. Modeli Blender’a al  
2. Armature ekle / Mixamo’dan gelen rig’e weight paint  
3. Export FBX:
   - Armature + Mesh
   - Apply Transform
   - Bake Animation (gerekirse)
4. Aynı yola `RunnerCharacter.fbx` olarak koy

---

## Unity’de kontrol listesi

Import sonrası `RunnerCharacter` Inspector:

| Ayar | Beklenen |
|------|----------|
| Animation Type | **Humanoid** |
| Avatar | Create From This Model |
| Skinned Mesh Renderer | Child’da olmalı |

Play Console’da:
- `RunnerPlayer: karakter modeli yuklendi -> Models/RunnerCharacter`
- `model rig'siz ... fallback` **görünmemeli**

---

## Dosya hazır olunca

1. FBX’i `Assets/Resources/Models/RunnerCharacter.fbx` olarak koy  
2. Bana yaz → import/meta + sahne path’lerini doğrularım  
3. Gerekirse scale / Y offset ayarlarız

---

## Not

Mevcut animasyonlar (Idle, Running, Jump, Throw, Dash, Hit) Humanoid retarget ile custom rig’li modele uygulanır.  
Ayrı “Throw for my character” indirmene gerek yok — aynı Mixamo Humanoid iskeleti yeterli.
