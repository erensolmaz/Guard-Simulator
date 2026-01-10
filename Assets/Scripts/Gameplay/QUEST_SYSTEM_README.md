# Görev Sistemi Kullanım Kılavuzu

Bu sistem, diyalog tamamlandıktan sonra görev başlatma ve takip etme özelliği sağlar.

## 🎯 Özellikler

- **Diyalog Tabanlı Görev Başlatma**: Diyalog tamamlandıktan sonra otomatik görev başlatma
- **Görsel İşaretler**: NPC ve araç üzerinde ok işaretleri
- **Süzülme Animasyonu**: NPC üzerindeki ok aşağı yukarı süzülür
- **Yanıp Sönme**: Araç üzerindeki ok yanıp söner
- **Otomatik Takip**: NPC tutuklandığında ve teslim edildiğinde görev otomatik güncellenir

## 📋 Kurulum

### 1. QuestSystem GameObject Oluştur

1. Hierarchy'de boş bir GameObject oluştur
2. Adını `QuestSystem` yap
3. `QuestSystem` script'ini ekle (Add Component > Quest System > Quest System)

### 2. DialogueData'ya Görev Ayarları Ekle

1. DialogueData asset'ini seç (Project penceresinde)
2. Inspector'da **Quest Settings** bölümünü bul
3. **Start Quest On Complete** checkbox'ını işaretle
4. **Quest Target NPC**: Tutuklanacak NPC'yi sürükle-bırak
5. **Quest Delivery Vehicle**: Teslim edilecek arabayı (VehicleEscortDelivery component'i olan GameObject) sürükle-bırak

### 3. NPC ve Araç Hazırlığı

**NPC:**
- NPC GameObject'inde `NPC` component'i olmalı
- NPC'nin tutuklanabilir olması için gerekli ayarlar yapılmış olmalı

**Araç:**
- Araç GameObject'inde `VehicleEscortDelivery` component'i olmalı
- Collider (trigger) eklenmiş olmalı

## 🎮 Kullanım

### Senaryo: Diyalog → Görev → Tutuklama → Teslim

1. **Diyalog Başlat**: NPC ile konuş (K tuşu veya etkileşim tuşu)
2. **Diyalog Tamamla**: Diyalog tamamlandığında görev otomatik başlar
3. **NPC Üzerinde Ok İşareti**: Görev başladığında NPC üzerinde süzülen ok işareti görünür
4. **NPC'yi Tutukla**: NPC'yi tutukla (diyalog seçeneği veya manuel)
5. **Ok İşareti Değişir**: NPC tutuklandığında ok işareti kaybolur, araç üzerinde yanıp sönen ok belirir
6. **NPC'yi Taşı**: V tuşu ile NPC'yi taşı (PlayerEscortController ile)
7. **Araç Yanına Git**: Araç üzerindeki yanıp sönen ok işaretini takip et
8. **Teslim Et**: F tuşu ile NPC'yi teslim et
9. **Görev Tamamlandı**: Görev otomatik olarak tamamlanır

## 🔧 Script Detayları

### QuestSystem.cs
- Görev yönetimi için ana sistem
- Singleton pattern kullanır
- Aktif görevleri takip eder

### Quest.cs
- Görev verisi ve durumu
- NPC tutuklandığında ve teslim edildiğinde güncellenir

### QuestMarker.cs
- NPC ve araç üzerinde görsel işaretler gösterir
- Süzülme ve yanıp sönme animasyonları

## ⚙️ Ayarlar

### QuestMarker Ayarları

**Marker Settings:**
- **Marker Height**: Ok işaretinin yüksekliği (karakterin üstünde)
- **Float Speed**: Süzülme hızı
- **Float Distance**: Süzülme mesafesi
- **Blink Speed**: Yanıp sönme hızı (sadece araç için)

**Visual Settings:**
- **Arrow Prefab**: Özel ok prefab'ı (opsiyonel, yoksa otomatik oluşturulur)
- **Marker Color**: Ok rengi
- **Marker Size**: Ok boyutu

## 📝 Örnek Senaryo

1. **Diyalog Oluştur**:
   - DialogueData asset'i oluştur
   - Node 0: "Merhaba, bir görevim var senin için"
   - Choice: "Tamam, ne yapmam gerekiyor?" → nextNodeID: 1
   - Node 1: "Şu NPC'yi tutukla ve arabaya teslim et"
   - Choice: "Anladım" → nextNodeID: -1, autoEndDialogue: true

2. **DialogueData Ayarları**:
   - Start Quest On Complete: ✓
   - Quest Target NPC: [Tutuklanacak NPC]
   - Quest Delivery Vehicle: [Araç GameObject]

3. **Test**:
   - NPC ile konuş
   - Diyalog tamamlandığında görev başlar
   - NPC üzerinde ok işareti görünür
   - NPC'yi tutukla
   - Araç üzerinde yanıp sönen ok görünür
   - NPC'yi taşı ve teslim et

## 🐛 Sorun Giderme

### Görev Başlamıyor
- QuestSystem GameObject'i scene'de var mı kontrol et
- DialogueData'da "Start Quest On Complete" işaretli mi?
- Quest Target NPC ve Delivery Vehicle atanmış mı?

### Ok İşareti Görünmüyor
- QuestMarker component'i NPC/araç üzerinde var mı?
- Marker Height değeri çok düşük olabilir
- Kameraya bakıyor mu kontrol et

### Görev Tamamlanmıyor
- NPC tutuklandı mı? (IsArrested = true)
- NPC teslim edildi mi? (F tuşu ile)
- VehicleEscortDelivery component'i doğru çalışıyor mu?

## 📌 Notlar

- Görev sistemi otomatik olarak NPC tutuklandığında ve teslim edildiğinde güncellenir
- Birden fazla görev aynı anda aktif olabilir
- Her görev kendi marker'larını yönetir
- Marker'lar otomatik olarak kaldırılır (görev tamamlandığında veya NPC tutuklandığında)
