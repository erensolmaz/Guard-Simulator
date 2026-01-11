# 🏗️ Guard Simulator - Oyun Mimarisi ve Framework Dokümantasyonu

<div align="center">

**Kapsamlı Sistem Mimarisi, Flowchart ve Framework Diyagramları**

</div>

---

## 📊 Sistem Mimarisi (System Architecture)

Guard Simulator, modüler bir yapıda tasarlanmış bir FPS güvenlik simülasyonu oyunudur. Aşağıda oyunun genel mimarisi görselleştirilmiştir:

```mermaid
graph TB
    subgraph "Unity Game Engine"
        subgraph "Core Systems"
            PM[PlayerMain]
            QS[QuestSystem]
            DM[DialogueManager]
            SM[SoundManager]
        end
        
        subgraph "Character Systems"
            NPC[NPC Characters]
            BAI[BotAI Enemy AI]
            PEC[PlayerEscortController]
            DRN[DialogueNPC]
        end
        
        subgraph "Gameplay Systems"
            VED[VehicleEscortDelivery]
            QMK[QuestMarker]
            CI[CollectibleItem]
        end
        
        subgraph "UI Systems"
            MMM[MainMenuManager]
            CC[CinematicCamera]
            CE[CameraEffects]
            QOU[QuestObjectiveUI]
        end
    end
    
    PM --> QS
    PM --> DM
    PM --> PEC
    
    QS --> NPC
    QS --> QMK
    QS --> VED
    QS --> QOU
    
    DM --> DRN
    DM --> QS
    DM --> SM
    
    BAI --> NPC
    BAI --> PM
    
    PEC --> NPC
    PEC --> VED
    
    SM --> MMM
    
    CC --> MMM
```

---

## 🔄 Ana Oyun Döngüsü (Main Game Loop)

```mermaid
flowchart TD
    START([Oyun Başlat]) --> MAIN_MENU[Ana Menü]
    MAIN_MENU --> |"Play"| LOAD_GAME[Game Scene Yükle]
    MAIN_MENU --> |"Settings"| SETTINGS[Ayarlar Paneli]
    MAIN_MENU --> |"Credits"| CREDITS[Krediler]
    MAIN_MENU --> |"Quit"| EXIT([Çıkış])
    
    SETTINGS --> MAIN_MENU
    CREDITS --> MAIN_MENU
    
    LOAD_GAME --> CINEMATIC[Sinematik Intro]
    CINEMATIC --> GAMEPLAY[Gameplay Loop]
    
    GAMEPLAY --> CHECK{Görev Var mı?}
    CHECK --> |Evet| QUEST_ACTIVE[Görevi Takip Et]
    CHECK --> |Hayır| FREE_ROAM[Serbest Dolaşım]
    
    FREE_ROAM --> NPC_INTERACT{NPC ile Etkileşim?}
    NPC_INTERACT --> |Evet| DIALOGUE[Diyalog Başlat]
    NPC_INTERACT --> |Hayır| FREE_ROAM
    
    DIALOGUE --> QUEST_START{Görev Başlat?}
    QUEST_START --> |Evet| QUEST_ACTIVE
    QUEST_START --> |Hayır| FREE_ROAM
    
    QUEST_ACTIVE --> QUEST_COMPLETE{Görev Tamamlandı?}
    QUEST_COMPLETE --> |Evet| NEXT_QUEST{Sonraki Görev?}
    QUEST_COMPLETE --> |Hayır| QUEST_ACTIVE
    
    NEXT_QUEST --> |Evet| QUEST_ACTIVE
    NEXT_QUEST --> |Hayır| GAME_END([Oyun Sonu])
```

---

## 🎯 Görev Sistemi (Quest System) Flowchart

