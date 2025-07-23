using UnityEngine;
using TMPro; // Nécessaire pour TextMeshPro
using System.Collections; // Nécessaire pour IEnumerator

public class TypewriterEffect : MonoBehaviour
{
    // Le composant TextMeshPro que ce script va contrôler
    [Tooltip("Le composant TextMeshPro que ce script va animer.")]
    public TextMeshProUGUI textMeshProComponent;

    // Nouvelle variable : Taille de la police (qui affecte la taille des caractères)
    [Tooltip("La taille de la police du texte (affecte la taille des caractères eux-mêmes).")]
    public float fontSize = 36f; // Valeur par défaut

    // NOUVELLE VARIABLE : Scale du GameObject (qui affecte la taille globale de l'objet)
    [Tooltip("La scale du GameObject du texte. Elle sera forcée à cette valeur.")]
    public Vector3 lockedScale = new Vector3(1f, 1f, 1f); // Valeur par défaut (1,1,1)

    // Le texte complet à afficher
    // Ceci sera rempli par le script ObjectHighlighter
    [HideInInspector] // On le cache dans l'inspecteur car ObjectHighlighter le gérera
    public string fullTextToDisplay; 

    // Délai entre chaque lettre (plus petit = plus rapide)
    [Tooltip("Délai en secondes entre l'affichage de chaque caractère.")]
    public float delayBetweenChars = 0.05f;

    // Délai avant de commencer l'écriture
    [Tooltip("Délai en secondes avant de commencer à écrire le texte.")]
    public float startDelay = 0f;

    // Optionnel : Son à jouer à chaque frappe de caractère
    [Tooltip("Clip audio à jouer pour chaque caractère (optionnel).")]
    public AudioClip typingClip;

    private Coroutine typewriterCoroutine;
    private bool isTyping = false;

    public bool IsTyping { get { return isTyping; } }

    void Awake()
    {
        // Récupère le composant TextMeshProUGUI s'il n'est pas déjà assigné
        if (textMeshProComponent == null)
        {
            textMeshProComponent = GetComponent<TextMeshProUGUI>();
        }

        if (textMeshProComponent != null)
        {
            textMeshProComponent.text = "";
            textMeshProComponent.fontSize = fontSize; 
        }

        // --- Applique la scale verrouillée au démarrage ---
        transform.localScale = lockedScale; 
    }

    // Fonction pour démarrer l'effet
    public void StartTypewriterEffect()
    {
        // Arrête toute coroutine précédente pour éviter les chevauchements
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
        }

        // Applique la taille de police définie
        if (textMeshProComponent != null)
        {
            textMeshProComponent.fontSize = fontSize;
        }

        // --- Applique la scale verrouillée au moment du lancement de l'effet ---
        transform.localScale = lockedScale;
        
        // Lance la nouvelle coroutine
        typewriterCoroutine = StartCoroutine(Typewrite());
    }

    // Fonction pour arrêter l'effet et afficher le texte complet immédiatement
    public void SkipTypewriterEffect()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
        }
        if (textMeshProComponent != null)
        {
            textMeshProComponent.text = fullTextToDisplay; // Affiche tout le texte
            isTyping = false; // L'écriture est terminée
        }
    }

    // La coroutine qui gère l'écriture lettre par lettre
    IEnumerator Typewrite()
    {
        isTyping = true; // Indique que l'écriture commence
        
        textMeshProComponent.text = "";

        // Attend le délai de démarrage
        if (startDelay > 0)
        {
            yield return new WaitForSeconds(startDelay);
        }

        foreach (char c in fullTextToDisplay)
        {
            if (!isTyping) { 
                textMeshProComponent.text = fullTextToDisplay;
                yield break; 
            }

            textMeshProComponent.text += c; 

            if (typingClip != null)
            {
                AudioSource.PlayClipAtPoint(typingClip, Camera.main.transform.position, 0.5f);
            }

            yield return new WaitForSeconds(delayBetweenChars); 
        }

        isTyping = false; 
    }

    // Fonction pour cacher complètement le texte (utile quand le message disparaît)
    public void HideText()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
        }
        if (textMeshProComponent != null)
        {
            textMeshProComponent.gameObject.SetActive(false); // Désactive le GameObject ici
            textMeshProComponent.text = ""; // Efface le texte
        }
        isTyping = false;
    }
}