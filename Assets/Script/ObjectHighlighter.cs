using UnityEngine;
using System.Collections.Generic;
using TMPro; // N'oublie pas d'ajouter cette ligne pour les messages TextMeshPro.

public class ObjectHighlighter : MonoBehaviour
{
    [Tooltip("Le LayerMask des objets que ce script peut surligner.")]
    public LayerMask highlightableLayers;

    [Tooltip("Le Material à utiliser pour la surbrillance.")]
    public Material highlightMaterial;

    [Tooltip("La distance maximale de détection du Raycast.")]
    public float maxDetectionDistance = 100f;

    [Header("Conditions pour l'interaction")]
    [Tooltip("Référence au script PlayerMovement pour vérifier la torche.")]
    public PlayerMovement playerMovement;

    [Tooltip("Référence au script TypewriterEffect pour animer le message 'Pas de lumière'.")]
    public TypewriterEffect noLightMessageEffect;

    [Tooltip("Durée d'affichage du message 'Je ne peux rien voir...'")]
    public float messageDisplayDuration = 2.0f;

    // L'objet actuellement survolé.
    private GameObject currentHighlightedObject = null;
    
    // Dictionnaire pour stocker les matériaux d'origine de chaque objet survolé.
    private Dictionary<GameObject, Material[]> originalMaterialsMap = new Dictionary<GameObject, Material[]>();
    
    // Variables pour gérer l'affichage du message de warning
    private float messageTimer = 0f;
    private bool isMessageShowing = false;

    void Start()
    {
        // Vérification des références
        if (playerMovement == null)
        {
            Debug.LogError("PlayerMovement script non trouvé ! Assurez-vous qu'il existe et est assigné dans l'Inspecteur.");
            enabled = false;
        }
        if (noLightMessageEffect == null)
        {
            Debug.LogError("TypewriterEffect pour le message de warning non assigné ! Assurez-vous de le glisser depuis la scène.");
            enabled = false;
        }
        else
        {
            noLightMessageEffect.HideText();
        }
    }

    void Update()
    {
        // --- LOGIQUE DE GESTION DU MESSAGE DE WARNING ---
        if (isMessageShowing)
        {
            messageTimer -= Time.deltaTime;
            // Cache le message si le temps est écoulé
            if (messageTimer <= 0f)
            {
                HideMessage();
            }
        }
        
        // --- LOGIQUE DE SURBRILLANCE AU SURVOL (Raycast) ---
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        GameObject hitObject = null;

        if (Physics.Raycast(ray, out hit, maxDetectionDistance, highlightableLayers))
        {
            hitObject = hit.collider.gameObject;
        }

        if (hitObject != currentHighlightedObject)
        {
            ResetHighlight();
            if (hitObject != null)
            {
                ApplyHighlight(hitObject);
            }
        }

        // --- LOGIQUE D'INTERACTION (Clic de souris) ---
        if (Input.GetMouseButtonDown(0) && hitObject != null)
        {
            // Si le joueur n'a pas la torche
            if (playerMovement != null && !playerMovement.hasTorch)
            {
                ShowMessage("I can't see anything without light...");
                // On met la logique de l'effet d'écriture ici, si tu en as une.
            }
            else
            {
                // Le joueur clique sur un objet AVEC la torche, donc on peut agir.
                Debug.Log("Le joueur interagit avec " + hitObject.name + " AVEC la torche !");
                HideMessage(); // Cache le message si jamais il était affiché.
            }
        }
    }

    /// <summary>
    /// Applique le matériau de surbrillance à un objet.
    /// </summary>
    private void ApplyHighlight(GameObject objToHighlight)
    {
        // ... (Le code de cette méthode reste inchangé) ...
        Renderer objRenderer = objToHighlight.GetComponent<Renderer>();
        if (objRenderer == null) return;

        if (!originalMaterialsMap.ContainsKey(objToHighlight))
        {
            originalMaterialsMap.Add(objToHighlight, objRenderer.materials);
        }

        Material[] newMaterials = new Material[objRenderer.materials.Length];
        for (int i = 0; i < newMaterials.Length; i++)
        {
            newMaterials[i] = highlightMaterial;
        }

        objRenderer.materials = newMaterials;
        currentHighlightedObject = objToHighlight;
    }

    /// <summary>
    /// Réinitialise l'objet survolé à ses matériaux d'origine.
    /// </summary>
    private void ResetHighlight()
    {
        // ... (Le code de cette méthode reste inchangé) ...
        if (currentHighlightedObject != null)
        {
            Renderer objRenderer = currentHighlightedObject.GetComponent<Renderer>();
            
            if (objRenderer != null && originalMaterialsMap.ContainsKey(currentHighlightedObject))
            {
                objRenderer.materials = originalMaterialsMap[currentHighlightedObject];
                originalMaterialsMap.Remove(currentHighlightedObject);
            }
            currentHighlightedObject = null;
        }
    }

    /// <summary>
    /// Affiche le message de warning.
    /// </summary>
    /// <param name="message">Le texte à afficher.</param>
    private void ShowMessage(string message)
    {
        if (noLightMessageEffect != null)
        {
            noLightMessageEffect.gameObject.SetActive(true);
            noLightMessageEffect.fullTextToDisplay = message;
            noLightMessageEffect.StartTypewriterEffect();
            isMessageShowing = true;
            messageTimer = messageDisplayDuration;
        }
    }

    /// <summary>
    /// Cache le message de warning.
    /// </summary>
    private void HideMessage()
    {
        if (noLightMessageEffect != null)
        {
            noLightMessageEffect.HideText();
            isMessageShowing = false;
        }
    }

    /// <summary>
    /// S'assure de nettoyer la surbrillance et le message quand le script est désactivé.
    /// </summary>
    void OnDisable()
    {
        ResetHighlight();
        HideMessage();
    }
}