```mermaid
flowchart TD
    subgraph "Quest System Flow"
        QS_START([Quest System Başlat]) --> QS_INIT[Singleton Initialize]
        QS_INIT --> QS_WAIT[Diyalog Bekle]
        
        QS_WAIT --> |"Diyalog Tamamlandı"| QS_TYPE{Görev Tipi?}
        
        QS_TYPE --> |"Arrest Quest"| AQ[StartArrestQuest]
        QS_TYPE --> |"Collect Quest"| CQ[StartCollectQuest]
        QS_TYPE --> |"Bot Kill Quest"| BKQ[StartQuest2BotKill]
        
        AQ --> AQ_MARKER[Marker Oluştur]
        AQ_MARKER --> AQ_TRACK[NPC Takip Et]
        AQ_TRACK --> AQ_ARREST{NPC Tutuklandı?}
        AQ_ARREST --> |Hayır| AQ_TRACK
        AQ_ARREST --> |Evet| AQ_ESCORT[Eskort Başlat]
        AQ_ESCORT --> AQ_DELIVER{Teslim Edildi?}
        AQ_DELIVER --> |Hayır| AQ_ESCORT
        AQ_DELIVER --> |Evet| QS_COMPLETE
        
        CQ --> CQ_MARKER[Item Marker Oluştur]
        CQ_MARKER --> CQ_COLLECT{Item Toplandı?}
        CQ_COLLECT --> |Hayır| CQ_COLLECT
        CQ_COLLECT --> |Evet| CQ_CHECK{Tüm Itemlar?}
        CQ_CHECK --> |Hayır| CQ_COLLECT
        CQ_CHECK --> |Evet| QS_COMPLETE
        
        BKQ --> BKQ_MARKER[Bot Marker Oluştur]
        BKQ_MARKER --> BKQ_KILL{Bot Öldürüldü?}
        BKQ_KILL --> |Hayır| BKQ_KILL
        BKQ_KILL --> |Evet| QS_COMPLETE
        
        QS_COMPLETE[CompleteQuest] --> QS_REWARD[Ödül Ver]
        QS_REWARD --> QS_NEXT{Sonraki Görev?}
        QS_NEXT --> |Evet| QS_WAIT
        QS_NEXT --> |Hayır| QS_END([Tüm Görevler Tamamlandı])
    end
```

---

## 💬 Diyalog Sistemi (Dialogue System) Flowchart

```mermaid
flowchart TD
    subgraph "Dialogue System Flow"
        DS_START([Diyalog Başlat]) --> DS_CHECK{NPC Yakın mı?}
        
        DS_CHECK --> |Hayır| DS_END([Diyalog Yok])
        DS_CHECK --> |Evet| DS_INTERACT["E Tuşuna Bas"]
        
        DS_INTERACT --> DS_INIT[DialogueManager.StartDialogue]
        DS_INIT --> DS_LOCK[Player Controls Lock]
        DS_LOCK --> DS_SHOW[ShowDialogueNode]
        
        DS_SHOW --> DS_TYPE{Typewriter Effect?}
        DS_TYPE --> |Evet| DS_TYPEWRITER[Yazı Animasyonu]
        DS_TYPE --> |Hayır| DS_TEXT[Metin Göster]
        
        DS_TYPEWRITER --> DS_CHOICES
        DS_TEXT --> DS_CHOICES
        
        DS_CHOICES{Seçenek Var mı?}
        DS_CHOICES --> |Evet| DS_SHOW_CHOICES[ShowChoices]
        DS_CHOICES --> |Hayır| DS_CONTINUE[Devam Et]
        
        DS_SHOW_CHOICES --> DS_SELECT[Seçim Yap]
        DS_SELECT --> DS_NEXT_NODE[GoToNextNode]
        
        DS_CONTINUE --> DS_NEXT_NODE
        
        DS_NEXT_NODE --> DS_CHECK_END{Diyalog Bitti?}
        DS_CHECK_END --> |Hayır| DS_SHOW
        DS_CHECK_END --> |Evet| DS_END_DIALOGUE[EndDialogue]
        
        DS_END_DIALOGUE --> DS_UNLOCK[Player Controls Unlock]
        DS_UNLOCK --> DS_QUEST{Görev Başlat?}
        DS_QUEST --> |Evet| DS_START_QUEST[StartQuestFromDialogue]
        DS_QUEST --> |Hayır| DS_FINISH([Diyalog Tamamlandı])
        DS_START_QUEST --> DS_FINISH
    end
```

