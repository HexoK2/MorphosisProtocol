using UnityEngine;

public class Slab : MonoBehaviour
{
    [Header("Références")]
    private PlayerMovement playerMovement;
    private Animator plateAnimator;
    
    // Mémorise si le bouton est actuellement enfoncé
    private bool isPressed = false; 

    void Start()
    {
        // Récupère les composants nécessaires
        plateAnimator = GetComponent<Animator>();
        playerMovement = FindObjectOfType<PlayerMovement>();
        
        // Vérifications de sécurité
        if (plateAnimator == null)
        {
            Debug.LogError("Composant Animator manquant sur le bouton de pression !");
        }
        if (playerMovement == null)
        {
            Debug.LogError("PlayerMovement non trouvé ! Assure-toi qu'il est dans la scène.");
        }
    }

    // Appelé quand un objet entre en collision avec le bouton
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Vérifie si le bouton n'est pas déjà enfoncé ET si le joueur a la mutation Sticky
            if (!isPressed && playerMovement != null && playerMovement.IsSticky == true)
            {
                if (plateAnimator != null)
                {
                    // Animation : le bouton s'enfonce
                    plateAnimator.SetBool("IsActivated", true);
                }
                
                // Mémorise que le bouton est maintenant enfoncé
                isPressed = true;
                
                Debug.Log("Bouton de pression activé par le joueur Sticky.");
                
                // Ici tu peux ajouter des actions comme ouvrir une porte, etc.
                // OnButtonPressed();
            }
            else if (playerMovement != null && !playerMovement.IsSticky)
            {
                Debug.Log("Le joueur n'a pas la mutation Sticky, le bouton ne s'active pas.");
            }
        }
    }

    // Appelé quand un objet quitte la collision avec le bouton
    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Si le bouton était enfoncé, il remonte à sa position initiale
            if (isPressed)
            {
                if (plateAnimator != null)
                {
                    // Animation : le bouton remonte
                    plateAnimator.SetBool("IsActivated", false);
                }
                
                // Mémorise que le bouton n'est plus enfoncé
                isPressed = false;
                
                Debug.Log("Le joueur est parti, le bouton remonte à sa position initiale.");
                
                // Ici tu peux ajouter des actions comme fermer une porte, etc.
                // OnButtonReleased();
            }
        }
    }

    // Méthode optionnelle : actions quand le bouton est enfoncé
    private void OnButtonPressed()
    {
        // Exemple : ouvrir une porte, activer un mécanisme, etc.
        Debug.Log("Action déclenchée : porte ouverte, piège désactivé, etc.");
    }

    // Méthode optionnelle : actions quand le bouton est relâché
    private void OnButtonReleased()
    {
        // Exemple : fermer une porte, réactiver un piège, etc.
        Debug.Log("Action arrêtée : porte fermée, piège réactivé, etc.");
    }
}