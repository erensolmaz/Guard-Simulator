using UnityEngine;
using System.Collections.Generic;
using Akila.FPSFramework;
using System.Linq;
using GuardSimulator.Character;

[System.Serializable]
public class WeaponSlot
{
    [Tooltip("Silah adı")]
    public string weaponName;
    
    [Tooltip("Silah prefab'ı")]
    public GameObject weaponPrefab;
    
    [Tooltip("Oyun başında bu silah açık mı?")]
    public bool isUnlocked = true;
}

public class PlayerMain : MonoBehaviour
{
    public static PlayerMain Instance { get; private set; }
    
    [Header("Combat Mode Settings")]
    [Tooltip("Saldırı modu aktif mi?")]
    [SerializeField] private bool isCombatModeActive = false;
    
    [Tooltip("Saldırı modunu açıp kapatmak için kullanılacak tuş")]
    [SerializeField] private KeyCode combatModeKey = KeyCode.CapsLock;
    
    [Header("Inventory Settings")]
    [Tooltip("Oyuncunun sahip olduğu silahlar")]
    [SerializeField] private List<WeaponSlot> weapons = new List<WeaponSlot>();
    
    [Tooltip("Silahların spawn edileceği parent transform (genellikle Player'ın hand/weapon holder transform'u)")]
    [SerializeField] private Transform weaponParent;
    
    [Tooltip("Saldırı modu açıldığında silah çekme animasyonu için bekleme süresi (saniye)")]
    [SerializeField] private float weaponDrawDelay = 0.5f;
    
    [Tooltip("Saldırı modu kapatıldığında silah indirme animasyonu için bekleme süresi (saniye)")]
    [SerializeField] private float weaponHolsterDelay = 0.5f;
    
    // Akila Framework referansları
    private MonoBehaviour characterInput;
    private MonoBehaviour firstPersonController;
    private MonoBehaviour weaponManager;
    private MonoBehaviour leanController;
    private Inventory playerInventory;
    
    // Spawn edilmiş silahlar
    private Dictionary<string, GameObject> spawnedWeapons = new Dictionary<string, GameObject>();
    
    // Q ve E tuşlarını engellemek için
    private bool blockQInput = false;
    private bool blockEInput = false;
    
    // Silah çekme/indirme işlemi devam ediyor mu?
    private bool isWeaponTransitioning = false;
    
    // Lean input log mesajını sadece bir kez göstermek için
    private bool leanInputLogShown = false;
    
    // Events
    public System.Action<bool> OnCombatModeChanged;
    
