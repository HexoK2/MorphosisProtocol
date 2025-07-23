using UnityEngine;
using System.Collections.Generic;
using TMPro; // Nécessaire pour TextMeshPro

public class ObjectHighlighter : MonoBehaviour
{
    [Tooltip("Le LayerMask des objets que ce script peut surligner (et interagir avec).")]
    public LayerMask interactableLayers; // Renommé pour plus de clarté

    [Tooltip("Le Material à utiliser pour la surbrillance (doit être transparent et blanc).")]
    public Material hoverHighlightMaterial;

    [Tooltip("La distance maximale de détection du Raycast.")]
    public float maxDetectionDistance = 100f; // Renommé pour plus de clarté

    [Header("Conditions de lumière")]
    [Tooltip("Référence au script PlayerMovement pour vérifier la torche.")]
    public PlayerMovement playerMovement; // Lien vers le script PlayerMovement
    
    // NOUVEAU : Référence au script TypewriterEffect
    [Tooltip("Référence au script TypewriterEffect pour animer le message 'Pas de lumière'.")]
    public TypewriterEffect noLightMessageEffect; 

    [Tooltip("Durée d'affichage du message 'Je ne peux rien voir...'")]
    public float messageDisplayDuration = 2.0f;

    private GameObject currentlyHighlightedObject = null;
    private Dictionary<GameObject, Material[]> originalMaterialsMap = new Dictionary<GameObject, Material[]>(); // Gère tous les matériaux
    private float messageTimer = 0f;
    private bool isMessageShowing = false;

    void Start()
    {
        // Vérifie les références au démarrage
        if (playerMovement == null)
        {
            playerMovement = FindObjectOfType<PlayerMovement>();
            if (playerMovement == null)
            {
                Debug.LogError("PlayerMovement script non trouvé ! Veuillez l'assigner manuellement ou assurez-vous qu'il existe dans la scène.");
                enabled = false; // Désactive le script si essentiel manquant
                return;
            }
        }
        
        // NOUVEAU : Vérifier la référence TypewriterEffect
        if (noLightMessageEffect == null)
        {
            Debug.LogError("TypewriterEffect pour le message 'No Light' non assigné ! Assurez-vous de glisser votre objet TextMeshPro qui a le script TypewriterEffect dans l'Inspecteur.");
            enabled = false;
            return;
        }

        // Assure-toi que le message est caché au début via le script TypewriterEffect
        // Le TypewriterEffect est responsable de désactiver son propre GameObject au démarrage du jeu via cette fonction.
        noLightMessageEffect.HideText(); 
    }

    void Update()
    {
        // Gère le timer du message
        if (isMessageShowing)
        {
            messageTimer -= Time.deltaTime;
            // Si le timer est écoulé OU que le joueur clique (pour "skipper" l'écriture)
            if (messageTimer <= 0f || (Input.GetMouseButtonDown(0) && noLightMessageEffect.IsTyping))
            {
                // Si l'écriture est en cours, la "skipper" plutôt que de cacher directement
                if (noLightMessageEffect.IsTyping)
                {
                    noLightMessageEffect.SkipTypewriterEffect();
                    // Réinitialise le timer pour laisser le texte complet affiché un court instant
                    messageTimer = 0.5f; // Petite pause après le skip
                }
                else // Si l'écriture est terminée ou skiée, on peut cacher
                {
                    HideMessage();
                }
            }
        }

        // --- Détection de l'objet sous la souris ---
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxDetectionDistance, interactableLayers))
        {
            GameObject hitObject = hit.collider.gameObject;

            // --- Logique de surbrillance si le joueur a la torche ---
            if (playerMovement.hasTorch)
            {
                if (hitObject != currentlyHighlightedObject)
                {
                    ResetHighlight(); // Enlève la surbrillance de l'ancien objet
                    ApplyHighlight(hitObject); // Applique la surbrillance au nouvel objet
                }
            }
            else // Le joueur n'a PAS la torche
            {
                // Si la souris est sur un objet mais qu'il n'y a pas de torche, on ne surligne rien
                ResetHighlight();
            }

            // --- Logique de clic sur l'objet ---
            if (Input.GetMouseButtonDown(0)) // Si le joueur clique (bouton gauche)
            {
                // Si le joueur n'a PAS la torche ET l'objet est un meuble à inspecter
                if (!playerMovement.hasTorch)
                {
                    // NOUVEAU : Lance l'effet d'écriture
                    ShowMessage("I can't see anything without light...");
                }
                else
                {
                    // Optionnel : Ajoute ici la logique pour interagir avec le meuble quand la torche est présente
                    Debug.Log("Le joueur clique sur " + hitObject.name + " AVEC la torche !");
                    // Assure-toi de cacher le message si le joueur obtient la torche et interagit.
                    HideMessage(); 
                }
            }
        }
        else // Aucun objet n'est détecté par le Raycast
        {
            ResetHighlight(); // Enlève la surbrillance de tout objet si la souris quitte
            // Optionnel : Cacher le message si le joueur ne regarde plus d'objet
            // HideMessage(); 
        }
    }

    // Applique le matériau de surbrillance (inchangé)
    void ApplyHighlight(GameObject objToHighlight)
    {
        Renderer objRenderer = objToHighlight.GetComponent<Renderer>();
        if (objRenderer != null)
        {
            Material[] currentMaterials = objRenderer.sharedMaterials;
            
            if (!originalMaterialsMap.ContainsKey(objToHighlight))
            {
                originalMaterialsMap.Add(objToHighlight, currentMaterials);
            }

            Material[] newMaterials = new Material[currentMaterials.Length];
            for (int i = 0; i < newMaterials.Length; i++)
            {
                newMaterials[i] = hoverHighlightMaterial;
            }
            objRenderer.materials = newMaterials;
            
            currentlyHighlightedObject = objToHighlight;
        }
    }

    // Réinitialise l'objet à ses matériaux originaux (inchangé)
    void ResetHighlight()
    {
        if (currentlyHighlightedObject != null)
        {
            Renderer objRenderer = currentlyHighlightedObject.GetComponent<Renderer>();
            if (objRenderer != null && originalMaterialsMap.ContainsKey(currentlyHighlightedObject))
            {
                objRenderer.materials = originalMaterialsMap[currentlyHighlightedObject];
                originalMaterialsMap.Remove(currentlyHighlightedObject);
            }
            currentlyHighlightedObject = null;
        }
    }

    // Affiche le message en utilisant le TypewriterEffect
    void ShowMessage(string message)
    {
        if (noLightMessageEffect != null)
        {
            // ACTIVER le GameObject avant de démarrer la coroutine
            noLightMessageEffect.gameObject.SetActive(true); 

            noLightMessageEffect.fullTextToDisplay = message; // Donne le texte au TypewriterEffect
            noLightMessageEffect.StartTypewriterEffect(); // Déclenche l'écriture
            isMessageShowing = true;
            messageTimer = messageDisplayDuration;
        }
    }

    // Cache le message en utilisant le TypewriterEffect
    void HideMessage()
    {
        if (noLightMessageEffect != null)
        {
            noLightMessageEffect.HideText(); // Utilise la fonction HideText du TypewriterEffect pour désactiver le GameObject
            isMessageShowing = false;
        }
    }

    // Nettoyage si le script est désactivé
    void OnDisable()
    {
        ResetHighlight();
        HideMessage();
    }
}