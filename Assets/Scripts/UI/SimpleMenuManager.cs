using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SimpleMenuManager : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;
    
    [Header("Scene Settings")]
    [SerializeField] private string gameSceneName = "Sandbox";

    private void Start()
    {
        SetupButtons();
        SetCursor();
    }

    private void SetupButtons()
    {
        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayClicked);
            Debug.Log("Play button listener added!");
        }
        else
        {
            Debug.LogError("Play Button is NOT assigned!");
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitClicked);
            Debug.Log("Quit button listener added!");
        }
        else
        {
            Debug.LogError("Quit Button is NOT assigned!");
        }
    }

    private void SetCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void OnPlayClicked()
    {
        Debug.Log($"PLAY CLICKED! Loading scene: {gameSceneName}");
        
        if (!string.IsNullOrEmpty(gameSceneName))
        {
            try
            {
                SceneManager.LoadScene(gameSceneName);
                Debug.Log($"Scene load initiated: {gameSceneName}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load scene '{gameSceneName}': {e.Message}");
                Debug.LogError("Make sure the scene is added to Build Settings (File → Build Settings)");
            }
        }
        else
        {
            Debug.LogError("Game Scene Name is empty!");
        }
    }

    private void OnQuitClicked()
    {
        Debug.Log("QUIT CLICKED! Quitting application...");
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
