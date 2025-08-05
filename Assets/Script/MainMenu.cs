using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // Nom de la scène du jeu à charger
    public string gameSceneName = "GameScene";

    // Références aux panneaux UI
    public GameObject mainMenuPanel;
    public GameObject optionsMenuPanel;

    // S'assurer que le menu principal est actif et le menu d'options désactivé au démarrage
    void Start()
    {
        mainMenuPanel.SetActive(true);
        optionsMenuPanel.SetActive(false);
    }

    public void StartGame()
    {
        Debug.Log("Démarrage du jeu...");
        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenOptions()
    {
        mainMenuPanel.SetActive(false);
        optionsMenuPanel.SetActive(true);
    }

    public void CloseOptions()
    {
        optionsMenuPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void QuitGame()
    {
        Debug.Log("Quitter le jeu...");
        Application.Quit();
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}