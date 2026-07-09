# Yakantop — Day 3: Mixamo + Ses Durumu

**Tarih:** 9 Temmuz 2026

## Yerleştirilen yeni dosyalar

| İndirilen | Proje yolu | Rol |
|-----------|------------|-----|
| `Goalkeeper Overhand Throw.fbx` | `Assets/Resources/Models/Throw.fbx` | Throw animasyonu |
| `Hit To Side Of Body.fbx` | `Assets/Resources/Models/Hit.fbx` | Hit / eleme animasyonu |
| `Fireball.fbx` | `Assets/Resources/Models/Dash.fbx` | Dash placeholder (ileride gerçek Dash ile değiştirilebilir) |
| `scratchonix-dart-throw-380649.mp3` | `Assets/Audio/SFX_ThrowAlt.mp3` | Alternatif throw SFX (henüz slot’a bağlı değil) |

## Animasyon slot durumu

| Slot | Path | Durum |
|------|------|--------|
| Idle | `Models/Idle` | ✅ |
| Run | `Models/Running` | ✅ |
| Jump | `Models/Jump` | ✅ |
| Throw | `Models/Throw` | ✅ yeni |
| Dash | `Models/Dash` | ✅ placeholder (Fireball) |
| Hit | `Models/Hit` | ✅ yeni |
| Dodge | `Models/Dodge` | ❌ henüz yok — sen ekleyebilirsin |
| Revive | `Models/Revive` | ❌ henüz yok — sen ekleyebilirsin |

Kod: `CharacterModelVisual` Throw/Dash/Hit/Dodge/Revive slotlarını dinliyor.
Eksik clip varsa sessizce atlanır (kırmızı error yok).

## Ses durumu

| Slot | Dosya | Durum |
|------|-------|--------|
| throwSfx | `SFX_Throw.mp3` | ✅ AudioManager + BallData |
| hitSfx | `SFX_Hit.mp3` | ✅ |
| dashSfx | `SFX_Dash.mp3` | ✅ |
| reviveSfx | `SFX_Revive.mp3` | ✅ |
| dodgeSfx | — | Slot eklendi; dosya yoksa dash’e düşer |
| SFX_ThrowAlt | diskte | Opsiyonel — istersen bağlarız |

## Hâlâ eklenebilecekler (sen)

1. `Dodge.fbx` → `Assets/Resources/Models/Dodge.fbx`
2. `Revive.fbx` → `Assets/Resources/Models/Revive.fbx`
3. Gerçek `Dash.fbx` (Fireball yerine)
4. `SFX_Dodge.mp3` → `Assets/Audio/` (AudioManager.dodgeSfx)

## Test

1. Unity’ye dön → yeni FBX’ler import olsun (Humanoid)
2. Play → Thrower top atsın → Throw animasyonu
3. Runner vurulsun → Hit animasyonu
4. Dash → Dash/Fireball animasyonu
5. Console’da missing clip kırmızısı olmamalı (Dodge/Revive yoksa sessiz)
