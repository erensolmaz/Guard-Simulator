using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("Menu Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject creditsPanel;
    
    [Header("Main Menu Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button quitButton;
    
    [Header("Settings")]
    [SerializeField] private string gameSceneName = "Sandbox";
    
    [Header("Audio")]
    [SerializeField] private AudioSource menuMusic;

    private void Start()
    {
        ShowMainMenu();
        SetupButtonListeners();
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void SetupButtonListeners()
    {
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayClicked);
        
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsClicked);
        
        if (creditsButton != null)
            creditsButton.onClick.AddListener(OnCreditsClicked);
        
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);
    }

    private void ShowMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    public void OnPlayClicked()
    {
        if (!string.IsNullOrEmpty(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            Debug.LogWarning("Game scene name is not set!");
        }
    }

    public void OnSettingsClicked()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void OnCreditsClicked()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(true);
    }

    public void OnBackToMainMenu()
    {
        ShowMainMenu();
    }

    public void OnQuitClicked()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