---

## 🤖 Bot AI Sistemi Flowchart

```mermaid
flowchart TD
    subgraph "Bot AI System"
        AI_START([Bot AI Start]) --> AI_INIT[Initialize References]
        AI_INIT --> AI_FIND_PLAYER[FindPlayer]
        AI_FIND_PLAYER --> AI_FIND_WEAPON[FindWeapon]
        
        AI_FIND_WEAPON --> AI_UPDATE[Update Loop]
        
        AI_UPDATE --> AI_PLAYER_CHECK{Player Bulundu?}
        AI_PLAYER_CHECK --> |Hayır| AI_IDLE[Idle State]
        AI_IDLE --> AI_UPDATE
        
        AI_PLAYER_CHECK --> |Evet| AI_RANGE{Menzilde mi?}
        AI_RANGE --> |Hayır| AI_CHASE[Player'a Yaklaş]
        AI_CHASE --> AI_UPDATE
        
        AI_RANGE --> |Evet| AI_LOS{Line of Sight?}
        AI_LOS --> |Hayır| AI_SEEK[Player'ı Ara]
        AI_SEEK --> AI_UPDATE
        
        AI_LOS --> |Evet| AI_AIM[LookAtPlayer]
        AI_AIM --> AI_FIRE[TryShoot]
        AI_FIRE --> AI_UPDATE
    end
```

---

## 🚗 Eskort ve Teslim Sistemi Flowchart

```mermaid
flowchart TD
    subgraph "Vehicle Escort & Delivery System"
        VE_START([Eskort Başlat]) --> VE_ARREST[NPC Tutukla]
        
        VE_ARREST --> VE_FOLLOW[NPC Takip Ediyor]
        VE_FOLLOW --> VE_VEHICLE{Araca Ulaşıldı?}
        
        VE_VEHICLE --> |Hayır| VE_FOLLOW
        VE_VEHICLE --> |Evet| VE_ZONE[Delivery Zone'a Gir]
        
        VE_ZONE --> VE_TRIGGER[OnTriggerEnter]
        VE_TRIGGER --> VE_PROMPT["F Tuşuna Bas Göster"]
        
        VE_PROMPT --> VE_DELIVER{F Tuşuna Basıldı?}
        VE_DELIVER --> |Hayır| VE_WAIT[Bekle]
        VE_WAIT --> VE_DELIVER
        
        VE_DELIVER --> |Evet| VE_PROCESS[DeliverSuspect]
        VE_PROCESS --> VE_STOP[StopEscort]
        VE_STOP --> VE_COMPLETE_QUEST[Quest Complete]
        VE_COMPLETE_QUEST --> VE_SCREEN[Başarı Ekranı]
        VE_SCREEN --> VE_DESTROY[NPC Destroy]
        VE_DESTROY --> VE_NEXT{Sonraki Göreve Geç?}
        VE_NEXT --> |Evet| VE_NEXT_QUEST[OnQuestCompleted]
        VE_NEXT --> |Hayır| VE_END([Teslim Tamamlandı])
        VE_NEXT_QUEST --> VE_END
    end
```

---

## 🎵 Ses Sistemi (Sound System) Diyagramı

```mermaid
flowchart LR
    subgraph "Sound Manager Singleton"
        SM_INIT[SoundManager Instance] --> SM_MUSIC[Background Music]
        SM_INIT --> SM_RAGE[Rage Music]
        
        SM_MUSIC --> SM_PLAY[PlayBackgroundMusic]
        SM_MUSIC --> SM_STOP[StopBackgroundMusic]
        SM_MUSIC --> SM_PAUSE[PauseBackgroundMusic]
        SM_MUSIC --> SM_RESUME[ResumeBackgroundMusic]
        SM_MUSIC --> SM_VOLUME[SetMusicVolume]
        
        SM_RAGE --> SM_PLAY_RAGE[PlayRageMusic]
        SM_RAGE --> SM_STOP_RAGE[StopRageMusic]
        SM_RAGE --> SM_CHECK_RAGE[IsRageMusicPlaying]
    end
    
    COMBAT[Combat Trigger] --> SM_PLAY_RAGE
    PEACE[Peace State] --> SM_STOP_RAGE
    MENU[Menu] --> SM_PAUSE
    GAMEPLAY[Gameplay] --> SM_RESUME
```

