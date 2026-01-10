using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Collections;

namespace GuardSimulator.UI
{
    public class MatrixRainEffect : MonoBehaviour
    {
        [Header("Character Settings")]
        [Tooltip("Kullanılacak karakter seti")]
        [SerializeField] private CharacterSet characterSet = CharacterSet.Numbers;
        
        [Tooltip("Özel karakter seti (Custom seçilirse)")]
        [SerializeField] private string customCharacters = "0123456789ABCDEF";
        
        [Header("Visual Settings")]
        [Tooltip("Karakter rengi")]
        [SerializeField] private Color characterColor = new Color(0f, 1f, 0f, 0.3f);
        
        [Tooltip("Font boyutu")]
        [SerializeField] private int fontSize = 20;
        
        [Tooltip("Karakter solma efekti")]
        [SerializeField] private bool enableFading = true;
        
        [Tooltip("Solma hızı")]
        [SerializeField] private float fadeSpeed = 1f;
        
        [Header("Column Settings")]
        [Tooltip("Kolon sayısı (yatay)")]
        [SerializeField] private int columnCount = 20;
        
        [Tooltip("Minimum düşme hızı")]
        [SerializeField] private float minSpeed = 50f;
        
        [Tooltip("Maximum düşme hızı")]
        [SerializeField] private float maxSpeed = 150f;
        
        [Tooltip("Kolon başına karakter sayısı")]
        [SerializeField] private int charactersPerColumn = 15;
        
        [Tooltip("Karakterler arası mesafe (Y)")]
        [SerializeField] private float characterSpacing = 25f;
        
        [Header("Spawn Settings")]
        [Tooltip("Yeni kolon spawn aralığı")]
        [SerializeField] private float spawnInterval = 0.5f;
        
        [Tooltip("Başlangıç gecikmesi")]
        [SerializeField] private float startDelay = 0f;
        
        [Tooltip("Canvas enable olunca otomatik başlat")]
        [SerializeField] private bool autoStart = true;
        
        [Header("Advanced Settings")]
        [Tooltip("Karakter değişim hızı (saniye, 0 = değişmez)")]
        [SerializeField] private float characterChangeInterval = 0.1f;
        
        [Tooltip("Random spawn pozisyonları")]
        [SerializeField] private bool randomSpawnPositions = true;
        
        [Tooltip("Pool boyutu")]
        [SerializeField] private int poolSize = 100;
        
        public enum CharacterSet
        {
            Numbers,
            Binary,
            Hexadecimal,
            Katakana,
            Mixed,
            Custom
        }
        
        private RectTransform rectTransform;
        private Canvas parentCanvas;
        private List<MatrixColumn> activeColumns = new List<MatrixColumn>();
        private Queue<TextMeshProUGUI> textPool = new Queue<TextMeshProUGUI>();
        private bool isRunning = false;
        private string currentCharacterSet = "";
        
        private class MatrixColumn
        {
            public List<TextMeshProUGUI> characters = new List<TextMeshProUGUI>();
            public float speed;
            public float xPosition;
            public bool isActive = true;
        }
        
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = gameObject.AddComponent<RectTransform>();
            }
            
            parentCanvas = GetComponentInParent<Canvas>();
            
