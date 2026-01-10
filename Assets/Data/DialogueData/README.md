# Diyalog Sistemi Kullanım Kılavuzu

Bu klasör, oyundaki tüm NPC'ler için diyalog verilerini içerir. Her NPC için ayrı bir DialogueData asset'i oluşturulmalıdır.

---

## 🆕 Yeni DialogueData Oluşturma

### Unity Editor'dan 

1. **Project penceresinde** `Assets/Data/DialogueData/` klasörüne sağ tıkla
2. **Create > Dialogue System > Dialogue Data** seçeneğini tıkla
3. Yeni asset'e **NPC'nin adını** ver (örn: `GuardDialogue`, `MerchantDialogue`)
4. Asset'i seç ve Inspector'da düzenle

---

## 🔗 Node Sistemi Nasıl Çalışır?

Diyalog sistemi **Branching (dallanma) sistemi** kullanır.

### Temel Kavramlar

- **DialogueNode**: NPC'nin söylediği bir metin ve oyuncunun seçeneklerini içerir
- **DialogueChoice**: Oyuncunun seçebileceği bir seçenek
- **nextNodeID**: Seçenek seçildiğinde hangi node'a geçileceğini belirler

### Node Numaraları

- **Node 0**: Her zaman başlangıç node'u (diyalog buradan başlar)
- **Node 1, 2, 3...**: Sonraki node'lar
- **nextNodeID = -1**: Diyalog biter

### Örnek Yapı

```
Node 0: "Selam! Nasılsın?"
  ├─ Choice: "İyiyim, sen nasılsın?" → nextNodeID: 1
  └─ Choice: "Görüşürüz" → nextNodeID: -1 (biter)

Node 1: "Ben de iyiyim teşekkürler!"
  └─ Choice: "Tamam" → nextNodeID: -1 (biter)
```

### Node Ekleme

1. DialogueData asset'ini seç
2. Inspector'da **"Dialogue Nodes"** bölümüne git
3. **Size** değerini artır (örn: 0'dan 2'ye)
4. Her node için:
   - **NPC Text**: NPC'nin söyleyeceği metin
   - **Choices**: Oyuncunun seçenekleri
   - Her choice için **nextNodeID** ayarla

---

## 📖 Örnek Senaryo

#### 1. DialogueData Oluştur

- **Ad**: `GuardDialogue`
- **Klasör**: `Assets/Data/DialogueData/`

#### 2. Node'ları Ayarla

**Node 0 (Başlangıç):**
- **NPC Text**: "Burada ne yapıyorsun? Bu bölgeye giriş yasak!"
- **Choices**:
  - **Choice 1**: "Özür dilerim, bilmiyordum" → **nextNodeID**: 1
  - **Choice 2**: "Bana ne, geçeceğim" → **nextNodeID**: 2

**Node 1 (Özür):**
- **NPC Text**: "Tamam, bir daha olmasın. Dikkatli ol."
- **Choices**:
  - **Choice 1**: "Teşekkürler" → **nextNodeID**: -1 (biter)

**Node 2 (Kaba):**
- **NPC Text**: "O zaman seni tutuklamak zorundayım!"
- **Choices**:
  - **Choice 1**: "Tamam tamam, gidiyorum" → **nextNodeID**: -1 (biter)

#### 3. NPC'ye Component Ekle

1. Gardiyan GameObject'ini seç
2. **Add Component > Dialogue NPC**
3. **Dialogue Data**: `GuardDialogue` asset'ini sürükle
4. **Dialogue Camera Position**: `DialogueCameraPos` GameObject'ini sürükle


## 🎮 Hızlı Başlangıç Checklist

Yeni bir NPC için diyalog eklerken:

- [ ] DialogueData asset'i oluştur (`Assets/Data/DialogueData/`)
- [ ] Node'ları ayarla (en az Node 0)
- [ ] NPC GameObject'ine DialogueNPC component'i ekle
- [ ] DialogueData asset'ini DialogueNPC'ye ata
- [ ] DialogueCameraPos GameObject'i oluştur ve ayarla
- [ ] DialogueCameraPos'u DialogueNPC'ye ata
- [ ] Test et!

---

**Sorularınız için kodlara bakın veya takım arkadaşlarınıza sorun!**

