using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

public class IntroSceneManager : MonoBehaviour
{
    [Header("UI & Caméras")]
    [Tooltip("Liste des caméras pour la séquence d'intro.")]
    public List<GameObject> introCameras;
    [Tooltip("L'objet Canvas qui contient le texte d'intro.")]
    public GameObject introUI;
    [Tooltip("Le script qui gère l'effet d'écriture.")]
    public IntroTypewriter introTextEffect;
    [Tooltip("Le panneau de fondu (doit couvrir tout l'écran).")]
    public Image fadePanel;
    
    [Header("Timing")]
    [Tooltip("Durée de chaque fondu (en secondes).")]
    public float fadeDuration = 1.0f;
    [Tooltip("Durée de chaque plan de caméra (doit correspondre à l'ordre des caméras).")]
    public List<float> cameraDurations;
    [Tooltip("Délai entre le fondu et le début de l'écriture du texte.")]
    public float delayBeforeText = 0.5f;
    [Tooltip("Délai après le texte et avant la fin.")]
    public float textToCinematicDelay = 2.0f;
    
    [Header("Scène")]
    [Tooltip("Le nom de la scène à charger après l'intro.")]
    public string nextSceneName = "NomDeTaSceneDeJeu";

    void Start()
    {
        foreach (GameObject cam in introCameras)
        {
            cam.SetActive(false);
        }
        if (introUI != null) introUI.SetActive(false);
        
        if (introCameras.Count > 0)
        {
            introCameras[0].SetActive(true);
        }

        StartCoroutine(StartIntroSequence());
    }

    private IEnumerator StartIntroSequence()
    {
        // Étape 1 : Fondu d'entrée pour la première caméra
        yield return StartCoroutine(FadeOut(fadeDuration));
        
        // Étape 2 : Boucle sur les caméras (sauf la dernière)
        for (int i = 0; i < introCameras.Count - 1; i++)
        {
            float duration = (i < cameraDurations.Count) ? cameraDurations[i] : 2.0f;
            yield return new WaitForSeconds(duration);
            
            yield return StartCoroutine(FadeToBlack(fadeDuration));
            introCameras[i].SetActive(false);
            introCameras[i+1].SetActive(true);
            yield return StartCoroutine(FadeOut(fadeDuration));
        }

        // Étape 3 : Gérer la dernière caméra et la transition vers le texte
        if (introCameras.Count > 0)
        {
            int lastCameraIndex = introCameras.Count - 1;
            float duration = (lastCameraIndex < cameraDurations.Count) ? cameraDurations[lastCameraIndex] : 2.0f;
            yield return new WaitForSeconds(duration);
            
            yield return StartCoroutine(FadeToBlack(fadeDuration));
            introCameras[lastCameraIndex].SetActive(false);
        }

        // Étape 4 : Activer le panneau de texte et faire le fondu d'entrée
        if (introUI != null)
        {
            introUI.SetActive(true);
        }
        
        yield return StartCoroutine(FadeOut(fadeDuration));
        
        // Étape 5 : Attendre un petit délai avant de démarrer l'écriture
        yield return new WaitForSeconds(delayBeforeText);
        
        // Étape 6 : Démarrer l'effet de machine à écrire
        if (introTextEffect != null)
        {
            StartCoroutine(introTextEffect.ShowLines());
            while (!introTextEffect.IsFinished)
            {
                yield return null;
            }
        }
        
        // Étape 7 : Attendre un instant avant la fin de l'intro
        yield return new WaitForSeconds(textToCinematicDelay);
        
        // Étape 8 : Fondu final de sortie (l'écran redevient noir)
        yield return StartCoroutine(FadeToBlack(fadeDuration));

        // Étape 9 : Charger la scène de jeu
        SceneManager.LoadScene(nextSceneName);
    }
    
    private IEnumerator FadeToBlack(float duration)
    {
        fadePanel.gameObject.SetActive(true);
        Color panelColor = fadePanel.color;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            panelColor.a = Mathf.Clamp01(elapsed / duration);
            fadePanel.color = panelColor;
            yield return null;
        }
        panelColor.a = 1f;
        fadePanel.color = panelColor;
    }
    
    private IEnumerator FadeOut(float duration)
    {
        fadePanel.gameObject.SetActive(true);
        Color panelColor = fadePanel.color;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            panelColor.a = 1.0f - Mathf.Clamp01(elapsed / duration);
            fadePanel.color = panelColor;
            yield return null;
        }
        fadePanel.gameObject.SetActive(false);
    }
}