# Ball Visual Identity

**Tarih:** 13 Temmuz 2026  
**Kapsam:** Görsel kimlik (renk / ölçek / trail / UI). Fizik değerleri değişmedi.

## Kimlik tablosu

| Top | Palette | Ölçek (hızdan) | Trail | Material |
|-----|---------|----------------|-------|----------|
| Normal | Cybersphere mesh + albedo | ~1.00 (ref) | Kısa, ince | Textured Lit (soft emit) |
| Fast | Meshy mesh + albedo | ~0.80 | Uzun, ince | Textured Lit (soft emit) |
| Heavy | `BallHeavy` mor | ~1.38 | Kısa, kalın | Mat |
| Bouncy | Meshy jelly mesh + albedo | ~1.08 | Orta | Textured Lit (soft emit) |
| Curve | Meshy mesh + albedo | ~0.93 + ekvator halkası | Orta-geniş | Textured Lit (soft emit) |
| Mirror | Meshy mesh + albedo | ~0.93 | Uzun, ince | Textured Lit (metalik) |

## Kod / asset

- Renkler: `UIColorPalette.Ball*` + `ColorForBall(BallData)`
- Prefab: `BallVisualIdentity` — siluet `throwForce/mass` ile (Normal=1; Fast küçük, Heavy büyük). Root scale telafi edilir, collider aynı kalır.
- `scaleFromThrowSpeed` kapalıysa prefab `visualScale` kullanılır.
- Normal mesh: `Resources/Models/Balls/BallNormal` + `BallNormal_Albedo`
- Fast mesh: `Resources/Models/Balls/BallFast` + `BallFast_Albedo` (birim çap, ~8k face)
- Bouncy mesh: `Resources/Models/Balls/BallBouncy` + `BallBouncy_Albedo` (birim çap, ~8k face)
- Mirror mesh: `Resources/Models/Balls/BallMirror` + `BallMirror_Albedo` (birim çap, ~8k face)
- Curve mesh: `Resources/Models/Balls/BallCurve` + `BallCurve_Albedo` (birim çap, ~8k face)
- Fizik: `BallData_Fast` (throwForce 38, mass 0.75, knockback 3.5) — mesh sadece görsel
- Fizik Bouncy: `BallData_Bouncy` (maxBounces 3, bounceSpeedKeep 0.95) — mesh sadece görsel
- Fizik Mirror: `BallData_Mirror` (mirrorReturn) — mesh sadece görsel
- Fizik Curve: `BallData_Curve` (curveForce 68) — mesh sadece görsel
- Material: `Mat_Ball*.mat` (Normal runtime’da textured Lit)
- Trail tint spawn’da: `CombatVfx.TintBallTrail` (material rengi)
- UI slot: `BallSelectUI` → `UIColorPalette.ColorForBall`

## Doğrulama

Play → her topu at: siluet + trail + hit spark + sol UI accent 1 bakışta ayırt edilmeli.
