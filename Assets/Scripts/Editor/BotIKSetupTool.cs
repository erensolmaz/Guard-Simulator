using UnityEngine;
using UnityEditor;
using GuardSimulator.Character;

/// <summary>
/// Bot karakterlerinin IK target'larını oluşturup ayarlayan Editor tool
/// </summary>
public class BotIKSetupTool : EditorWindow
{
    private BotWeaponManager targetBotWeaponManager;
    private Transform weaponTransform;
    private Vector2 scrollPosition;
    
    [MenuItem("Tools/Bot IK Setup Tool")]
    public static void ShowWindow()
    {
        GetWindow<BotIKSetupTool>("Bot IK Setup");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Bot IK Setup Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "Bu tool, bot karakterlerinin IK target'larını oluşturup ayarlar.\n\n" +
            "1. Bot karakterinizi seçin\n" +
            "2. Silahınızı seçin veya Pistol_bot child'ını bulun\n" +
            "3. 'IK Target'ları Oluştur' butonuna tıklayın\n" +
            "4. Hierarchy'de IK_LeftHand ve IK_RightHand'ı bulup pozisyonlarını ayarlayın\n" +
            "5. Inspector'da BotWeaponManager'daki IK Position ve Rotation değerlerini güncelleyin",
            MessageType.Info
        );
        
        EditorGUILayout.Space(10);
        
        // Seçili objeden BotWeaponManager al
        if (Selection.activeGameObject != null)
        {
            BotWeaponManager selected = Selection.activeGameObject.GetComponent<BotWeaponManager>();
            if (selected != null && targetBotWeaponManager != selected)
            {
                targetBotWeaponManager = selected;
            }
        }
        
        targetBotWeaponManager = (BotWeaponManager)EditorGUILayout.ObjectField(
            "Target Bot Weapon Manager",
            targetBotWeaponManager,
            typeof(BotWeaponManager),
            true
        );
        
        EditorGUILayout.Space(10);
        
        // Silah transform'u seç
        weaponTransform = (Transform)EditorGUILayout.ObjectField(
            "Weapon Transform (Pistol_bot veya silah)",
            weaponTransform,
            typeof(Transform),
            true
        );
        
        EditorGUILayout.Space(10);
        
