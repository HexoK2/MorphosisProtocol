using UnityEngine;
using System.Collections;

public class PoisonPit : MonoBehaviour
{
    // Enum pour définir le type de durée de l'effet
    public enum ScaleEffectDurationType
    {
        Temporary, // L'effet dure un temps défini puis revient à la normale
        Permanent  // L'effet dure indéfiniment (le joueur reste gros)
    }

    [Tooltip("Le Layer du joueur pour la détection.")]
    public LayerMask playerLayer;

    private GameObject playerBall;
    private PlayerMovement playerMovementScript;

    [Header("Paramètres de l'effet PoisonPit (redimensionnement)")]
    [Tooltip("Type de durée pour l'effet de grossissement du poison.")]
    public ScaleEffectDurationType durationType = ScaleEffectDurationType.Temporary;
    [Tooltip("Taille que le joueur prendra en tombant dans le PoisonPit.")]
    public float poisonBoostScale = 2.0f;
    [Tooltip("Durée pendant laquelle le joueur reste à cette taille augmentée après être tombé dans le poison (seulement si 'Temporary').")]
    public float poisonBoostDuration = 5.0f;

    void Start()
    {
        playerBall = GameObject.FindGameObjectWithTag("Player");
        if (playerBall != null)
        {
            playerMovementScript = playerBall.GetComponent<PlayerMovement>();
            if (playerMovementScript == null)
            {
                Debug.LogError("Le script PlayerMovement est introuvable sur le GameObject 'Player' ! Assurez-vous qu'il est attaché.");
                enabled = false;
            }
        }
        else
        {
            Debug.LogError("Aucun GameObject avec le tag 'Player' trouvé dans la scène ! Assurez-vous que votre joueur a bien le tag 'Player'.");
            enabled = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Vérifie si l'objet qui est entré dans le trigger est sur le 'playerLayer'
        if (((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            Debug.Log("Joueur a touché le PoisonPit ! Application de l'effet grossissant...");
            // ✅ CORRECTION : Le PoisonPit rend le joueur GRAND (IsBig = true, IsSmall = false)
            if (playerMovementScript != null)
            {
                playerMovementScript.IsSmall = false; // Le joueur devient grand
                playerMovementScript.IsBig = true;    // Marque explicitement comme grand
            }
            ApplyPoisonEffect(other.gameObject);
        }
    }

    void ApplyPoisonEffect(GameObject player)
    {
        // Optionnel : Arrêter le mouvement du Rigidbody pour une transition plus douce
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Si le script PlayerMovement est présent, change la taille du joueur
        if (playerMovementScript != null)
        {
            // Déterminer la durée à passer à ChangePlayerScale en fonction du type de durée choisi
            float actualDuration = (durationType == ScaleEffectDurationType.Temporary) ? poisonBoostDuration : -1f;

            // Appliquer l'effet de redimensionnement
            playerMovementScript.ChangePlayerScale(poisonBoostScale, actualDuration);
            
            // ✅ IMPORTANT : Lancer la coroutine de respawn après l'effet
            StartCoroutine(RespawnAfterPoisonEffect(player));
        }
        else
        {
            Debug.LogError("Erreur : PlayerMovement script est null lors de l'application de l'effet PoisonPit.");
        }
    }

    IEnumerator RespawnAfterPoisonEffect(GameObject player)
    {
        // Petit délai pour voir l'effet visuel
        yield return new WaitForSeconds(0.2f);

        if (playerMovementScript != null)
        {
            // ✅ NOUVEAU : Utilise la méthode pour retourner à la tuile précédente
            playerMovementScript.ReturnToPreviousTile();
            Debug.Log("Joueur retourné à la tuile précédente après PoisonPit");
        }
        else
        {
            Debug.LogError("Erreur : PlayerMovement script est null lors de la réinitialisation de la position.");
        }
    }
}