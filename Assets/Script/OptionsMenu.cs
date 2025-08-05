using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class OptionsMenu : MonoBehaviour
{
    // Références à l'AudioMixer et aux paramètres exposés
    [Header("Paramètres Audio")]
    public AudioMixer masterMixer;
    public string sfxVolumeParameter = "SFXVolume";
    public string musicVolumeParameter = "MusicVolume";

    // Références aux éléments d'UI
    [Header("Éléments d'UI")]
    public Dropdown qualityDropdown;
    public TMPro.TMP_Dropdown resolutionDropdown;
    public Slider sfxVolumeSlider;
    public Slider musicVolumeSlider;
    public TMPro.TextMeshProUGUI musicVolumeText;
    public GameObject optionsPanel;

    // Référence au panneau du menu principal
    public GameObject mainMenuPanel;

    // Variables pour les résolutions
    private Resolution[] resolutions;

    private void Start()
    {
        SetupResolutionDropdown();
        LoadSettings();
    }

    private void SetupResolutionDropdown()
    {
        resolutions = Screen.resolutions;
        resolutions = resolutions.Where(r => r.width / (float)r.height > 1.7f && r.width / (float)r.height < 1.8f).ToArray();
        
        resolutionDropdown.ClearOptions();
        
        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }
        
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }
    
    public void LoadSettings()
    {
        float sfxVolume;
        if (masterMixer.GetFloat(sfxVolumeParameter, out sfxVolume))
        {
            sfxVolumeSlider.value = Mathf.Pow(10, sfxVolume / 20);
        }

        float musicVolume;
        if (masterMixer.GetFloat(musicVolumeParameter, out musicVolume))
        {
            musicVolumeSlider.value = Mathf.Pow(10, musicVolume / 20);
        }
        
        UpdateMusicVolumeText(musicVolumeSlider.value); 

        qualityDropdown.value = QualitySettings.GetQualityLevel();
        qualityDropdown.RefreshShownValue();
    }
    
    public void SetMusicVolume(float volume)
    {
        float safeVolume = Mathf.Clamp(volume, 0.0001f, 1f);
        masterMixer.SetFloat(musicVolumeParameter, Mathf.Log10(safeVolume) * 20f);
        UpdateMusicVolumeText(volume);
    }

    public void SetSFXVolume(float volume)
    {
        float safeVolume = Mathf.Clamp(volume, 0.0001f, 1f);
        masterMixer.SetFloat(sfxVolumeParameter, Mathf.Log10(safeVolume) * 20f);
    }
    
    public void UpdateMusicVolumeText(float volume)
    {
        int percent = Mathf.RoundToInt(volume * 100);
        musicVolumeText.text = percent + "%";
    }

    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }

    // Méthode pour le bouton "Apply"
    public void ApplyAndClose()
    {
        // On pourrait ajouter ici des lignes pour sauvegarder les paramètres si nécessaire
        
        // Ferme le panneau d'options
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(false);
        }

        // Active le panneau du menu principal
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }
    }
    
    // Méthode pour ouvrir le menu d'options
    public void OpenOptions()
    {
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(true);
        }
        // S'assurer que le menu principal est désactivé
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }
    }
}