# Day 12 — Arena + VFX Polish

**Tarih:** 10 Temmuz 2026  
**Kapsam:** Procedural arena neon + combat VFX (binary asset yok)

---

## Arena

| Degisiklik | Detay |
|------------|--------|
| Hourglass neon | Diagonal duvar ustune kirmizi neon tac (`CreateDiagonalWall`) |
| SafeZone rim pulse | Cep neonlari charge/deplete ile nabiz atar (`SafeZone.rimRenderers`) |

## Combat VFX (`CombatVfx.cs`)

| Event | Efekt |
|-------|--------|
| Duvar/zemin carpma | Burst + sparks (normal yonlu) |
| Oyuncu vurus | Buyuk burst + sparks (top rengi) |
| Dodge parry | Cyan genisleyen halka |
| Shield break | Burst + 10 shard |
| Eliminate | Kirmizi puff |
| Throw | Trail rengi top materialinden |

## Diger

- `VFXAutoDestroy`: opsiyonel alpha fade
- Prefab `PF_ImpactEffect` hala spawn edilir (eski + yeni birlikte)

## Test

1. Play → arena hourglass kenarlarinda neon var mi?
2. Cepe gir → rim nabzi; butce tukenince soluklasiyor mu?
3. Top at → trail top renginde mi?
4. Duvara carp → sparks; oyuncuya carp → buyuk burst + elim puff
5. Q parry → cyan ring; G shield break → shard
