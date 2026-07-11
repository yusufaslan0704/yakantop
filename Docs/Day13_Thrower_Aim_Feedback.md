# Day 13 — Thrower Aim Feedback

**Tarih:** 10 Temmuz 2026  
**Kapsam:** Atıcı nişan okunabilirliği (aim assist yok)

---

## Ne eklendi?

### Yörünge önizlemesi (`ThrowAimPreview`)
- Charge basılıyken gravity yayını çizer (`LineRenderer`)
- Origin / yön / force = gerçek atışla aynı (`PlayerThrow.TryGetAimPreview`)
- Yay bitişinde isabet marker’ı
- Bot charge kullanmadığı için sadece insan atıcıda görünür

### Reticle (`ThrowerReticleUI`)
- Eski TMP `+` gizlendi
- Image-based cross (4 kol + nokta)
- Charge arttıkça gap kapanır, renk ısınır

### Hedef highlight
- Merkez ray altında Runner/Saver varsa ayak halkası
- Charge yokken hafif, charge’da daha sıcak / büyük

---

## Kontroller
Değişmedi: sol tık / RT basılı tut = charge, bırak = at.

## Aim feel fix (aynı gün)
- Atıcı kamerası artık `LookAt` karakter değil — **pitch/yaw = nişan** (`useLookRotation`)
- Daha alçak/yakın offset, daha geniş pitch, biraz daha yüksek mouse sensitivity
- Zemin raycast isabeti XZ’de kalır, Y atış yüksekliğine çekilir (uzak saha için mouse’u aşırı kaldırmaya gerek kalmaz)

---

## Dosyalar
- `PlayerThrow.cs` — `TryGetAimPreview` + zemin aim düzeltmesi
- `ThrowAimPreview.cs` — yay + impact + highlight
- `ThrowerReticleUI.cs` — yeni nişangâh
- `CameraFollow.cs` / `SplitScreenManager.cs` — atıcı look-rotation
- `GameUI.cs` — legacy crosshair off + Ensure
- `UIColorPalette.cs` — AimArc / AimTarget renkleri

---

## Test
1. Lobby → Start → Thrower (Tab ile tam ekran atıcı)
2. Mouse ile bakış: ekran merkezi gittiğin yöne bakıyor mu? (yere yapışık değil)
3. Sol tık basılı tut → cyan yay + impact
4. Sahanın öbür tarafına normal mouse hareketiyle nişan alınabiliyor mu?
5. Crosshair Runner üstündeyken ayak halkası var mı?
