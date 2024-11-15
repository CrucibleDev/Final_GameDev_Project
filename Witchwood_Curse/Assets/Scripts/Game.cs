using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class Game : MonoBehaviour
{
    private static Game _instance;
    public static Game instance {get{return _instance;}}

    [Header("UI References")]
    [SerializeField] private GameObject pauseMenuPanel;
    
    [Header("Game Settings")]
    [SerializeField] private bool canPause = true;
    [SerializeField] private bool persistBetweenScenes = true;
    
    private bool isPaused = false;
    public bool IsPaused => isPaused;
    
    //gameState
    Player player; 

    void Awake() {
        if(_instance != null && _instance != this){
            Destroy(gameObject);
        }
        else{
            _instance = this;
            if (persistBetweenScenes)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
    }
    
    void Start()
    {

    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && canPause)
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (!canPause) return;
        
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
    }

    public void ChangeScene(string sceneName)
    {
        Time.timeScale = 1f;  // Reset time scale when changing scenes
        isPaused = false;     // Reset pause state
        SceneManager.LoadScene(sceneName);
    }

    public void LoadMainMenu()
    {
        ChangeScene("MainMenu");
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    public void SetPauseEnabled(bool enabled)
    {
        canPause = enabled;
        if (!canPause && isPaused)
        {
            ResumeGame();
        }
    }
}
