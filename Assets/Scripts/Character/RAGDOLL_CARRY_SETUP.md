# 🎯 Ragdoll Taşıma Sistemi - Kurulum Rehberi

## ✅ Hızlı Kurulum (3 Adım)

### 1️⃣ Ragdoll Kurulumu
```
Tools → Guard Simulator → Ragdoll Setup Tool
```
1. Karakteri seçin
2. "Kemikleri Otomatik Tespit Et"
3. "Ragdoll Kur" ✨

### 2️⃣ Komponentleri Ekleyin
Karaktere şu komponentleri ekleyin:
- ✅ **Damageable** (Akila FPS Framework)
- ✅ **Ragdoll** (Akila FPS Framework)
- ✅ **DamageableCarryable** (YENİ!)

### 3️⃣ Inspector Ayarları

#### Damageable (Script)
```
☐ Destroy On Death  ← KAPALI OLMALI!
☐ Destroy Root  ← KAPALI OLMALI!
Destroy Delay: 0  ← SIFIR OLMALI!
Type: NPC (veya Player)
Health: 100
```

#### DamageableCarryable (Script)
```
☑️ Move Whole Character  ← AÇIK!
☑️ Find Parent Root  ← AÇIK! (Bot içindeki Man_Full için)
Carry Rotation Offset: (0, 180, 0)
```

---

## 🎮 Nasıl Çalışır?

### Ölüm:
1. **Ateş edin** → Health 0 olur
2. **Ragdoll aktif olur** → Karakter düşer
3. **DamageableCarryable taşınabilir olur**

### Taşıma:
1. **[L] tuşuna basın** → Taşıma başlar
2. **Tüm rigidbody'ler kinematic olur** → Düşme durur
3. **Root transform taşınır** → Tüm karakter (Bot + Man_Full + Skeleton)
4. **Her frame pozisyon güncellenir** → Smooth taşıma

### Bırakma:
1. **[L] tekrar basın** → Bırakır
2. **Rigidbody'ler non-kinematic olur** → Ragdoll devam eder
3. **Karakter yere düşer** → Gerçekçi ragdoll

---

## 🔧 Sorun Giderme

### ❌ Karakter Yere Düşüyor

**Kontrol Edin:**
1. **Rigidbody'ler kinematic mi?**
   - Console'da "rigidbody kinematic değildi" log'u var mı?
   - Inspector'da rigidbody'ler kinematic mi?

2. **Ragdoll.isBeingCarried = true mi?**
   - Console'da "Ragdoll.isBeingCarried = true" log'u var mı?
   - Inspector'da Ragdoll → isBeingCarried = true mi?

3. **Root transform doğru mu?**
   - Console'da "Root transform bulundu: Bot" log'u var mı?
   - Bot GameObject'i mi taşınıyor?

**Çözüm:**
- `Move Whole Character` açık olmalı
- `Find Parent Root` açık olmalı
- Tüm rigidbody'ler kinematic olmalı

### ❌ Karakter Kayboluyor

**Kontrol Edin:**
1. **Destroy On Death kapalı mı?**
   - Inspector'da Damageable → Destroy On Death ☐

2. **Renderer'lar aktif mi?**
   - Console'da "Renderer kapalıydı" log'u var mı?
   - Inspector'da Man_Full → Renderer enabled mi?

**Çözüm:**
- Destroy On Death ☐ KAPALI
- Destroy Root ☐ KAPALI
- Destroy Delay = 0

### ❌ 5 Saniye Sonra Yok Oluyor

**Kontrol Edin:**
1. **Destroy Delay = 0 mı?**
   - Inspector'da Damageable → Destroy Delay = 0

2. **Console'da uyarı var mı?**
   - "destroyDelay > 0, sıfırlandı!" log'u

**Çözüm:**
- Destroy Delay = 0 yapın
- Update() her frame kontrol ediyor

---

## 📋 Kontrol Listesi

### Karakterde Olmalı:
- ✅ **Damageable** komponenti
- ✅ **Ragdoll** komponenti
- ✅ **DamageableCarryable** komponenti
- ✅ **Rigidbody'ler** (her kemikte)
- ✅ **CapsuleCollider'lar** (her kemikte)
- ✅ **CharacterJoint'ler** (bağlantılar için)

### Inspector Ayarları:
- ✅ Damageable → Destroy On Death: ☐ KAPALI
- ✅ Damageable → Destroy Root: ☐ KAPALI
- ✅ Damageable → Destroy Delay: 0
- ✅ DamageableCarryable → Move Whole Character: ☑️ AÇIK
- ✅ DamageableCarryable → Find Parent Root: ☑️ AÇIK

---

## 🎯 İç İçe Yapı (Bot + Man_Full)

### Hierarchy Yapısı:
```
Bot (rootTransform) ✅ Taşınır
├── Man_Full (mesh) ✅ Taşınır
└── Skeleton ✅ Taşınır
    ├── Pelvis (rigidbody) ✅ Kinematic
    ├── Spine (rigidbody) ✅ Kinematic
    └── ... (tüm kemikler)
```

### FindRootTransform() Nasıl Çalışır:
1. Parent'ta "Bot" ismini arar
2. Parent'ta Damageable arar
3. Bulursa rootTransform = parent
4. Bulamazsa rootTransform = transform

---

## 🧪 Test Adımları

1. **Play'e basın**
2. **Ateş edin** → Health 0 → Ragdoll aktif
3. **5 saniye bekleyin** → Karakter hala görünür olmalı
4. **[L] tuşuna basın** → Taşıma başlar
5. **Karakter elinizde durmalı** → Düşmemeli
6. **Hareket edin** → Karakter takip etmeli
7. **[L] tekrar** → Yere bırakır, ragdoll devam eder

---

## 📝 Console Log'ları

### Başarılı Taşıma:
```
[DamageableCarryable] Destroy On Death devre dışı bırakıldı. destroyDelay=0
[DamageableCarryable] Root transform bulundu: Bot
[Damageable] Karakter taşınabilir, destroy ve respawn engellendi. GameObject: Bot
[DamageableCarryable] Taşıma başladı. Main bone: Hips
[DamageableCarryable] Ragdoll.isBeingCarried = true
[DamageableCarryable] 12 rigidbody kinematic yapıldı
[DamageableCarryable] Root transform taşıma pozisyonunda: Bot at (x, y, z)
```

### Sorun Varsa:
```
[DamageableCarryable] destroyDelay > 0, sıfırlandı!
[DamageableCarryable] 3 rigidbody kinematic değildi, düzeltildi!
[DamageableCarryable] GameObject kapalıydı, aktif edildi: Bot
```

---

## ⚙️ Önemli Notlar

### ✅ Yapılması Gerekenler:
- Ragdoll Setup Tool ile ragdoll kurun
- Destroy On Death KAPALI yapın
- Move Whole Character AÇIK yapın
- Find Parent Root AÇIK yapın (Bot içindeki Man_Full için)

### ❌ Yapılmaması Gerekenler:
- Destroy On Death AÇIK yapmayın
- Destroy Delay > 0 yapmayın
- Move Whole Character KAPALI yapmayın

---

## 🎉 Artık Hazırsınız!

Sisteminiz tamamen çalışıyor:
1. ✅ Health 0 → Ragdoll aktif
2. ✅ [L] → Taşıma başlar
3. ✅ Ragdoll halinde taşınır
4. ✅ Düşmez, kaybolmaz
5. ✅ Tüm karakter birlikte taşınır

**İyi oyunlar! 🎮✨**