        // Otomatik silah bul
        if (targetBotWeaponManager != null)
        {
            if (GUILayout.Button("Pistol_bot'u Otomatik Bul"))
            {
                FindPistolBot();
            }
            
            EditorGUILayout.Space(5);
            
            // IK Target'ları oluştur
            GUI.enabled = (weaponTransform != null && targetBotWeaponManager != null);
            if (GUILayout.Button("IK Target'ları Oluştur/Güncelle", GUILayout.Height(30)))
            {
                CreateIKTargets();
            }
            GUI.enabled = true;
            
            EditorGUILayout.Space(10);
            
            // Mevcut IK target pozisyonlarını göster
            if (targetBotWeaponManager != null)
            {
                EditorGUILayout.LabelField("Mevcut IK Ayarları", EditorStyles.boldLabel);
                
                SerializedObject serializedObject = new SerializedObject(targetBotWeaponManager);
                SerializedProperty leftHandPos = serializedObject.FindProperty("leftHandIKPosition");
                SerializedProperty rightHandPos = serializedObject.FindProperty("rightHandIKPosition");
                SerializedProperty leftHandRot = serializedObject.FindProperty("leftHandIKRotation");
                SerializedProperty rightHandRot = serializedObject.FindProperty("rightHandIKRotation");
                
                EditorGUILayout.PropertyField(leftHandPos, new GUIContent("Sol El IK Position"));
                EditorGUILayout.PropertyField(rightHandPos, new GUIContent("Sağ El IK Position"));
                EditorGUILayout.PropertyField(leftHandRot, new GUIContent("Sol El IK Rotation"));
                EditorGUILayout.PropertyField(rightHandRot, new GUIContent("Sağ El IK Rotation"));
                
                serializedObject.ApplyModifiedProperties();
            }
            
            EditorGUILayout.Space(10);
            
            // Hierarchy'deki IK target'ları seç
            if (weaponTransform != null)
            {
                EditorGUILayout.LabelField("Hierarchy'deki IK Target'lar", EditorStyles.boldLabel);
                
                Transform leftHandIK = weaponTransform.Find("IK_LeftHand");
                Transform rightHandIK = weaponTransform.Find("IK_RightHand");
                
                if (leftHandIK != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.ObjectField("IK_LeftHand", leftHandIK, typeof(Transform), true);
                    if (GUILayout.Button("Seç", GUILayout.Width(50)))
                    {
                        Selection.activeTransform = leftHandIK;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.HelpBox("IK_LeftHand bulunamadı. 'IK Target'ları Oluştur' butonuna tıklayın.", MessageType.Warning);
                }
                
                if (rightHandIK != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.ObjectField("IK_RightHand", rightHandIK, typeof(Transform), true);
                    if (GUILayout.Button("Seç", GUILayout.Width(50)))
                    {
                        Selection.activeTransform = rightHandIK;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.HelpBox("IK_RightHand bulunamadı. 'IK Target'ları Oluştur' butonuna tıklayın.", MessageType.Warning);
                }
                
                EditorGUILayout.Space(10);
                
                // IK target pozisyonlarını Inspector'dan al
                if (leftHandIK != null && rightHandIK != null)
                {
                    if (GUILayout.Button("Mevcut IK Target Pozisyonlarını Inspector'a Kopyala"))
                    {
                        CopyIKTargetPositionsToInspector(leftHandIK, rightHandIK);
                    }
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Lütfen bir BotWeaponManager component'i seçin.", MessageType.Warning);
        }
    }
    
    private void FindPistolBot()
    {
        if (targetBotWeaponManager == null) return;
        
        Transform botTransform = targetBotWeaponManager.transform;
        Transform pistolBot = botTransform.Find("Pistol_bot");
        
        if (pistolBot != null)
        {
            weaponTransform = pistolBot;
            
            // Pistol_bot içindeki silahı bul
            foreach (Transform child in pistolBot)
            {
                if (child.name.Contains("Pistol") || child.GetComponent<Akila.FPSFramework.Firearm>() != null)
                {
                    weaponTransform = child;
                    break;
                }
            }
            
            EditorUtility.DisplayDialog("Başarılı", $"Pistol_bot bulundu: {weaponTransform.name}", "Tamam");
        }
        else
        {
            EditorUtility.DisplayDialog("Hata", "Pistol_bot bulunamadı! Önce oyunu çalıştırıp silahı ekleyin.", "Tamam");
        }
    }
    
    private void CreateIKTargets()
    {
        if (targetBotWeaponManager == null || weaponTransform == null)
        {
            EditorUtility.DisplayDialog("Hata", "BotWeaponManager ve Weapon Transform gerekli!", "Tamam");
            return;
        }
        
        // Reflection ile SetupIKTargets metodunu çağır
        var method = typeof(BotWeaponManager).GetMethod("SetupIKTargets", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        
        if (method != null)
        {
            method.Invoke(targetBotWeaponManager, new object[] { weaponTransform });
            EditorUtility.DisplayDialog("Başarılı", "IK Target'ları oluşturuldu/güncellendi!", "Tamam");
            EditorUtility.SetDirty(targetBotWeaponManager);
        }
        else
        {
            EditorUtility.DisplayDialog("Hata", "SetupIKTargets metodu bulunamadı!", "Tamam");
        }
    }
    
    private void CopyIKTargetPositionsToInspector(Transform leftHandIK, Transform rightHandIK)
    {
        if (targetBotWeaponManager == null) return;
        
        SerializedObject serializedObject = new SerializedObject(targetBotWeaponManager);
        
        // Sol el pozisyon ve rotasyon
        SerializedProperty leftPos = serializedObject.FindProperty("leftHandIKPosition");
        SerializedProperty leftRot = serializedObject.FindProperty("leftHandIKRotation");
        
        leftPos.vector3Value = leftHandIK.localPosition;
        leftRot.vector3Value = leftHandIK.localEulerAngles;
        
        // Sağ el pozisyon ve rotasyon
        SerializedProperty rightPos = serializedObject.FindProperty("rightHandIKPosition");
        SerializedProperty rightRot = serializedObject.FindProperty("rightHandIKRotation");
        
        rightPos.vector3Value = rightHandIK.localPosition;
        rightRot.vector3Value = rightHandIK.localEulerAngles;
        
        serializedObject.ApplyModifiedProperties();
        
        EditorUtility.DisplayDialog("Başarılı", "IK pozisyonları Inspector'a kopyalandı!", "Tamam");
    }
}

