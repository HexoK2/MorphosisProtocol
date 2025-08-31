using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using System.Collections.Generic;
using System.Linq;

public class OptionsMenu : MonoBehaviour
{
    [Header("Références UI")]
    public Button backButton;
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public Toggle fullscreenToggle;
    public Dropdown resolutionDropdown;
    public GameObject optionsMenuUI; // Référence au GameObject de ton menu d'options
    public GameObject pauseMenuUI; // Référence au GameObject du menu pause
    

    [Header("Audio")]
    public AudioMixer audioMixer;

    [Header("Configuration")]
    public string defaultReturnScene = "MainMenu";

    private Resolution[] resolutions;

    void Start()
    {
        SetupButtons();
        SetupAudioOptions();
        SetupGraphicsOptions();
        LoadSettings();
    }

    void SetupButtons()
    {
        if (backButton != null)
        {
            backButton.onClick.AddListener(GoBack);
        }
    }

    void SetupAudioOptions()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        }
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        }
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
        }
    }

    public void SetMasterVolume(float volume)
    {
        if (audioMixer != null) audioMixer.SetFloat("MasterVolume", Mathf.Log10(volume) * 20);
    }

    public void SetMusicVolume(float volume)
    {
        if (audioMixer != null) audioMixer.SetFloat("MusicVolume", Mathf.Log10(volume) * 20);
    }

    public void SetSFXVolume(float volume)
    {
        if (audioMixer != null) audioMixer.SetFloat("SFXVolume", Mathf.Log10(volume) * 20);
    }

    void SetupGraphicsOptions()
    {
        if (resolutionDropdown != null)
        {
            resolutions = Screen.resolutions.Where(res => res.refreshRateRatio.value == Screen.currentResolution.refreshRateRatio.value).ToArray();
            resolutionDropdown.ClearOptions();
            List<string> options = new List<string>();
            int currentResolutionIndex = 0;

            for (int i = 0; i < resolutions.Length; i++)
            {
                string option = resolutions[i].width + " x " + resolutions[i].height;
                options.Add(option);
                if (resolutions[i].width == Screen.currentResolution.width && resolutions[i].height == Screen.currentResolution.height)
                {
                    currentResolutionIndex = i;
                }
            }
            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = currentResolutionIndex;
            resolutionDropdown.RefreshShownValue();
            resolutionDropdown.onValueChanged.AddListener(SetResolution);
        }
        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        PlayerPrefs.SetInt("ResolutionIndex", resolutionIndex);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
    }

    public void ResetSettings()
    {
        if (masterVolumeSlider != null) masterVolumeSlider.value = 0.75f;
        if (musicVolumeSlider != null) musicVolumeSlider.value = 0.75f;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = 0.75f;
        if (fullscreenToggle != null) fullscreenToggle.isOn = true;
        
        PlayerPrefs.DeleteAll();
        Debug.Log("Paramètres réinitialisés. Redémarrez le jeu pour voir les changements.");
    }
    
    void LoadSettings()
    {
        if (masterVolumeSlider != null)
        {
            float volume = PlayerPrefs.GetFloat("MasterVolume", 0.75f);
            masterVolumeSlider.value = volume;
            SetMasterVolume(volume);
        }
        if (musicVolumeSlider != null)
        {
            float volume = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
            musicVolumeSlider.value = volume;
            SetMusicVolume(volume);
        }
        if (sfxVolumeSlider != null)
        {
            float volume = PlayerPrefs.GetFloat("SFXVolume", 0.75f);
            sfxVolumeSlider.value = volume;
            SetSFXVolume(volume);
        }
        if (fullscreenToggle != null)
        {
            bool isFullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
            fullscreenToggle.isOn = isFullscreen;
            SetFullscreen(isFullscreen);
        }
        if (resolutionDropdown != null)
        {
            int resIndex = PlayerPrefs.GetInt("ResolutionIndex", 0);
            resolutionDropdown.value = resIndex;
        }
    }

    // === NAVIGATION ===
    public void GoBack()
    {
        if (optionsMenuUI != null)
        {
            optionsMenuUI.SetActive(false); // Cache le menu d'options
        }
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true); // Affiche le menu pause
        }
    }
}