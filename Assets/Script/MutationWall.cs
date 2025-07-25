using UnityEngine;
using TMPro;
using System.Collections;

public class MutationWall : MonoBehaviour
{
    [Tooltip("Référence au GameObject du message d'erreur (ex: un TextMeshProUGUI) à afficher si le joueur est trop grand.")]
    public GameObject messageErreurUI;

    [Tooltip("La durée (en secondes) pendant laquelle le message d'erreur est visible.")]
    public float dureeAffichageMessage = 3.0f;

    private PlayerMovement playerMovementScript;
    private Collider wallCollider;

    // Ajout pour stocker l'état précédent de IsSmall et détecter les changements
    private bool previousIsSmallState;

    void Start()
    {
        GameObject playerBall = GameObject.FindGameObjectWithTag("Player");
        if (playerBall != null)
        {
            playerMovementScript = playerBall.GetComponent<PlayerMovement>();
            if (playerMovementScript == null)
            {
                Debug.LogError("MutationWall : Le script PlayerMovement est introuvable sur le GameObject 'Player' ! Assurez-vous qu'il est attaché.");
                enabled = false;
            }
        }
        else
        {
            Debug.LogError("MutationWall : Aucun GameObject avec le tag 'Player' trouvé dans la scène ! Assurez-vous que votre joueur a bien le tag 'Player'.");
            enabled = false;
        }

        wallCollider = GetComponent<Collider>();
        if (wallCollider == null)
        {
            Debug.LogError("MutationWall : Le composant Collider est introuvable sur ce GameObject de mur ! Ce script nécessite un collider.");
            enabled = false;
        }
        else if (wallCollider.isTrigger)
        {
            Debug.LogWarning("MutationWall : Le collider du mur est marqué 'Is Trigger'. Pour bloquer physiquement le joueur, cette case DOIT ÊTRE DÉCOCHÉE. Le script va tenter de le désactiver, mais la collision physique sera gérée différemment.");
        }

        if (messageErreurUI != null)
        {
            messageErreurUI.SetActive(false);
        }

        // Initialiser l'état précédent avec l'état actuel du joueur au démarrage
        if (playerMovementScript != null)
        {
            previousIsSmallState = playerMovementScript.IsSmall;
            // Appeler la fonction de mise à jour de l'état du collider dès le début
            UpdateWallColliderState();
        }
    }

    void Update()
    {
        // On vérifie l'état de IsSmall à chaque frame et on détecte si ça a changé
        if (playerMovementScript != null)
        {
            if (playerMovementScript.IsSmall != previousIsSmallState)
            {
                // Si l'état de IsSmall a changé, on met à jour le collider
                UpdateWallColliderState();
                previousIsSmallState = playerMovementScript.IsSmall; // Mettre à jour l'état précédent
            }
        }
    }

    // Nouvelle fonction pour gérer l'activation/désactivation du collider du mur
    void UpdateWallColliderState()
    {
        if (wallCollider == null) return; // Sécurité

        if (playerMovementScript.IsSmall)
        {
            // Si le joueur est petit, désactive le collider du mur
            wallCollider.enabled = false;
            Debug.Log("Le joueur est petit, le collider du mur est désactivé.");
        }
        else
        {
            // Si le joueur n'est PAS petit, active le collider du mur
            wallCollider.enabled = true;
            Debug.Log("Le joueur n'est PAS petit, le collider du mur est activé.");
        }
    }

    // On peut simplifier OnCollisionEnter maintenant
    void OnCollisionEnter(Collision collision)
    {
        // Vérifiez si l'objet qui entre en collision est le joueur
        if (collision.gameObject.CompareTag("Player") && playerMovementScript != null)
        {
            // Si le joueur est de taille normale ou grande, il ne peut pas passer
            // (le collider sera déjà activé grâce à UpdateWallColliderState)
            if (!playerMovementScript.IsSmall)
            {
                Debug.Log("Le joueur est trop grand pour passer ici !");
                StartCoroutine(AfficherMessageErreur());
            }
            // Si le joueur est petit, le collider est déjà désactivé, donc il passe sans message.
        }
    }

    // OnCollisionExit est toujours utile si le mur a des comportements spécifiques en sortie.
    // Pour ce cas d'usage, il peut être maintenu si tu veux réactiver le mur d'une autre manière,
    // mais la logique principale est maintenant dans UpdateWallColliderState.
    void OnCollisionExit(Collision collision)
    {
        // Tu pourrais vouloir réactiver le mur ici si tu le désactives avec OnCollisionEnter
        // et que tu veux qu'il se réactive après la traversée,
        // mais avec la nouvelle logique dans Update, ce n'est plus strictement nécessaire pour l'activation.
        // OnCollisionExit(Collision collision)
        // {
        // if (collision.gameObject.CompareTag("Player") && playerMovementScript != null)
        // {
        //     // Si le joueur quitte et qu'il n'est plus petit, on s'assure que le mur est activé.
        //     // Cependant, la méthode UpdateWallColliderState dans Update gère déjà ça.
        // }
        // }
    }


    IEnumerator AfficherMessageErreur()
    {
        if (messageErreurUI != null)
        {
            messageErreurUI.SetActive(true);
            yield return new WaitForSeconds(dureeAffichageMessage);
            messageErreurUI.SetActive(false);
        }
    }
}