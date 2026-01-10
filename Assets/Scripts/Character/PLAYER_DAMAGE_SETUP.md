# Player Hasar Alma Setup Rehberi

## Player GameObject'inde Olması Gereken Component'ler

### 1. **Damageable** Component (ZORUNLU)
- **Component**: `Akila.FPSFramework.Damageable`
- **Konum**: Player GameObject'inin root'unda
- **Ayarlar**:
  - `Type`: `Player` (DamagableType.Player)
  - `Health`: 100 (veya istediğiniz değer)
  - `Auto Heal`: Açık (opsiyonel)
  - `Destroy On Death`: Kapalı (player ölünce destroy edilmemeli)

### 2. **Actor** Component (ÖNERİLİR)
- **Component**: `Akila.FPSFramework.Actor`
- **Konum**: Player GameObject'inin root'unda
- **Açıklama**: Damageable ile birlikte çalışır, UI ve death event'leri için kullanılır

### 3. **Layer Ayarları**
- **Player Layer**: Layer 8 (`Player`)
- **Kontrol**: Player GameObject'inin Inspector'ında `Layer` dropdown'ından `Player` seçili olmalı
- **Not**: Bot silahlarının `hittableLayers`'ında Layer 8 (Player) otomatik olarak eklenir

## Kontrol Listesi

✅ Player GameObject'inde `Damageable` component'i var mı?
✅ Player GameObject'inde `Actor` component'i var mı?
✅ Player GameObject'inin `Layer`'ı `Player` (Layer 8) mi?
✅ Bot silahının `FirearmPreset`'inde `hittableLayers` Layer 8'i içeriyor mu? (Otomatik)

## Sorun Giderme

### Player Hasar Almıyor
1. **Player'da Damageable var mı kontrol et:**
   - Inspector'da Player GameObject'ini seç
   - `Damageable` component'i var mı bak
   - Yoksa: `Add Component > Akila > FPS Framework > Health System > Damageable`

2. **Player'ın Layer'ı doğru mu kontrol et:**
   - Inspector'da Player GameObject'ini seç
   - `Layer` dropdown'ından `Player` (Layer 8) seçili olmalı

3. **Bot silahının hittableLayers'ında Player var mı kontrol et:**
   - Bot silahının `FirearmPreset`'ini aç
   - `Hittable Layers` mask'ında Layer 8 (Player) işaretli olmalı
   - Otomatik olarak eklenir, ama kontrol edebilirsiniz

4. **Console'da hata var mı kontrol et:**
   - `[BotFirearmController] ❌ IDamageable bulunamadı!` mesajı görüyorsanız, Player'da Damageable yok demektir

## Otomatik Setup (Opsiyonel)

Eğer Player'da bu component'ler yoksa, `PlayerMain.cs` scripti otomatik olarak ekleyebilir. 
Ancak genellikle Player prefab'ında zaten bulunur.