---

## 🎮 Player Controller Yapısı

```mermaid
classDiagram
    class PlayerMain {
        +Awake()
        +Start()
        +Update()
        +SetCombatMode(bool)
        +DrawWeapons()
        +HolsterWeapons()
        +SpawnWeapon(int)
        +AddWeapon(WeaponSlot)
        +RemoveWeapon(int)
    }
    
    class PlayerEscortController {
        +IsEscortingTarget: bool
        +CurrentEscortTarget: NPC
        +StartEscort(NPC)
        +StopEscort()
    }
    
    class WeaponSlot {
        +weaponPrefab: GameObject
        +weaponName: string
        +isUnlocked: bool
    }
    
    PlayerMain --> WeaponSlot: manages
    PlayerMain --> PlayerEscortController: references
    PlayerEscortController --> NPC: escorts
```

---

## 📁 Proje Klasör Yapısı

```mermaid
graph LR
    subgraph "Assets/Scripts/"
        subgraph "Character/"
            C1[BotAI.cs]
            C2[DialogueManager.cs]
            C3[DialogueNPC.cs]
            C4[NPC.cs]
            C5[PlayerEscortController.cs]
            C6[PlayerMain.cs]
        end
        
        subgraph "Gameplay/"
            G1[QuestSystem.cs]
            G2[Quest.cs]
            G3[QuestMarker.cs]
            G4[VehicleEscortDelivery.cs]
            G5[CollectibleItem.cs]
        end
        
        subgraph "Sound/"
            S1[SoundManager.cs]
        end
        
        subgraph "UI/"
            U1[MainMenuManager.cs]
            U2[CinematicCamera.cs]
            U3[CameraEffects.cs]
            U4[QuestObjectiveUI.cs]
        end
        
        subgraph "Editor/"
            E1[DialogueDataEditor.cs]
            E2[CinematicCameraEditor.cs]
        end
    end
```

---

## 🔗 Sistem Bağımlılıkları (Dependencies)

```mermaid
graph TD
    subgraph "Core Dependencies"
        UNITY[Unity Engine 6000.1.3f1]
        URP[URP 17.2.0]
        INPUT[Input System 1.14.2]
        NAV[AI Navigation 2.0.9]
        TIMELINE[Timeline 1.8.9]
    end
    
    subgraph "Third Party"
        AKILA[Akila FPS Framework]
        NEWTONSOFT[Newtonsoft JSON 3.2.1]
    end
    
    subgraph "Game Systems"
        PLAYER[PlayerMain]
        AI[BotAI]
        QUEST[QuestSystem]
        DIALOGUE[DialogueManager]
    end
    
    UNITY --> PLAYER
    UNITY --> AI
    UNITY --> QUEST
    UNITY --> DIALOGUE
    
    URP --> PLAYER
    INPUT --> PLAYER
    NAV --> AI
    TIMELINE --> DIALOGUE
    
    AKILA --> PLAYER
    AKILA --> AI
    NEWTONSOFT --> QUEST
```

---

## 🎯 Görev Tipleri ve Durumları

```mermaid
stateDiagram-v2
    [*] --> Inactive: Görev Başlamadı
    
    Inactive --> Active: StartQuest()
    
    state Active {
        [*] --> InProgress
        InProgress --> Tracking: Marker Aktif
        Tracking --> ObjectiveComplete: Hedef Tamamlandı
        ObjectiveComplete --> [*]
    }
    
    Active --> Completed: CompleteQuest()
    Active --> Failed: Görev Başarısız
    
    Completed --> [*]: Ödül Verildi
    Failed --> [*]: Görev Sıfırlandı
```

---

## 📊 NPC State Machine