    // Public Properties
    public bool IsCombatModeActive => isCombatModeActive;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("[PlayerMain] Birden fazla PlayerMain instance bulundu! Sadece bir tane olmalı.");
        }
    }
    
    private void Start()
    {
        // Akila Framework component'lerini bul
        FindAkilaComponents();
        
        // Player'ın Inventory component'ini bul
        playerInventory = GetComponentInChildren<Inventory>();
        if (playerInventory == null)
        {
            playerInventory = GetComponent<Inventory>();
        }
        
        // Debug: CharacterInput'un yapısını yazdır
        DebugCharacterInputStructure();
        
        // Başlangıçta saldırı modu kapalı
        SetCombatMode(false);
        
        // Oyun başında silahları spawn ETME - sadece saldırı modu açıldığında spawn edilecek
    }
    
    private void Update()
    {
        // Capslock tuşuna basıldığında saldırı modunu aç/kapat
        if (Input.GetKeyDown(combatModeKey))
        {
            ToggleCombatMode();
        }
        
        // Saldırı modu kapalıyken Q ve E tuşlarını engelle
        if (!isCombatModeActive)
        {
            // LeanController'ı her frame kontrol et ve disable et
            if (leanController != null && leanController.enabled)
            {
                leanController.enabled = false;
            }
            
            // CharacterInput'ta lean input backing field'larını her frame false yap
            // Bu, Q ve E tuşlarına basılsa bile lean input'larının false kalmasını sağlar
            if (characterInput != null)
            {
                SetLeanInputEnabled(characterInput, false);
            }
        }
        else
        {
            // Saldırı modu açıkken LeanController'ı aktif et
            if (leanController != null && !leanController.enabled)
            {
                leanController.enabled = true;
            }
            
            // BattleMode açıkken backing field'ları CharacterInput'un normal akışına bırak
            // SetLeanInputEnabled çağrılmayacak, böylece CharacterInput Q/E tuşlarını normal şekilde işleyecek
        }
        
        // Player'ın inventory'sindeki aktif olmayan silahların input'unu engelle
        UpdateFirearmInputStates();
    }
    
    /// <summary>
    /// Player'ın inventory'sindeki aktif olmayan silahların isInputActive property'sini false yap
    /// Böylece player'ın elinde olmayan silahlar sol click ile ateş edemez
    /// Bot silahları bu kontrolden hariçtir (bot input kullanmıyor)
    /// </summary>
    private void UpdateFirearmInputStates()
    {
        if (playerInventory == null) return;
        
        // Player'ın inventory'sindeki aktif item'ı al
        InventoryItem activeItem = playerInventory.currentItem;
        
        // Sahnedeki tüm Firearm'ları bul
        Firearm[] allFirearms = FindObjectsOfType<Firearm>(true);
        
        foreach (Firearm firearm in allFirearms)
        {
            if (firearm == null) continue;
            
            // Bot silahı kontrolü - bot silahları bu kontrolden hariçtir
            BotFirearmController botController = firearm.GetComponent<BotFirearmController>();
            if (botController != null && botController.IsBotWeapon())
            {
                // Bot silahı - isInputActive'i değiştirme (bot input kullanmıyor zaten)
                continue;
            }
            
            // Eğer bu silah player'ın inventory'sindeki aktif item ise, input aktif olsun
            if (activeItem != null && firearm.gameObject == activeItem.gameObject)
            {
                firearm.isInputActive = true;
            }
            // Eğer bu silah player'ın inventory'sinde ama aktif değilse, input'u kapat
            else if (playerInventory.items.Contains(firearm as InventoryItem))
            {
                firearm.isInputActive = false;
            }
            // Eğer bu silah player'ın inventory'sinde değilse (bot silahı veya yerde duran silah), input'u kapat
            else
            {
                firearm.isInputActive = false;
            }
        }
    }
    
    /// <summary>
    /// Akila Framework component'lerini bul
    /// </summary>
    private void FindAkilaComponents()
    {
        MonoBehaviour[] allComponents = GetComponents<MonoBehaviour>();
        
        foreach (MonoBehaviour comp in allComponents)
        {
            string typeName = comp.GetType().Name;
            
            if (typeName == "FirstPersonController")
            {
                firstPersonController = comp;
            }
            else if (typeName == "CharacterInput")
            {
                characterInput = comp;
            }
            else if (typeName == "WeaponManager")
            {
                weaponManager = comp;
            }
            else if (typeName == "LeanController" || typeName.Contains("Lean"))
            {
                leanController = comp;
            }
        }
        
        // Child'larda da ara
        if (leanController == null)
        {
            leanController = GetComponentInChildren<MonoBehaviour>();
            if (leanController != null && !leanController.GetType().Name.Contains("Lean"))
            {
                leanController = null;
            }
        }
        
        Debug.Log($"[PlayerMain] Component'ler bulundu - CharacterInput: {characterInput != null}, " +
                  $"FirstPersonController: {firstPersonController != null}, " +
                  $"WeaponManager: {weaponManager != null}, " +
                  $"LeanController: {leanController != null}");
    }
    
    /// <summary>
    /// Saldırı modunu aç/kapat
    /// </summary>
    public void ToggleCombatMode()
    {
        SetCombatMode(!isCombatModeActive);
    }
    
    /// <summary>
    /// Saldırı modunu ayarla
    /// </summary>
    public void SetCombatMode(bool active)
    {
        if (isCombatModeActive == active) return;
        if (isWeaponTransitioning) return; // Silah çekme/indirme işlemi devam ediyorsa bekle
        
        isCombatModeActive = active;
        
        // Lean Controller'ı enable/disable et (Q ve E için)
        if (leanController != null)
        {
            leanController.enabled = active;
            Debug.Log($"[PlayerMain] LeanController {(active ? "aktif" : "devre dışı")} edildi.");
        }
        
        // CharacterInput'ta Q ve E kontrolünü ayarla (reflection ile)
        if (characterInput != null)
        {
            SetLeanInputEnabled(characterInput, active);
        }
        
        // Silahları çek/indir
        if (active)
        {
            // Saldırı modu açıldı - silahları çek
            StartCoroutine(DrawWeapons());
        }
        else
        {
            // Saldırı modu kapatıldı - silahları indir
            StartCoroutine(HolsterWeapons());
        }
        
        OnCombatModeChanged?.Invoke(isCombatModeActive);
        
        Debug.Log($"[PlayerMain] Saldırı modu: {(isCombatModeActive ? "AÇIK" : "KAPALI")}");
    }
    
    
    /// <summary>
    /// CharacterInput'ta Q ve E (lean) input'larını enable/disable et
    /// </summary>
    private void SetLeanInputEnabled(MonoBehaviour characterInputComp, bool enabled)
    {
        if (characterInputComp == null) return;
        
        try
        {
            System.Type inputType = characterInputComp.GetType();
            bool found = false;
            
            // Backing field'ları bul
            System.Reflection.FieldInfo leanRightBackingField = inputType.GetField("<leanRightInput>k__BackingField", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            
            System.Reflection.FieldInfo leanLeftBackingField = inputType.GetField("<leanLeftInput>k__BackingField", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            
            // Sadece BattleMode kapalıyken backing field'ları false yap
            // BattleMode açıkken CharacterInput'un normal akışına bırak
            if (!enabled)
            {
                if (leanRightBackingField != null && leanRightBackingField.FieldType == typeof(bool))
                {
                    leanRightBackingField.SetValue(characterInputComp, false);
                    found = true;
                }
                
                if (leanLeftBackingField != null && leanLeftBackingField.FieldType == typeof(bool))
                {
                    leanLeftBackingField.SetValue(characterInputComp, false);
                    found = true;
                }
            }
            // BattleMode açıkken backing field'ları CharacterInput'un normal akışına bırak
            // (Update metodunda Q/E tuşlarına göre otomatik set edilecek)
            
            // toggleLean field'ını da kontrol et
            System.Reflection.FieldInfo toggleLeanField = inputType.GetField("toggleLean", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Public | 
                System.Reflection.BindingFlags.Instance);
            
            if (toggleLeanField != null && toggleLeanField.FieldType == typeof(bool))
            {
                // toggleLean'i false yaparak lean'i disable edebiliriz
                if (!enabled)
                {
                    toggleLeanField.SetValue(characterInputComp, false);
                    found = true;
                }
                // BattleMode açıkken toggleLean'i olduğu gibi bırak (CharacterInput'un ayarına göre)
            }
            
            if (found && !enabled && !leanInputLogShown)
            {
                Debug.Log($"[PlayerMain] CharacterInput lean input backing fields devre dışı edildi.");
                leanInputLogShown = true;
            }
            else if (enabled && leanInputLogShown)
            {
                // Eğer tekrar aktif edilirse flag'i sıfırla (isteğe bağlı)
                leanInputLogShown = false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[PlayerMain] CharacterInput'ta lean input kontrolü yapılamadı: {e.Message}");
        }
    }
    
    /// <summary>
    /// Debug: CharacterInput'un yapısını yazdır (sadece ilk başlangıçta)
    /// </summary>
    private void DebugCharacterInputStructure()
    {
        if (characterInput == null) return;
        
        System.Type inputType = characterInput.GetType();
        Debug.Log($"[PlayerMain] CharacterInput Type: {inputType.Name}");
        
        // Tüm property'leri listele (lean ile ilgili olanları)
        System.Reflection.PropertyInfo[] props = inputType.GetProperties();
        foreach (var prop in props)
        {
            string propName = prop.Name.ToLower();
            if (propName.Contains("lean") || propName.Contains("q") || propName.Contains("e"))
            {
                Debug.Log($"[PlayerMain] Property found: {prop.Name} (Type: {prop.PropertyType.Name}, CanWrite: {prop.CanWrite})");
            }
        }
        
        // Tüm field'ları listele (lean ile ilgili olanları)
        System.Reflection.FieldInfo[] fields = inputType.GetFields(
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Public | 
            System.Reflection.BindingFlags.Instance);
        
        foreach (var field in fields)
        {
            string fieldName = field.Name.ToLower();
            if (fieldName.Contains("lean") || fieldName.Contains("q") || fieldName.Contains("e"))
            {
                Debug.Log($"[PlayerMain] Field found: {field.Name} (Type: {field.FieldType.Name})");
            }
        }
    }
    
    /// <summary>
    /// Silahları çek (saldırı modu açıldığında)
    /// </summary>
    private System.Collections.IEnumerator DrawWeapons()
    {
        isWeaponTransitioning = true;
        
        // Animasyon için bekle
        yield return new WaitForSeconds(weaponDrawDelay);
        
        if (weaponParent == null)
        {
            Debug.LogWarning("[PlayerMain] Weapon parent bulunamadı! Silahlar spawn edilemedi.");
            isWeaponTransitioning = false;
            yield break;
        }
        
        // Tüm unlock olan silahları spawn et
        foreach (WeaponSlot slot in weapons)
        {
            if (slot.isUnlocked && slot.weaponPrefab != null)
            {
                SpawnWeapon(slot);
            }
        }
        
        Debug.Log($"[PlayerMain] {spawnedWeapons.Count} silah çekildi.");
        isWeaponTransitioning = false;
    }
    
    /// <summary>
    /// Silahları indir (saldırı modu kapatıldığında)
    /// </summary>
    private System.Collections.IEnumerator HolsterWeapons()
    {
        isWeaponTransitioning = true;
        
        // Animasyon için bekle
        yield return new WaitForSeconds(weaponHolsterDelay);
        
        // Tüm silahları kaldır
        RemoveAllWeapons();
        
        Debug.Log("[PlayerMain] Silahlar indirildi.");
        isWeaponTransitioning = false;
    }
    
    /// <summary>
    /// Belirli bir silahı spawn et
    /// </summary>
    public void SpawnWeapon(WeaponSlot slot)
    {
        if (slot.weaponPrefab == null)
        {
            Debug.LogWarning($"[PlayerMain] {slot.weaponName} için prefab bulunamadı!");
            return;
        }
        
        if (weaponParent == null)
        {
            Debug.LogWarning("[PlayerMain] Weapon parent bulunamadı!");
            return;
        }
        
        // Eğer bu silah zaten spawn edilmişse, tekrar spawn etme
        if (spawnedWeapons.ContainsKey(slot.weaponName))
        {
            Debug.Log($"[PlayerMain] {slot.weaponName} zaten spawn edilmiş.");
            return;
        }
        
        GameObject weaponInstance = Instantiate(slot.weaponPrefab, weaponParent);
        weaponInstance.name = slot.weaponName;
        spawnedWeapons[slot.weaponName] = weaponInstance;
        
        Debug.Log($"[PlayerMain] {slot.weaponName} spawn edildi.");
    }
    
    /// <summary>
    /// Silah ekle (runtime'da)
    /// </summary>
    public void AddWeapon(WeaponSlot slot)
    {
        if (!weapons.Contains(slot))
        {
            weapons.Add(slot);
        }
        
        if (slot.isUnlocked)
        {
            SpawnWeapon(slot);
        }
    }
    
    /// <summary>
    /// Silahı unlock et ve spawn et
    /// </summary>
    public void UnlockWeapon(string weaponName)
    {
        WeaponSlot slot = weapons.Find(w => w.weaponName == weaponName);
        if (slot != null && !slot.isUnlocked)
        {
            slot.isUnlocked = true;
            SpawnWeapon(slot);
        }
    }
    
    /// <summary>
    /// Spawn edilmiş silahı al
    /// </summary>
    public GameObject GetWeapon(string weaponName)
    {
        spawnedWeapons.TryGetValue(weaponName, out GameObject weapon);
        return weapon;
    }
    
    /// <summary>
    /// Tüm spawn edilmiş silahları al
    /// </summary>
    public Dictionary<string, GameObject> GetAllSpawnedWeapons()
    {
        return new Dictionary<string, GameObject>(spawnedWeapons);
    }
    
    /// <summary>
    /// Silahı kaldır (spawn edilmiş silahı destroy et)
    /// </summary>
    public void RemoveWeapon(string weaponName)
    {
        if (spawnedWeapons.TryGetValue(weaponName, out GameObject weapon))
        {
            Destroy(weapon);
            spawnedWeapons.Remove(weaponName);
            Debug.Log($"[PlayerMain] {weaponName} kaldırıldı.");
        }
    }
    
    /// <summary>
    /// Tüm silahları kaldır
    /// </summary>
    private void RemoveAllWeapons()
    {
        List<string> weaponNames = new List<string>(spawnedWeapons.Keys);
        
        foreach (string weaponName in weaponNames)
        {
            RemoveWeapon(weaponName);
        }
        
        spawnedWeapons.Clear();
    }
}

