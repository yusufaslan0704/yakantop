# Ayna Top

**Tarih:** 11 Temmuz 2026

## Davranış
Karşıya gider, sonra **bir kez** aynı yoldan (veya ters hızla) geri döner.

- İnsan + path: gidiş izi bitince iz **tersine** çevrilir, dönüş bitince yok olur
- Bot / fizik: ilk çevre çarpışmasında hız tersine döner; ikinci çarpışmada yok olur
- Gidişte oyuncuya çarparsa normal eleme (ayna tetiklenmez)

## Dosyalar
- `BallData.mirrorReturn`
- `BallData_Mirror.asset` — Ayna Top
- `PF_Ball_Mirror.prefab` + `Mat_BallMirror.mat`
- `Ball.cs` — path/fizik ayna dönüşü
- `PF_Thrower` `ballTypes` listesine eklendi (tuş **6**)

## Test
1. Liste → **Ayna** / **6**
2. At → karşıya gitsin, geri dönsün
3. Dönüşte Runner’a çarpabiliyor mu?
