# Emote Wheel

**Kontrol**
- **T** basılı tut → emote çemberi açılır (imleç serbest)
- Mouse ile dilim seç → **T bırak** = emote at
- Hızlı seçim: **1–4** (ilk 4 emote)
- Gamepad: **Select** basılı tut + sağ stick ile seç; D-Pad = hızlı 1–4

**Emote listesi (8)**
`Haha!` · `Buraya!` · `Iskaladin!` · `<3` · `GG` · `Kos!` · `Wow!` · `...`

**Baloncuk yazı**
- Daha küçük (`textSize 0.16`, bold)
- Koyu outline katmanı ile netlik
- Süre 1.6 sn

**Dosyalar**
- `EmoteWheelUI.cs` — runtime radial UI
- `PlayerEmote.cs` — `PlayEmote(index)`, geniş liste, outline bubble
- `PlayerInputHandler.cs` — `EmoteWheelHeld/Released`
- `CameraFollow.cs` — çember açıkken look + cursor unlock
