using UnityEngine;

[System.Serializable]
public class DialogueNode
{
    [Tooltip("Node numarası (sadece görsel, otomatik atanır)")]
    public int nodeNumber;
    
    [TextArea(2, 4)]
    public string npcText;
    
    [Tooltip("Açılırsa, NPC text'i gösterildikten sonra seçenekler gösterilmeden diyalog otomatik olarak kapanır. Delay süresi DialogueData'dan alınır.")]
    public bool quitAfterText = false;
    
    public DialogueChoice[] choices;
}

[System.Serializable]
public class DialogueChoice
{
    [TextArea(1, 2)]
    public string choiceText;
    
    [Tooltip("Bu seçenek seçildiğinde hangi node numarasına geçilecek? -1 ise diyalog biter.")]
    public int nextNodeID = -1; // -1 = diyalog biter
    
    [Tooltip("Açılırsa, seçenek seçildikten sonra belirli bir süre bekleyip diyalog otomatik olarak biter. nextNodeID göz ardı edilir. Delay süresi DialogueData'dan alınır.")]
    public bool autoEndDialogue = false;
    
    [Tooltip("Bu seçenek seçildiğinde NPC tutuklanacak mı? (Tutuklama hem teslim olma hem de tutuklama işlemini içerir)")]
    public bool triggerArrest = false;
    
    [Tooltip("Bu seçenek seçildiğinde NPC rage moduna geçecek mi? (Rage modunda NPC saldırıya geçer)")]
    public bool triggerRage = false;
    
    [Tooltip("Bu seçenek seçildiğinde görev başlatılacak mı? (Diyalog tamamlandıktan sonra)")]
    public bool triggerQuest = false;
    
    [Tooltip("Bu seçenek seçildiğinde item toplama görevi başlatılacak mı?")]
    public bool triggerCollectQuest = false;
}

[CreateAssetMenu(fileName = "New Dialogue", menuName = "Dialogue System/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [Header("Settings")]
    [Tooltip("autoEndDialogue aktif olan seçenekler için kaç saniye sonra diyalog bitecek?")]
    public float endDialogueDelay = 2f;
    
    [Header("Dialogue Nodes")]
    [Tooltip("Diyalog node'ları. İlk node (index 0) başlangıç diyaloğudur.")]
    public DialogueNode[] dialogueNodes;
    
    /// <summary>
    /// Başlangıç node'unu döndür (index 0)
    /// </summary>
    public DialogueNode GetStartNode()
    {
        if (dialogueNodes != null && dialogueNodes.Length > 0)
            return dialogueNodes[0];
        return null;
    }
    
    /// <summary>
    /// ID'ye göre node bul
    /// </summary>
    public DialogueNode GetNodeByID(int nodeID)
    {
        if (dialogueNodes != null && nodeID >= 0 && nodeID < dialogueNodes.Length)
            return dialogueNodes[nodeID];
        return null;
    }
}
