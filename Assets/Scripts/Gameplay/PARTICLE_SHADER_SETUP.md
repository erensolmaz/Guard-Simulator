# Particle Shader Kurulum Rehberi

## 🔍 Particle Shader'ları Nasıl Kontrol Edilir?

### 1. Unity Editor'da Kontrol

1. **Hierarchy'de** bir GameObject seçin
2. **Add Component > Effects > Particle System** ekleyin
3. **Particle System** component'ini açın
4. **Renderer** modülünü açın
5. **Material** alanına bakın - eğer "None (Material)" görüyorsanız shader yüklü değildir

### 2. Shader'ları Kontrol Etme

**Unity Console'da test:**
```csharp
// URP shader kontrolü
Shader urpShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
Debug.Log(urpShader != null ? "URP Shader bulundu!" : "URP Shader bulunamadı!");

// Built-in shader kontrolü
Shader builtInShader = Shader.Find("Particles/Standard Unlit");
Debug.Log(builtInShader != null ? "Built-in Shader bulundu!" : "Built-in Shader bulunamadı!");
```

## 📦 Particle Shader'ları Nasıl Yüklenir?

### URP (Universal Render Pipeline) Kullanıyorsanız:

1. **Window > Package Manager** açın
2. **Unity Registry** seçin
3. **Universal RP** paketinin yüklü olduğundan emin olun
4. Eğer yüklü değilse: **Install** butonuna tıklayın

**URP Particle Shader'ları:**
- `Universal Render Pipeline/Particles/Unlit`
- `Universal Render Pipeline/Particles/Lit`
- `Universal Render Pipeline/Particles/Simple Lit`

### Built-in Render Pipeline Kullanıyorsanız:

Particle shader'ları Unity ile birlikte gelir, ekstra yükleme gerekmez.

**Built-in Particle Shader'ları:**
- `Particles/Standard Unlit`
- `Particles/Additive`
- `Particles/Alpha Blended`
- `Particles/Multiply`

## ✅ Shader'ların Yüklü Olduğunu Doğrulama

### Yöntem 1: Shader Graph Kontrolü

1. **Project** penceresinde **Create > Shader Graph > URP > Sprite Lit** (veya başka bir shader) oluşturun
2. Eğer oluşturabiliyorsanız shader'lar yüklüdür

### Yöntem 2: Material Oluşturma

1. **Project** penceresinde **Create > Material** oluşturun
2. Material'i seçin
3. **Shader** dropdown'ından **Universal Render Pipeline > Particles > Unlit** seçin
4. Eğer görünüyorsa shader yüklüdür

### Yöntem 3: Script ile Kontrol

```csharp
using UnityEngine;

public class ShaderChecker : MonoBehaviour
{
    void Start()
    {
        CheckShaders();
    }
    
    void CheckShaders()
    {
        string[] shaders = new string[]
        {
            "Universal Render Pipeline/Particles/Unlit",
            "Particles/Standard Unlit",
            "Particles/Additive"
        };
        
        foreach (string shaderName in shaders)
        {
            Shader shader = Shader.Find(shaderName);
            Debug.Log($"{shaderName}: {(shader != null ? "✓ Yüklü" : "✗ Yüklü değil")}");
        }
    }
}
```

## 🔧 Sorun Giderme

### Problem: Particle'lar mor görünüyor

**Çözüm:**
1. Render Pipeline'ınızı kontrol edin (URP mu Built-in mi?)
2. URP kullanıyorsanız URP paketinin yüklü olduğundan emin olun
3. QuestMarker script'i otomatik olarak doğru shader'ı seçmeye çalışır

### Problem: Shader bulunamıyor

**Çözüm:**
1. **Edit > Project Settings > Graphics** açın
2. **Scriptable Render Pipeline Settings** kontrol edin
3. URP kullanıyorsanız **UniversalRenderPipelineAsset** atanmış olmalı

### Problem: Particle'lar görünmüyor

**Çözüm:**
1. Particle System'in **Renderer** modülünü kontrol edin
2. Material atanmış mı bakın
3. **Render Mode** **Billboard** olmalı
4. **Sorting Order** değerini artırın (100 gibi)

## 📝 Notlar

- QuestMarker script'i otomatik olarak render pipeline'ı tespit eder
- URP kullanıyorsanız URP particle shader'larını kullanır
- Built-in kullanıyorsanız built-in particle shader'larını kullanır
- Shader bulunamazsa console'da uyarı gösterilir

## 🎯 Hızlı Test

Unity Editor'da şunu çalıştırın:

```csharp
Shader test = Shader.Find("Universal Render Pipeline/Particles/Unlit");
Debug.Log(test != null ? "URP Particle Shader OK!" : "URP Particle Shader YOK!");
```

Eğer "YOK" görüyorsanız, URP paketini yükleyin veya Built-in render pipeline kullanın.
