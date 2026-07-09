# Day 6 — Bot AI

**Tarih:** 9 Temmuz 2026  
**Kapsam:** RunnerBot tehdit kaçışı + SafeZone, SaverBot revive, ThrowerBot log temizliği

---

## Yapılanlar

### 1. RunnerBot (yenilendi)
- Tehdit: yakın toplar (yaklaşma yönü ağırlıklı) + Thrower’lar
- Güçlü tehditte **SafeZone**’a koşar (koruma bütçesi varsa)
- Cepte bütçe bitince / tehdit azalınca çıkar
- Tehdit yoksa eski wander davranışı
- `ArenaBuilder` hâlâ `wanderZone` bağlar

### 2. SaverBot (yeni)
- `Assets/Scripts/SaverBot.cs` + `PF_Saver` prefab’ına eklendi
- **İnsan Saver kontrol etmiyorken** otomatik çalışır
- Elenen Runner’a gider → menzilde `PlayerRevive` hold eder
- Downed yoksa hayattaki Runner yakınında idle orbit
- Hafif top sidestep

### 3. Entegrasyon
- `PlayerRevive`: SaverBot hold’unu insan F/gamepad ile aynı kabul eder
- `TeamControlManager`: Saver’da `SaverBot` varken `PlayerRevive` açık kalır (bot revive edebilsin)
- `SafeZone.TryGetNearestCenter` botlar için eklendi
- `ThrowerBot`: hedef değişince `Debug.Log` spam kaldırıldı

---

## Davranış özeti

| Durum | RunnerBot | SaverBot |
|-------|-----------|----------|
| Round kapalı | Durur | Durur |
| Tehdit var | Kaç / cebe gir | Sidestep + revive hedefi |
| Runner elendi | — | Runner’a koş → hold revive |
| İnsan Saver aktif | — | Durur (kontrol insanda) |

---

## Play test checklist

1. Lobby → **Kosucu Botu Açık** → Start  
2. RunnerBot top gelince kaçıyor / cebe giriyor mu?  
3. İnsan Runner’ı ele → kontrol Saver’a geçince SaverBot durmalı (sen oynuyorsun)  
4. İnsan Runner hayattayken (kontrol Runner’da) SaverBot elenen **RunnerBot**’u revive ediyor mu?  
5. Console’da ThrowerBot spam yok mu?

### Revive testi (önerilen)
- Lobby: Runner bot açık  
- Sen Runner oyna; RunnerBot’u thrower’a yedir  
- Kontrol sende (Runner) kalsın → SaverBot downed RunnerBot’a gidip revive etmeli  

---

## Notlar

- SaverBot, `PlayerMovement` kapalıyken kendi Rigidbody hareketini kullanır
- İnsan Saver’a geçince bot kendini kapatır; çakışma olmamalı
