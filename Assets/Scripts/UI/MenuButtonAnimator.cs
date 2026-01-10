using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class MenuButtonAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Scale Animation")]
    [SerializeField] private bool enableScaleAnimation = true;
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float animationSpeed = 10f;
    
    [Header("Color Animation")]
    [SerializeField] private bool enableColorAnimation = true;
    [SerializeField] private Image targetImage;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = new Color(1f, 0.8f, 0.2f);
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;
    
    private Vector3 originalScale;
    private Vector3 targetScale;
    private Color targetColor;
    private Button button;

    private void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
        
        button = GetComponent<Button>();
        
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }
        
        if (targetImage != null)
        {
            normalColor = targetImage.color;
            targetColor = normalColor;
        }
        
        if (button != null)
        {
            button.onClick.AddListener(OnClick);
        }
    }

    private void Update()
    {
        if (enableScaleAnimation)
        {
            transform.localScale = Vector3.Lerp(
                transform.localScale,
                targetScale,
                Time.deltaTime * animationSpeed
            );
        }
        
        if (enableColorAnimation && targetImage != null)
        {
            targetImage.color = Color.Lerp(
                targetImage.color,
                targetColor,
                Time.deltaTime * animationSpeed
            );
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = originalScale * hoverScale;
        targetColor = hoverColor;
        
        if (audioSource != null && hoverSound != null)
        {
            audioSource.PlayOneShot(hoverSound);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
        targetColor = normalColor;
    }

    private void OnClick()
    {
        if (audioSource != null && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }
}
