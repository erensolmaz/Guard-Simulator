# 🛡️ Guard Simulator

<div align="center">

![Unity](https://img.shields.io/badge/Unity-6000.1.3f1-black?style=for-the-badge&logo=unity)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![URP](https://img.shields.io/badge/URP-Universal%20Render%20Pipeline-blue?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)

**A first-person security simulation game - Complete missions, escort VIPs, and protect the city!**

### 🌐 [Play Demo](https://guard-simulator-web.vercel.app/)

</div>

---

## 📖 Project Overview

Guard Simulator is a first-person (FPS) security simulation game developed with the Unity game engine. Players take on the role of a security guard, completing various missions, engaging in dialogues with NPCs, and carrying out VIP escort tasks.

### ✨ Key Features

- 🎯 **Quest System** - Various missions with a dynamic quest system
- 🚗 **Vehicle Escort System** - Safely transport VIPs to their destinations
- 💬 **Dialogue System** - Interactive conversations with NPCs
- 🤖 **Artificial Intelligence (AI)** - Bot characters and enemy AI system
- 🎬 **Cinematic Camera** - Impressive visual transitions and scenes
- 🎵 **Sound System** - Dynamic music and sound effects management
- 🎮 **Main Menu** - Modern and animated user interface

---

## 🛠️ Technology Stack & Dependencies

### Game Engine
| Component | Version |
|-----------|---------|
| Unity | 6000.1.3f1 (Unity 6) |
| Render Pipeline | Universal Render Pipeline (URP) 17.2.0 |

### Core Packages
| Package | Version | Description |
|---------|---------|-------------|
| `com.unity.inputsystem` | 1.14.2 | New Input System |
| `com.unity.ai.navigation` | 2.0.9 | NavMesh AI navigation |
| `com.unity.animation.rigging` | 1.3.0 | Animation Rigging |
| `com.unity.postprocessing` | 3.5.0 | Post-processing effects |
| `com.unity.shadergraph` | 17.2.0 | Shader Graph |
| `com.unity.timeline` | 1.8.9 | Timeline animations |
| `com.unity.visualscripting` | 1.9.7 | Visual Scripting |
| `com.unity.nuget.newtonsoft-json` | 3.2.1 | JSON serialization |

### Asset Store Packages
- **Akila FPS Framework** - First-person weapon and character system
- Various 3D models and environment assets

---

## 💻 Installation & Deployment Guide

### Requirements

- **Unity Hub** (latest version recommended)
- **Unity 6000.1.3f1** or higher
- **Git** (for version control)
- **Visual Studio 2022** or **JetBrains Rider** (for C# development)

### Cloning the Project

```bash
# Clone the repository
git clone https://github.com/YOUR_USERNAME/Guard-Simulator.git

# Navigate to the project directory
cd Guard-Simulator
```

### Opening in Unity

1. Open **Unity Hub**
2. Click **Add** → **Add project from disk**
3. Select the cloned project folder
4. Make sure the correct Unity version is selected next to the project
5. Click on the project to open it

### First Run

1. When the Unity project opens, the `Library` folder will be created automatically
2. Wait for all assets to be imported
3. Open the main menu scene from `Assets/Scenes/MainMenu&Credits`
4. Press the **Play** button to test the game

---

## 🎮 Usage Instructions

### Game Controls

| Key | Action |
|-----|--------|
| `W A S D` | Movement |
| `Mouse` | Look/Aim |
| `Space` | Jump |
| `Shift` | Sprint |
| `E` | Interact |
| `Esc` | Menu |

### Scenes

| Scene | Description |
|-------|-------------|
| `MainMenu&Credits` | Main menu and credits screen |
| `GameScene` | Main gameplay scene |
| `Sandbox` | Testing and development scene |

### Building the Game

1. Open the `File` → `Build Settings` menu
2. Select the target platform (Windows, macOS, Linux)
3. Add the required scenes to the scene list
4. Click the `Build` button
5. Select the output folder

---

## 🔧 API Keys / Environment Variables

This project currently does not use any external APIs or services. All operations are performed locally.

### For Future Integrations

If online features are added in the future, the following variables may be required:

```csharp
// Example: Create a ScriptableObject in the Resources/Config folder
[CreateAssetMenu(fileName = "GameConfig", menuName = "Config/Game Configuration")]
public class GameConfig : ScriptableObject
{
    public string apiEndpoint;
    public string analyticsKey;
    // Other configuration values
}
```

---

## ⚠️ Known Issues & Troubleshooting

### Known Issues

| Issue | Status | Solution |
|-------|--------|----------|
| Shader compilation time may be long | 🟡 Expected | Wait during first launch, then no issues |
| Post-processing may reduce performance on some devices | 🟡 Expected | Reduce effects in Quality Settings |

### Troubleshooting

#### ❌ "Missing Reference" Errors
```
Solution: Right-click on the Assets folder → Reimport All
```

#### ❌ Pink/Magenta Materials
```
Solution: 
1. Edit → Rendering → Materials → Convert All Built-in Materials to URP
2. Change shaders to URP compatible shaders
```

#### ❌ Input System Not Working
```
Solution:
1. Edit → Project Settings → Player
2. Set "Active Input Handling" → "Both" or "Input System Package (New)"
3. Restart Unity
```

#### ❌ NavMesh AI Not Moving
```
Solution:
1. Make sure there is a NavMesh Surface component in your scene
2. Bake the NavMesh from Window → AI → Navigation
```

---

## 📁 Project Structure

```
Guard Simulator/
├── Assets/
│   ├── Data/                    # Data files
│   ├── GV & URP/               # URP settings
│   ├── Imported Assets/        # Externally imported assets
│   ├── Materias/               # Materials
│   ├── Music/                  # Music files
│   ├── Prefabs/                # Prefab objects
│   ├── Scenes/                 # Game scenes
│   │   ├── GameScene/          # Main game scene
│   │   ├── MainMenu&Credits/   # Main menu
│   │   └── Sandbox/            # Test scene
│   └── Scripts/                # C# source code
│       ├── Character/          # Character and NPC scripts
│       ├── Editor/             # Editor tools
│       ├── Gameplay/           # Game mechanics
│       ├── Sound/              # Sound system
│       └── UI/                 # User interface
├── Packages/                   # Unity package dependencies
├── ProjectSettings/            # Project settings
└── README.md                   # This file
```

---

## 🤝 Contributing

1. Fork this repository
2. Create a feature branch (`git checkout -b feature/NewFeature`)
3. Commit your changes (`git commit -m 'Add new feature'`)
4. Push your branch (`git push origin feature/NewFeature`)
5. Open a Pull Request

---

## 📜 License

This project is licensed under the **MIT License**. See the [LICENSE](LICENSE) file for details.

```
MIT License

Copyright (c) 2025 Guard Simulator

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

---

## 🙏 Credits & Acknowledgements

### Tools and Resources Used

- [Unity Documentation](https://docs.unity3d.com/) - Official Unity documentation
- [Universal Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest) - URP documentation
- [Akila FPS Framework](https://assetstore.unity.com/) - Base framework for FPS mechanics
- [Unity Input System](https://docs.unity3d.com/Packages/com.unity.inputsystem@latest) - New input system

### Special Thanks

- Unity Technologies - For the amazing game engine
- Asset Store community - For quality assets
- Open source community - For inspiring projects

---

<div align="center">

**Experience the thrill of being a security guard with Guard Simulator!** 🛡️

Made with ❤️ and Unity

</div>
