using UnityEngine;


public class Slab : MonoBehaviour
{
    [Header("Références")]
    private PlayerMovement playerMovement;
    private Animator plateAnimator;

    // Mémorise si le bouton est actuellement enfoncé
    private bool isPressed = false;

    // 🔑 PROPRIÉTÉ PUBLIQUE : Permet aux autres scripts de lire l'état
    public bool IsActive
    {
        get { return isPressed; }
    }

    void Start()
    {
        // Récupère les composants nécessaires
        plateAnimator = GetComponent<Animator>();
        // NOTE: Si le script de mouvement du joueur est le GridManager, il faut adapter
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
            }
        }
    }
}
