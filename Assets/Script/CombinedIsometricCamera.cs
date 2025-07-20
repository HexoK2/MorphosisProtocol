using UnityEngine;
using System.Collections.Generic; // Nécessaire pour List et Dictionary

public class CombinedIsometricCamera : MonoBehaviour
{
    [Header("Paramètres de Suivi de Caméra")]
    [Tooltip("La cible que la caméra doit suivre (votre joueur).")]
    public Transform playerTarget;

    [Tooltip("L'offset de position par rapport à la cible (x, y, z).")]
    public Vector3 cameraOffset = new Vector3(10f, 10f, -10f); // Exemple d'offset isométrique
    
    [Tooltip("La vitesse à laquelle la caméra se déplace pour atteindre la position cible.")]
    [Range(0.01f, 1f)] // Restreint la valeur entre 0.01 et 1
    public float cameraSmoothSpeed = 0.125f; 

    [Header("Paramètres de Transparence des Obstacles")]
    [Tooltip("Le LayerMask qui contient tous les objets qui peuvent bloquer la vue et qui doivent devenir transparents.")]
    public LayerMask obstacleLayer;

    [Tooltip("L'alpha cible lorsque l'objet est transparent (0 = complètement invisible, 1 = complètement visible).")]
    [Range(0f, 1f)]
    public float transparentAlpha = 0.3f; // Valeur d'alpha pour la transparence

    [Tooltip("La vitesse à laquelle la transparence change.")]
    public float fadeSpeed = 5f;

    [Header("Paramètres SphereCast pour Obstacles")]
    [Tooltip("Le rayon de la sphère utilisée pour détecter les obstacles (pour les obstacles entre caméra et joueur).")]
    public float sphereCastRadius = 0.5f; // Ajustez cette valeur ! (La taille de votre blob/joueur peut être une bonne base)

    [Header("Debug")]
    [Tooltip("Active/désactive les Gizmos pour visualiser les Raycasts.")]
    public bool showDebugRay = true;

    // Variables pour la gestion de la transparence
    private List<Renderer> currentlyTransparentObjects = new List<Renderer>(); // Objets transparents entre caméra et joueur
    private Dictionary<Renderer, Color> originalAlbedoColors = new Dictionary<Renderer, Color>();

    // Nouvelle variable pour l'objet actuellement survolé par la souris
    private Renderer mouseOverObject = null;


    void LateUpdate()
    {
        // --- Gestion du Suivi de Caméra ---
        HandleCameraFollowing();

        // --- Gestion de la Transparence des Obstacles (entre caméra et joueur) ---
        // Cette fonction doit déterminer quels objets SONT entre la caméra et le joueur
        // et les marquer comme transparents (ou maintenir leur transparence).
        HandleObstacleTransparencyBetweenCameraAndPlayer();

        // --- Gestion de la transparence au survol de la souris ---
        // Cette fonction gère l'objet directement sous la souris.
        // Elle a priorité sur la transparence par SphereCast pour l'objet survolé.
        HandleMouseOverTransparency();

        // --- Réinitialisation des objets qui ne devraient plus être transparents ---
        // Cette boucle est cruciale pour que les objets redeviennent opaques.
        ResetNoLongerTransparentObjects();
    }

    void HandleCameraFollowing()
    {
        if (playerTarget == null)
        {
            Debug.LogWarning("La caméra n'a pas de cible à suivre (playerTarget) ! Veuillez assigner une cible dans l'inspecteur.");
            return;
        }

        Vector3 desiredPosition = playerTarget.position + cameraOffset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, cameraSmoothSpeed);
        transform.position = smoothedPosition;

