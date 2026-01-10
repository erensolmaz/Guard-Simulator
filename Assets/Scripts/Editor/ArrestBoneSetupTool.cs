using UnityEngine;
using UnityEditor;
using GuardSimulator.Character;

/// <summary>
/// NPC karakterlerinin tutuklama sistemi için kemikleri otomatik bulan Editor tool
/// </summary>
public class ArrestBoneSetupTool : EditorWindow
{
    private NPC targetNPC;
    private Vector2 scrollPosition;
    
    [MenuItem("Tools/Arrest Bone Setup Tool")]
    public static void ShowWindow()
    {
        GetWindow<ArrestBoneSetupTool>("Arrest Bone Setup");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Arrest Bone Setup Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "Bu tool, NPC karakterlerinin tutuklama sistemi için gerekli kemikleri otomatik olarak bulur ve ayarlar.\n\n" +
            "Man_Full model yapısı: root > pelvis > spine_01 > spine_02 > spine_03 > clavicle_l/clavicle_r",
            MessageType.Info
        );
        
        EditorGUILayout.Space(10);
        
        // Seçili objeden NPC al
        if (Selection.activeGameObject != null)
        {
            NPC selectedNPC = Selection.activeGameObject.GetComponent<NPC>();
            if (selectedNPC != null && targetNPC != selectedNPC)
            {
                targetNPC = selectedNPC;
            }
        }
        
        targetNPC = (NPC)EditorGUILayout.ObjectField(
            "Target NPC",
            targetNPC,
            typeof(NPC),
            true
        );
        
        EditorGUILayout.Space(10);
        
        if (targetNPC == null)
        {
            EditorGUILayout.HelpBox("Lütfen bir NPC seçin veya sürükleyip bırakın.", MessageType.Warning);
            return;
        }
        
        // Kemik bulma butonu
        if (GUILayout.Button("Kemikleri Otomatik Bul", GUILayout.Height(30)))
        {
            FindBones();
        }
        
        EditorGUILayout.Space(10);
        
        // IK Test butonu (Play modunda)
        if (Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Play modunda - IK sistemi test edilebilir", MessageType.Info);
            if (GUILayout.Button("IK Sistemini Test Et (Tutuklama)", GUILayout.Height(30)))
            {
                TestIKSystem();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("IK testi için Play moduna geçin", MessageType.Warning);
        }
        
        EditorGUILayout.Space(10);
        
        // Kemik referanslarını göster
        if (targetNPC != null)
        {
            ShowBoneReferences();
        }
    }
    
    private void FindBones()
    {
        if (targetNPC == null) return;
        
        // NPC'nin AutoFindBones metodunu çağır (reflection ile)
        System.Reflection.MethodInfo method = typeof(NPC).GetMethod(
            "AutoFindBones",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance
        );
        
        if (method != null)
        {
            method.Invoke(targetNPC, null);
            EditorUtility.SetDirty(targetNPC);
            Debug.Log($"[ArrestBoneSetupTool] {targetNPC.NPCName} için kemikler bulundu!");
            EditorUtility.DisplayDialog(
                "Başarılı",
                $"{targetNPC.NPCName} için kemikler bulundu!\n\nKonsolu kontrol edin.",
                "OK"
            );
        }
        else
        {
            Debug.LogError("[ArrestBoneSetupTool] AutoFindBones metodu bulunamadı!");
            EditorUtility.DisplayDialog(
                "Hata",
                "AutoFindBones metodu bulunamadı! NPC.cs dosyasını kontrol edin.",
                "OK"
            );
        }
    }
    
    private void ShowBoneReferences()
    {
        EditorGUILayout.LabelField("Kemik Referansları", EditorStyles.boldLabel);
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        // Reflection ile arrestBones field'ını al
        System.Reflection.FieldInfo field = typeof(NPC).GetField(
            "arrestBones",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );
        
        if (field != null)
        {
            object arrestBones = field.GetValue(targetNPC);
            System.Type bonesType = arrestBones.GetType();
            
            // bonesFound
            System.Reflection.FieldInfo bonesFoundField = bonesType.GetField("bonesFound");
            bool bonesFound = (bool)bonesFoundField.GetValue(arrestBones);
            
            EditorGUILayout.LabelField("Durum:", bonesFound ? "✅ Kemikler Bulundu" : "❌ Kemikler Bulunamadı");
            EditorGUILayout.Space(5);
            
            // Tüm kemikleri göster
            string[] boneNames = {
                "pelvis", "spine_03", "leftClavicle", "rightClavicle",
                "leftUpperArm", "rightUpperArm",
                "leftLowerArm", "rightLowerArm",
                "leftHand", "rightHand"
            };
            
            foreach (string boneName in boneNames)
            {
                System.Reflection.FieldInfo boneField = bonesType.GetField(boneName);
                if (boneField != null)
                {
                    Transform bone = (Transform)boneField.GetValue(arrestBones);
                    EditorGUILayout.ObjectField(
                        boneName,
                        bone,
                        typeof(Transform),
                        true
                    );
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("arrestBones field'ı bulunamadı!", MessageType.Warning);
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private void OnSelectionChange()
    {
        Repaint();
    }
    
    private void TestIKSystem()
    {
        if (targetNPC == null) return;
        
        // NPC'nin Arrest metodunu çağır
        System.Reflection.MethodInfo method = typeof(NPC).GetMethod(
            "Arrest",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance
        );
        
        if (method != null)
        {
            method.Invoke(targetNPC, null);
            Debug.Log($"[ArrestBoneSetupTool] {targetNPC.NPCName} için tutuklama testi başlatıldı!");
        }
        else
        {
            Debug.LogError("[ArrestBoneSetupTool] Arrest metodu bulunamadı!");
        }
    }
}

