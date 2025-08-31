using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("Références UI")]
    public GameObject pauseMenuUI;
    public GameObject optionsMenuUI;
    public Button resumeButton;
    public Button restartButton;
    public Button mainMenuButton;
    public Button quitButton;
    public Button optionsButton;

    [Header("Configuration")]
    public KeyCode pauseKey = KeyCode.Escape;
    public string mainMenuSceneName = "MainMenu";
    
    private bool gameIsPaused = false;
    private PlayerMovement playerMovement;

    void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();
        }

        SetupButtons();
        
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }
        if (optionsMenuUI != null)
        {
            optionsMenuUI.SetActive(false);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(pauseKey))
        {
            if (gameIsPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    void SetupButtons()
    {
        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (restartButton != null) restartButton.onClick.AddListener(RestartLevel);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(LoadMainMenu);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
        if (optionsButton != null) optionsButton.onClick.AddListener(OpenOptions);
    }

    public void Pause()
    {
        if (gameIsPaused) return;
        gameIsPaused = true;
        
        if (pauseMenuUI != null) pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        if (playerMovement != null) playerMovement.enabled = false;
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        if (!gameIsPaused) return;
        gameIsPaused = false;
        
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (optionsMenuUI != null) optionsMenuUI.SetActive(false);
        
        Time.timeScale = 1f;
        
        // ⬅️ Ajout de la réactivation du script de mouvement
        if (playerMovement != null) playerMovement.enabled = true;
        

    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    public void OpenOptions()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }
        if (optionsMenuUI != null)
        {
            optionsMenuUI.SetActive(true);
        }
    }
}