# Yakantop — Bug Listesi

**Tarih:** 9 Temmuz 2026  
**Kaynak:** Proje Audit

Öncelik: **P0** = Play/build kırar · **P1** = ciddi gameplay/UX · **P2** = temizlik/polish

---

## Düzeltilen (bu audit)

| ID | Öncelik | Bug | Düzeltme |
|----|---------|-----|----------|
| BUG-001 | P0 | Ana `Canvas` `localScale = (0,0,0)` — HUD görünmez / raycast bozuk | Scale `(1,1,1)` yapıldı (`Yakantop.unity`) |
| BUG-002 | P2 | `PlayerRevive` her frame `Debug.Log` spam | Log kaldırıldı |

---

## Açık buglar / riskler

| ID | Öncelik | Bug | Etki | Öneri |
|----|---------|-----|------|--------|
| BUG-003 | P1 | `RunnerCharacter.fbx` rig’siz (statik mesh) | Runner’da gerçek animasyon yok; Mixamo fallback’e düşülüyor | Rig’li FBX export et veya custom modeli animasyonlu hale getir |
| BUG-004 | P2 | BallData `throwSfx` / `hitSfx` null | Per-ball ses yok (global AudioManager çalışıyor) | BallData asset’lerine SFX ata |
| BUG-005 | P2 | Sahne stale alanlar: `runnerPlayer`, `saverPlayer`, `runnerTarget`, `saverTarget` | Inspector kafa karışıklığı; runtime’da `PlayerManager` kullanılıyor | Sahne YAML temizliği |
| BUG-006 | P2 | `InputSystem_Actions.inputactions` bağlı değil | Custom `PlayerInputHandler` polling kullanılıyor | Ya bağla ya dokümante et / kaldır |
| BUG-007 | P1 | Batch build bu oturumda alınamadı (Editor kilitli) | CI/smoke build doğrulanamadı | **Day 7:** Windows smoke build başarılı |
| BUG-008 | P2 | RunnerBot sadece wander | Solo test zayıf | **Day 6:** tehdit kaçışı + SafeZone |
| BUG-009 | P2 | Saver AI yok | Solo tam maç zor | **Day 6:** `SaverBot` eklendi |
| BUG-010 | P2 | Dodge cooldown UI yok | Oyuncu dodge hazır mı bilmiyor | **Day 4:** `DodgeCooldownUI` |
| BUG-011 | P2 | SafeZone süre bütçesi HUD yok | Pocket anti-camp görünmez | **Day 4:** `SafeZoneUI` |
| BUG-012 | P3 | Unity Cloud Project ID 401 log spam | Zararsız | Cloud project bağla veya yok say |

---

## Geçmişte çözülmüş (log izi)

| ID | Bug | Not |
|----|-----|-----|
| HIST-001 | `MatchLobbyUI` compile error | UI `MatchLobbyManager` içine merge edildi |
| HIST-002 | `RunnerCharacter.fbx.meta` invalid GUID (31 char) | 32 hex’e tamamlandı |
| HIST-003 | Karakter dönme / duck stuck | Rigidbody freeze + pose reset |

---

## Play kabul kontrol listesi

- [ ] Console Clear
- [ ] Play
- [ ] Kırmızı error yok
- [ ] Lobby görünüyor ve tıklanıyor
- [ ] Maç başlıyor
- [ ] Timer / skor / dash bar görünüyor (Canvas scale fix sonrası)
- [ ] Top atılınca ses geliyor
- [ ] Karakter modeli / animasyon fallback çalışıyor
