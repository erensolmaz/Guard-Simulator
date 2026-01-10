# Görev Sistemi Kurulum Rehberi

## 🎯 Nasıl Kullanılır?

### 1. QuestSystem GameObject Oluştur

1. Hierarchy'de boş bir GameObject oluştur
2. Adını `QuestSystem` yap
3. `QuestSystem` script'ini ekle (Add Component > Quest System > Quest System)

### 2. DialogueNPC Component'ine Quest Ayarları Ekle

**ÖNEMLİ:** Quest ayarları artık **DialogueNPC component'inde** yapılır (DialogueData'da değil)!

1. NPC GameObject'ini seç (DialogueNPC component'i olan)
2. Inspector'da **Quest Settings** bölümünü bul
3. **Start Quest On Complete** checkbox'ını işaretle
4. **Quest Target NPC**: Tutuklanacak NPC'yi sürükle-bırak (sahne objesi olabilir)
5. **Quest Delivery Vehicle**: Teslim edilecek arabayı sürükle-bırak (VehicleEscortDelivery component'i olan GameObject)

### 3. Örnek Senaryo

**Senaryo:** NPC A ile konuş → NPC B'yi tutukla → Arabaya teslim et

1. **NPC A'yı hazırla:**
   - NPC A GameObject'ine `DialogueNPC` component'i ekle
   - DialogueData'yı ata
   - Quest Settings:
     - Start Quest On Complete: ✓
     - Quest Target NPC: [NPC B GameObject'ini sürükle]
     - Quest Delivery Vehicle: [Araç GameObject'ini sürükle]

2. **NPC B'yi hazırla:**
   - NPC B GameObject'ine `NPC` component'i ekle
   - Tutuklanabilir olmalı

3. **Araç hazırla:**
   - Araç GameObject'ine `VehicleEscortDelivery` component'i ekle
   - Collider (trigger) ekle

4. **Test:**
   - NPC A ile konuş
   - Diyalog tamamlandığında görev başlar
   - NPC B üzerinde ok işareti görünür
   - NPC B'yi tutukla
   - Araç üzerinde yanıp sönen ok görünür
   - NPC B'yi taşı ve teslim et

## ✅ Avantajlar

- **Sahne objelerini direkt ekleyebilirsiniz** (ScriptableObject sorunu yok)
- Her NPC kendi quest ayarlarını tutar
- Daha esnek ve kolay kullanım

## 📝 Notlar

- Quest Target NPC genellikle **başka bir NPC** olur (konuştuğunuz NPC değil)
- Eğer aynı NPC'yi tutuklamak istiyorsanız, Quest Target NPC'ye kendisini atayabilirsiniz
- Quest Delivery Vehicle mutlaka `VehicleEscortDelivery` component'ine sahip olmalı