        transform.LookAt(playerTarget);
    }

    void HandleObstacleTransparencyBetweenCameraAndPlayer()
    {
        if (playerTarget == null) 
        {
            Debug.LogWarning("Player Target is null in HandleObstacleTransparencyBetweenCameraAndPlayer. Exiting.");
            return;
        }

        // Liste temporaire pour les objets qui devraient être transparents via SphereCast ce frame
        List<Renderer> sphereCastTransparentThisFrame = new List<Renderer>();

        // Effectuer un SphereCast pour détecter les obstacles entre la caméra et le joueur
        Vector3 rayOrigin = transform.position;
        Vector3 rayDirection = (playerTarget.position - rayOrigin).normalized;
        float rayDistance = Vector3.Distance(rayOrigin, playerTarget.position);

        RaycastHit[] hits = Physics.SphereCastAll(rayOrigin, sphereCastRadius, rayDirection, rayDistance, obstacleLayer);

        foreach (RaycastHit hit in hits)
        {
            Renderer hitRenderer = hit.collider.GetComponent<Renderer>();
            if (hitRenderer != null) 
            {
                // Si l'objet n'est PAS l'objet actuellement survolé par la souris, on le marque pour la transparence SphereCast
                if (hitRenderer != mouseOverObject)
                {
                    sphereCastTransparentThisFrame.Add(hitRenderer);
                    if (!originalAlbedoColors.ContainsKey(hitRenderer))
                    {
                        originalAlbedoColors[hitRenderer] = hitRenderer.material.color; 
                    }
                    SetTransparent(hitRenderer);
                }
            }
        }
        // Met à jour la liste des objets actuellement transparents via SphereCast
        currentlyTransparentObjects = sphereCastTransparentThisFrame;
    }

    void HandleMouseOverTransparency()
    {
        Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        Renderer newMouseOverObject = null; // Le renderer que la souris survole *ce frame*

        // Si le rayon de la souris touche un objet de l'obstacleLayer
        if (Physics.Raycast(mouseRay, out hit, Mathf.Infinity, obstacleLayer))
        {
            newMouseOverObject = hit.collider.GetComponent<Renderer>();
        }

        // Si l'objet survolé a changé ou n'existe plus
        if (mouseOverObject != newMouseOverObject)
        {
            // Si l'ancien objet survolé existait et n'est PAS un objet transparent par SphereCast, le réinitialiser.
            // S'il est dans currentlyTransparentObjects, c'est que le SphereCast veut le garder transparent, donc on ne le touche pas.
            if (mouseOverObject != null && !currentlyTransparentObjects.Contains(mouseOverObject))
            {
                // ResetObjectMaterial va le ramener à son état normal si ce n'est pas le nouveau mouseOverObject
                // et qu'il n'est pas déjà dans currentlyTransparentObjects
            }
            
            // Met à jour l'objet survolé
            mouseOverObject = newMouseOverObject;

            // Si un nouvel objet est maintenant survolé et qu'il n'était pas déjà transparent par SphereCast
            if (mouseOverObject != null) // && !currentlyTransparentObjects.Contains(mouseOverObject)) // Non, on veut qu'il soit transparent *même si* il est déjà opaque
            {
                if (!originalAlbedoColors.ContainsKey(mouseOverObject))
                {
                    originalAlbedoColors[mouseOverObject] = mouseOverObject.material.color;
                }
                SetTransparent(mouseOverObject);
            }
        }
        // Si l'objet est toujours le même que le frame précédent (mouseOverObject == newMouseOverObject)
        else if (mouseOverObject != null)
        {
            SetTransparent(mouseOverObject); // S'assure qu'il reste transparent
        }
    }

    // Nouvelle fonction pour réinitialiser les objets qui ne devraient plus être transparents
    void ResetNoLongerTransparentObjects()
    {
        // On récupère toutes les clés (Renderers) des couleurs originales enregistrées
        List<Renderer> renderersWithOriginalColors = new List<Renderer>(originalAlbedoColors.Keys);

        foreach (Renderer rend in renderersWithOriginalColors)
        {
            if (rend == null) 
            {
                // Si le Renderer n'existe plus, on le retire.
                originalAlbedoColors.Remove(rend);
                continue;
            }

            // Un objet doit être réinitialisé si :
            // 1. Il n'est PAS dans la liste des objets transparents par SphereCast ce frame.
            // 2. Il n'est PAS l'objet actuellement survolé par la souris.
            // 3. Et son alpha n'est pas déjà la valeur originale (pour éviter des Lerp inutiles).

            bool shouldBeOpaque = !currentlyTransparentObjects.Contains(rend) && rend != mouseOverObject;

            if (shouldBeOpaque)
            {
                ResetObjectMaterial(rend);
            }
        }
    }

    private bool IsOccludingWithSphereCast(GameObject obj)
    {
        if (obj == null || playerTarget == null) return false;

        Vector3 rayOrigin = transform.position;
        Vector3 rayDirection = (playerTarget.position - rayOrigin).normalized;
        float rayDistance = Vector3.Distance(rayOrigin, playerTarget.position);

        RaycastHit hit;
        if (Physics.SphereCast(rayOrigin, sphereCastRadius, rayDirection, out hit, rayDistance + 0.1f, obstacleLayer)) 
        {
            if (hit.collider.gameObject == obj)
            {
                return true;
            }
        }
        return false;
    }

    private void SetTransparent(Renderer rend)
    {
        if (rend == null) return;

        Material mat = rend.material; 
        Color currentColor = mat.color;
        
        Color targetColor = new Color(originalAlbedoColors.ContainsKey(rend) ? originalAlbedoColors[rend].r : currentColor.r,
                                      originalAlbedoColors.ContainsKey(rend) ? originalAlbedoColors[rend].g : currentColor.g,
                                      originalAlbedoColors.ContainsKey(rend) ? originalAlbedoColors[rend].b : currentColor.b,
                                      transparentAlpha);

        mat.color = Color.Lerp(currentColor, targetColor, Time.deltaTime * fadeSpeed);
    }

    private bool ResetObjectMaterial(Renderer rend)
    {
        if (rend == null) return true; 

        if (originalAlbedoColors.ContainsKey(rend))
        {
            Color originalColor = originalAlbedoColors[rend];
            Material currentMat = rend.material; 
            
            currentMat.color = Color.Lerp(currentMat.color, originalColor, Time.deltaTime * fadeSpeed);

            // Si l'alpha est très proche de la valeur originale, on le fixe et on le retire des "objets transparents"
            if (Mathf.Abs(currentMat.color.a - originalColor.a) < 0.01f)
            {
                currentMat.color = originalColor; 
                originalAlbedoColors.Remove(rend); // Retire la couleur originale, indiquant qu'il est réinitialisé.
                return true; // Indique que l'objet est complètement réinitialisé
            }
        }
        else
        {
            // Si aucune couleur originale n'est trouvée, l'objet est déjà considéré comme réinitialisé.
             return true; 
        }
        return false; // Indique que l'objet n'est pas encore complètement réinitialisé
    }

    void OnDrawGizmos()
    {
        if (showDebugRay && playerTarget != null)
        {
            // Rayon rouge de la caméra au joueur
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, playerTarget.position);
            
            // Visualisation du SphereCast (sphères et ligne centrale)
            Gizmos.color = Color.blue;
            Vector3 rayOrigin = transform.position;
            Vector3 rayDirection = (playerTarget.position - rayOrigin).normalized;
            float rayDistance = Vector3.Distance(rayOrigin, playerTarget.position);

            Gizmos.DrawWireSphere(rayOrigin, sphereCastRadius);
            Gizmos.DrawWireSphere(rayOrigin + rayDirection * rayDistance, sphereCastRadius);
            
            // Dessine les lignes reliant les bords des sphères pour représenter le cylindre du SphereCast
            Gizmos.DrawLine(rayOrigin + transform.up * sphereCastRadius, rayOrigin + rayDirection * rayDistance + transform.up * sphereCastRadius);
            Gizmos.DrawLine(rayOrigin - transform.up * sphereCastRadius, rayOrigin + rayDirection * rayDistance - transform.up * sphereCastRadius);
            Gizmos.DrawLine(rayOrigin + transform.right * sphereCastRadius, rayOrigin + rayDirection * rayDistance + transform.right * sphereCastRadius);
            Gizmos.DrawLine(rayOrigin - transform.right * sphereCastRadius, rayOrigin + rayDirection * rayDistance - transform.right * sphereCastRadius);


            // Dessine des boîtes autour des objets actuellement transparents via SphereCast
            Gizmos.color = Color.magenta;
            foreach(Renderer rend in currentlyTransparentObjects)
            {
                if (rend != null) 
                {
                    Gizmos.DrawWireCube(rend.bounds.center, rend.bounds.size);
                }
            }
            // Dessine des boîtes autour de l'objet survolé par la souris
            if (mouseOverObject != null)
            {
                Gizmos.color = Color.yellow; // Une couleur différente pour l'objet survolé
                Gizmos.DrawWireCube(mouseOverObject.bounds.center, mouseOverObject.bounds.size);
            }
        }
    }
}