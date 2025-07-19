using UnityEngine;

public class PoisonPit : MonoBehaviour
{
    [Tooltip("Le Layer du joueur pour la détection.")]
    public LayerMask playerLayer;

    // Référence à la position de départ de la boule
    private Vector3 startPosition;
    // Référence au GameObject de la boule pour la réinitialiser
    private GameObject playerBall;

    // Ajoutez un champ pour l'effet de dissolution si vous en avez un
    // public Material dissolveMaterial; // Matériau de dissolution pour la boule
    // public float dissolveDuration = 1.0f; // Durée de l'effet de dissolution

    void Start()
    {
        // Au démarrage, essayez de trouver la boule et sa position de départ.
        // C'est mieux si la boule est marquée d'un tag "Player".
        playerBall = GameObject.FindGameObjectWithTag("Player");
        if (playerBall != null)
        {
            startPosition = playerBall.transform.position;
        }
        else
        {
            Debug.LogError("Le GameObject avec le tag 'Player' n'a pas été trouvé. Assurez-vous que votre boule a le tag 'Player' !");
            enabled = false; // Désactive ce script si le joueur n'est pas trouvé
        }
    }

    // Cette fonction est appelée lorsqu'un autre Collider entre dans ce Trigger
    void OnTriggerEnter(Collider other)
    {
        // Vérifie si le collider qui est entré appartient au layer du joueur
        if (((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            Debug.Log("Joueur a touché le poison ! Retour à la case de départ.");
            ResetPlayerToStart(other.gameObject);
        }
    }

    void ResetPlayerToStart(GameObject player)
    {
        // Arrête le mouvement de la boule
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Lance l'effet de "fondue" (dissolution) si implémenté
        StartCoroutine(DissolveAndReset(player));
    }

    System.Collections.IEnumerator DissolveAndReset(GameObject player)
    {
        // --- PARTIE EFFET VISUEL DE DISSOLUTION (À ACTIVER SI VOUS AVEZ UN SHADER DE DISSOLUTION) ---
        /*
        Renderer playerRenderer = player.GetComponent<Renderer>();
        if (playerRenderer != null && dissolveMaterial != null)
        {
            Material originalMaterial = playerRenderer.material; // Sauvegarder le matériau original
            playerRenderer.material = dissolveMaterial; // Appliquer le matériau de dissolution

            float timer = 0f;
            while (timer < dissolveDuration)
            {
                timer += Time.deltaTime;
                // Si votre shader de dissolution a une propriété _DissolveAmount
                // playerRenderer.material.SetFloat("_DissolveAmount", timer / dissolveDuration);
                yield return null;
            }
            // Réinitialiser la boule après la dissolution complète
        }
        else
        {
            Debug.LogWarning("Impossible d'appliquer l'effet de dissolution. Vérifiez si la boule a un Renderer et si un Dissolve Material est assigné.");
            // Si pas d'effet de dissolution, attendez juste un court instant avant de réinitialiser
            yield return new WaitForSeconds(0.5f);
        }
        */
        // --- FIN PARTIE EFFET VISUEL ---

        // Dans tous les cas, attendez un court instant pour que le joueur réalise ce qui se passe
        yield return new WaitForSeconds(0.5f); // Temps d'attente avant la réinitialisation effective

        // Réinitialise la position de la boule
        player.transform.position = startPosition;

        // Réactive le matériau original si un effet de dissolution a été appliqué
        /*
        if (playerRenderer != null && originalMaterial != null)
        {
            playerRenderer.material = originalMaterial;
        }
        */

        Debug.Log("La boule est revenue à sa position de départ.");
    }
}