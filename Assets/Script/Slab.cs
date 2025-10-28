using UnityEngine;

public class Slab : MonoBehaviour
{
    [Header("Références")]
    // Référence au script du joueur pour vérifier la mutation Sticky
    private PlayerMovement playerMovement;

    // Référence au gestionnaire central de l'énigme de séquence
    private SequenceManager sequenceManager;

    private Animator plateAnimator;

    // Mémorise si le bouton est actuellement enfoncé
    private bool isPressed = false;

    // PROPRIÉTÉ PUBLIQUE : Permet aux autres scripts (facultatif ici) de lire l'état
    public bool IsActive
    {
        get { return isPressed; }
    }

    void Start()
    {
        // Récupère les composants et références nécessaires
        plateAnimator = GetComponent<Animator>();

        // Trouver les gestionnaires
        playerMovement = FindObjectOfType<PlayerMovement>();
        sequenceManager = FindObjectOfType<SequenceManager>();

        // Vérifications de sécurité
        if (plateAnimator == null)
        {
            Debug.LogError("Animator manquant sur le bouton de pression.");
        }
        if (playerMovement == null)
        {
            Debug.LogError("PlayerMovement non trouvé. Vérifie qu'il est dans la scène.");
        }
        if (sequenceManager == null)
        {
            Debug.LogError("SequenceManager non trouvé. L'énigme ne pourra pas fonctionner.");
        }
    }

    // Appelé quand un objet entre en collision avec le bouton
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Vérifie si le bouton n'est pas déjà enfoncé ET si le joueur a la mutation Sticky
            // NOTE : J'utilise le nom PlayerMovement.IsSticky que tu as utilisé précédemment.
            if (!isPressed && playerMovement != null && playerMovement.IsSticky == true)
            {
                if (plateAnimator != null)
                {
                    // Animation : le bouton s'enfonce
                    plateAnimator.SetBool("IsActivated", true);
                }

                // Mémorise que le bouton est maintenant enfoncé
                isPressed = true;

                Debug.Log($"Bouton de pression activé par le joueur Sticky: {gameObject.name}");

                // 🔑 NOUVEAU : On informe le SequenceManager que cette dalle a été pressée
                if (sequenceManager != null)
                {
                    // On envoie le nom du GameObject de la dalle pour la vérification de la séquence
                    sequenceManager.RegisterSlabPress(gameObject.name);
                }
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

                Debug.Log($"Le joueur est parti, le bouton {gameObject.name} remonte.");

                // NOTE : On ne reset pas la séquence ici. C'est au SequenceManager de le faire 
                // si la dalle est incorrecte. La dalle remonte juste physiquement.
            }
        }
    }
}