```mermaid
stateDiagram-v2
    [*] --> Idle: Başlangıç
    
    Idle --> Dialogue: Player Yaklaştı
    Dialogue --> Idle: Diyalog Bitti
    
    Idle --> Alert: Tehdit Algılandı
    Alert --> Combat: Saldırı Modu
    Combat --> Alert: Tehdit Uzaklaştı
    Alert --> Idle: Tehdit Yok
    
    Idle --> Arrested: Tutuklama
    Arrested --> Escorted: Eskort Başladı
    Escorted --> Delivered: Teslim Edildi
    Delivered --> [*]: NPC Kaldırıldı
    
    Combat --> Dead: Öldürüldü
    Dead --> [*]
```

---

## 🎬 Sinematik Kamera Sistemi

```mermaid
flowchart TD
    subgraph "Cinematic Camera Flow"
        CC_START([Kamera Başlat]) --> CC_INIT[Waypoint'leri Yükle]
        CC_INIT --> CC_POS[İlk Pozisyona Git]
        CC_POS --> CC_ROT[Başlangıç Rotasyonu]
        
        CC_ROT --> CC_LOOP[Update Loop]
        CC_LOOP --> CC_MOVE[MoveTowardsWaypoint]
        CC_MOVE --> CC_LOOK[RotateTowardsTarget]
        
        CC_LOOK --> CC_CHECK{Waypoint'e Ulaşıldı?}
        CC_CHECK --> |Hayır| CC_LOOP
        CC_CHECK --> |Evet| CC_NEXT{Son Waypoint?}
        
        CC_NEXT --> |Hayır| CC_INCREMENT[currentWaypointIndex++]
        CC_INCREMENT --> CC_LOOP
        
        CC_NEXT --> |Evet| CC_LOOP_CHECK{Loop Aktif?}
        CC_LOOP_CHECK --> |Evet| CC_WAIT[Wait Timer]
        CC_WAIT --> CC_RESET[Index Sıfırla]
        CC_RESET --> CC_LOOP
        
        CC_LOOP_CHECK --> |Hayır| CC_END([Sinematik Bitti])
    end
```

---

## 📋 Script Dosyaları Özeti

| Kategori | Dosya | Açıklama |
|----------|-------|----------|
| **Character** | `PlayerMain.cs` | Ana oyuncu controller |
| **Character** | `BotAI.cs` | Düşman AI sistemi |
| **Character** | `NPC.cs` | NPC temel sınıfı |
| **Character** | `DialogueManager.cs` | Diyalog yönetimi |
| **Character** | `DialogueNPC.cs` | Diyalog yapan NPC |
| **Character** | `PlayerEscortController.cs` | Eskort sistemi |
| **Gameplay** | `QuestSystem.cs` | Görev yönetimi |
| **Gameplay** | `Quest.cs` | Görev veri yapısı |
| **Gameplay** | `QuestMarker.cs` | Görev işaretleyici |
| **Gameplay** | `VehicleEscortDelivery.cs` | Araç teslim sistemi |
| **Gameplay** | `CollectibleItem.cs` | Toplanabilir item |
| **Sound** | `SoundManager.cs` | Ses yönetimi (Singleton) |
| **UI** | `MainMenuManager.cs` | Ana menü yönetimi |
| **UI** | `CinematicCamera.cs` | Sinematik kamera |
| **UI** | `CameraEffects.cs` | Kamera efektleri |
| **UI** | `QuestObjectiveUI.cs` | Görev UI |

---

## 🔧 Framework Özellikleri

### Singleton Pattern Kullanımı
- `QuestSystem.Instance` - Görev sistemi
- `DialogueManager.Instance` - Diyalog yönetimi
- `SoundManager.Instance` - Ses yönetimi

### Event-Driven Mimari
- Görev tamamlama eventleri
- Diyalog başlangıç/bitiş eventleri
- NPC durum değişikliği eventleri

### Component-Based Design
- Modüler script yapısı
- Tekrar kullanılabilir bileşenler
- Bağımsız sistemler

---

<div align="center">

**Guard Simulator - Modüler ve Ölçeklenebilir Oyun Mimarisi** 🏗️

</div>
