# InputSystem_Actions — Not

**Durum:** Projede `Assets/InputSystem_Actions.inputactions` dosyası var ama **bağlı değil**.

Oyun girdisi `PlayerInputHandler` üzerinden doğrudan Input System polling ile okunur
(klavye + gamepad). Bu bilinçli bir tercih: rol bazlı enable/disable ve
Runner/Saver/Thrower ayrımı kodda daha net.

## Ne yapılmamalı
- Bu dosyayı silmeden önce `PlayerInputHandler` / `ThrowerHumanControl` akışını
  gözden geçir.
- Sahneye `PlayerInput` component ekleyip çift girdi yaratma.

## İleride
İstersen action map’e taşınabilir; şu an prototip için polling yeterli.
