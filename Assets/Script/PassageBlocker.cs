using UnityEngine;
using TMPro; // Nécessaire si vous utilisez TextMeshPro pour le message UI
using System.Collections; // Nécessaire pour les coroutines

public class PassageBlocker : MonoBehaviour
{
    [Tooltip("Référence au GameObject du message d'erreur (ex: un TextMeshProUGUI) à afficher si le joueur est trop grand.")]
    public GameObject messageErreurUI;

    [Tooltip("La durée (en secondes) pendant laquelle le message d'erreur est visible.")]
    public float dureeAffichageMessage = 3.0f;

    private PlayerMovement playerMovementScript; // Référence au script PlayerMovement du joueur
    private Collider passageCollider; // Référence au collider de ce GameObject (le mur/passage)

    void Start()
    {
        // Trouver et obtenir le script PlayerMovement du joueur
        GameObject playerBall = GameObject.FindGameObjectWithTag("Player");
        if (playerBall != null)
        {
            playerMovementScript = playerBall.GetComponent<PlayerMovement>();
            if (playerMovementScript == null)
            {
                Debug.LogError("PassageBlocker : Le script PlayerMovement est introuvable sur le GameObject 'Player' ! Assurez-vous qu'il est attaché.");
                enabled = false; // Désactive ce script si le PlayerMovement n'est pas trouvé
            }
        }
        else
        {
            Debug.LogError("PassageBlocker : Aucun GameObject avec le tag 'Player' trouvé dans la scène ! Assurez-vous que votre joueur a bien le tag 'Player'.");
            enabled = false; // Désactive ce script si le joueur n'est pas trouvé
        }

        // Récupérer le composant Collider de ce GameObject
        passageCollider = GetComponent<Collider>();
        if (passageCollider == null)
        {
            Debug.LogError("PassageBlocker : Le composant Collider est introuvable sur ce GameObject de passage ! Ce script nécessite un collider.");
            enabled = false; // Désactive ce script si aucun collider n'est trouvé
        }
        // L'ancienne version suggérait Is Trigger. Pour bloquer physiquement, il faut que Is Trigger soit DÉCOCHÉ.
        else if (passageCollider.isTrigger) 
        {
            Debug.LogWarning("PassageBlocker : Le collider du passage est marqué 'Is Trigger'. Pour bloquer physiquement le joueur, cette case DOIT ÊTRE DÉCOCHÉE sur le collider de ce GameObject.");
        }

        // S'assurer que le message d'erreur est désactivé au démarrage du jeu
        if (messageErreurUI != null)
        {
            messageErreurUI.SetActive(false);
        }
    }

    void OnCollisionEnter(Collision collision) // Utilisez OnCollisionEnter pour les collisions physiques
    {
        // Vérifiez si l'objet qui entre en collision est le joueur et si nous avons une référence à son script de mouvement
        if (collision.gameObject.CompareTag("Player") && playerMovementScript != null)
        {
            // Vérifier si le joueur N'EST PAS petit
            if (!playerMovementScript.IsSmall) 
            {
                Debug.Log("Le joueur est trop grand ou de taille normale pour passer ici !");
                // Le mur, avec son collider non-trigger, bloquera naturellement le joueur.
                StartCoroutine(AfficherMessageErreur()); // Affiche le message "Je suis trop grand..."
                // Assurez-vous que le collider du passage est bien activé pour bloquer
                if (passageCollider != null)
                {
                    passageCollider.enabled = true;
                }
            }
            else
            {
                // Le joueur est petit, il peut passer.
                // Pour permettre le passage, désactivez le collider du mur.
                Debug.Log("Le joueur est petit, il peut passer.");
                if (passageCollider != null)
                {
                    passageCollider.enabled = false; // Désactive le collider
                }
            }
        }
    }

    void OnCollisionExit(Collision collision) // Appelé lorsque le joueur quitte la collision avec ce GameObject
    {
        // Si le joueur n'est plus en contact (est passé ou a reculé)
        if (collision.gameObject.CompareTag("Player") && playerMovementScript != null)
        {
            // Réactiver le collider du mur une fois que le joueur est passé
            // Cela suppose que le "passage" se referme après que le joueur l'ait traversé.
            // Si le joueur est toujours petit, le collider restera désactivé.
            if (passageCollider != null && !passageCollider.enabled && !playerMovementScript.IsSmall)
            {
                 // Si le collider était désactivé et que le joueur n'est plus petit, le mur se "referme"
                passageCollider.enabled = true;
                Debug.Log("Collider du passage réactivé.");
            }
        }
    }

    // Coroutine pour afficher le message d'erreur pendant une durée définie
    IEnumerator AfficherMessageErreur()
    {
        if (messageErreurUI != null)
        {
            messageErreurUI.SetActive(true); // Active le GameObject du message
            yield return new WaitForSeconds(dureeAffichageMessage); // Attend la durée spécifiée
            messageErreurUI.SetActive(false); // Désactive le GameObject du message
        }
    }
}