            SetupCharacterSet();
            InitializePool();
        }
        
        private void OnEnable()
        {
            if (autoStart)
            {
                if (startDelay > 0)
                {
                    Invoke(nameof(StartEffect), startDelay);
                }
                else
                {
                    StartEffect();
                }
            }
        }
        
        private void OnDisable()
        {
            StopEffect();
        }
        
        private void SetupCharacterSet()
        {
            switch (characterSet)
            {
                case CharacterSet.Numbers:
                    currentCharacterSet = "0123456789";
                    break;
                case CharacterSet.Binary:
                    currentCharacterSet = "01";
                    break;
                case CharacterSet.Hexadecimal:
                    currentCharacterSet = "0123456789ABCDEF";
                    break;
                case CharacterSet.Katakana:
                    currentCharacterSet = "ｱｲｳｴｵｶｷｸｹｺｻｼｽｾｿﾀﾁﾂﾃﾄﾅﾆﾇﾈﾉﾊﾋﾌﾍﾎﾏﾐﾑﾒﾓﾔﾕﾖﾗﾘﾙﾚﾛﾜｦﾝ";
                    break;
                case CharacterSet.Mixed:
                    currentCharacterSet = "0123456789ABCDEFｱｲｳｴｵｶｷｸｹｺ";
                    break;
                case CharacterSet.Custom:
                    currentCharacterSet = customCharacters;
                    break;
            }
        }
        
        private void InitializePool()
        {
            for (int i = 0; i < poolSize; i++)
            {
                TextMeshProUGUI text = CreateTextObject();
                text.gameObject.SetActive(false);
                textPool.Enqueue(text);
            }
        }
        
        private TextMeshProUGUI CreateTextObject()
        {
            GameObject textObj = new GameObject("MatrixChar");
            textObj.transform.SetParent(transform, false);
            
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.color = characterColor;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            
            RectTransform rt = textObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(characterSpacing, characterSpacing);
            
            return text;
        }
        
        private TextMeshProUGUI GetFromPool()
        {
            if (textPool.Count > 0)
            {
                TextMeshProUGUI text = textPool.Dequeue();
                text.gameObject.SetActive(true);
                return text;
            }
            
            return CreateTextObject();
        }
        
        private void ReturnToPool(TextMeshProUGUI text)
        {
            if (text != null)
            {
                text.gameObject.SetActive(false);
                textPool.Enqueue(text);
            }
        }
        
        public void StartEffect()
        {
            if (!isRunning)
            {
                isRunning = true;
                StartCoroutine(SpawnColumns());
                
                if (characterChangeInterval > 0)
                {
                    StartCoroutine(ChangeCharacters());
                }
            }
        }
        
        public void StopEffect()
        {
            isRunning = false;
            StopAllCoroutines();
            ClearAllColumns();
        }
        
        private void ClearAllColumns()
        {
            foreach (var column in activeColumns)
            {
                foreach (var character in column.characters)
                {
                    ReturnToPool(character);
                }
            }
            activeColumns.Clear();
        }
        
        private IEnumerator SpawnColumns()
        {
            while (isRunning)
            {
                if (activeColumns.Count < columnCount)
                {
                    SpawnColumn();
                }
                
                yield return new WaitForSecondsRealtime(spawnInterval);
            }
        }
        
        private void SpawnColumn()
        {
            MatrixColumn column = new MatrixColumn();
            column.speed = Random.Range(minSpeed, maxSpeed);
            
            float screenWidth = rectTransform.rect.width;
            float columnWidth = screenWidth / columnCount;
            
            if (randomSpawnPositions)
            {
                column.xPosition = Random.Range(-screenWidth / 2f, screenWidth / 2f);
            }
            else
            {
                int columnIndex = activeColumns.Count % columnCount;
                column.xPosition = -screenWidth / 2f + columnWidth * columnIndex + columnWidth / 2f;
            }
            
            float screenHeight = rectTransform.rect.height;
            float startY = screenHeight / 2f;
            
            for (int i = 0; i < charactersPerColumn; i++)
            {
                TextMeshProUGUI text = GetFromPool();
                text.text = GetRandomCharacter();
                
                RectTransform rt = text.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(column.xPosition, startY + i * characterSpacing);
                
                if (enableFading)
                {
                    float alpha = 1f - (i / (float)charactersPerColumn);
                    Color color = characterColor;
                    color.a *= alpha;
                    text.color = color;
                }
                
                column.characters.Add(text);
            }
            
            activeColumns.Add(column);
        }
        
        private string GetRandomCharacter()
        {
            if (string.IsNullOrEmpty(currentCharacterSet))
            {
                return "0";
            }
            
            int index = Random.Range(0, currentCharacterSet.Length);
            return currentCharacterSet[index].ToString();
        }
        
        private void Update()
        {
            if (!isRunning)
            {
                return;
            }
            
            float deltaTime = Time.unscaledDeltaTime;
            float screenHeight = rectTransform.rect.height;
            
            for (int i = activeColumns.Count - 1; i >= 0; i--)
            {
                MatrixColumn column = activeColumns[i];
                
                bool shouldRemove = true;
                foreach (var character in column.characters)
                {
                    RectTransform rt = character.GetComponent<RectTransform>();
                    Vector2 pos = rt.anchoredPosition;
                    pos.y -= column.speed * deltaTime;
                    rt.anchoredPosition = pos;
                    
                    if (pos.y > -screenHeight / 2f - characterSpacing * 2f)
                    {
                        shouldRemove = false;
                    }
                }
                
                if (shouldRemove)
                {
                    foreach (var character in column.characters)
                    {
                        ReturnToPool(character);
                    }
                    activeColumns.RemoveAt(i);
                }
            }
        }
        
        private IEnumerator ChangeCharacters()
        {
            while (isRunning)
            {
                foreach (var column in activeColumns)
                {
                    foreach (var character in column.characters)
                    {
                        if (Random.value > 0.5f)
                        {
                            character.text = GetRandomCharacter();
                        }
                    }
                }
                
                yield return new WaitForSecondsRealtime(characterChangeInterval);
            }
        }
    }
}
