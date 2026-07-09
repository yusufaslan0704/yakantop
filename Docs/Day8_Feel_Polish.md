# Day 8 — Feel Polish

**Tarih:** 9 Temmuz 2026  
**Kapsam:** Dash / dodge / throw / hit-stop / kamera hissiyatı

---

## Ne değişti?

### Kamera
- `CameraFollow`: daha yakın offset, daha hızlı takip (`followSpeed` 12→16), look height
- `CameraShake`: ease-out sarsıntı + **FOV punch** API (`PunchFovAll`)

### Dash
| | Eski | Yeni |
|--|------|------|
| Mesafe | 4.0 | 4.6 |
| Süre | 0.18 | 0.15 |
| CD | 2.0 | 1.85 |
| Shake | 0.08 / 0.08 | 0.10 / 0.12 |
| FOV punch | — | +5.5° |

### Dodge
| | Eski | Yeni |
|--|------|------|
| Pencere | 0.32 | 0.28 (daha sıkı) |
| CD | 2.4 | 2.1 |
| Deflect hız | 1.05× | 1.18× |
| Başarı feedback | yok | ekstra shake + FOV |

### Throw
- Charge daha hızlı doluyor (`maxChargeTime` 1.2→0.95)
- Force aralığı 16–42
- Atış anında hafif shake + FOV punch

### Hit / eleme
- Hit-stop: ani freeze yerine **kısa slow-mo + ease-out**
- Knockback / fling biraz daha belirgin
- BallData tipleri ayrıştı (Fast snappy, Heavy ağır, Curve daha kıvrık)

### Hareket
- `moveSpeed` 8→8.5, `rotationSpeed` 12→14

---

## Play test checklist

1. **Dash (Shift):** daha patlayıcı mı? FOV kısa genişliyor mu?  
2. **Dodge (Q):** pencere daha kısa ama deflect daha tatmin edici mi?  
3. **Throw charge:** tam şarj ~1 sn’de mi? Atışta hafif punch var mı?  
4. **Hit:** vuruşta slow-mo “oturuyor” mu, donuk kalmıyor mu?  
5. **Heavy vs Fast:** Heavy daha ağır savrulma, Fast daha hafif mi?  
6. Kamera takip daha yapışkan / az lag mı?

---

## Not

Prefab instance’larda Inspector override varsa eski sayılar görünebilir.  
Override varsa: Prefab → **Revert** ilgili alanlar, veya Play’de script default’ları zaten yeni (sahne override yoksa).
