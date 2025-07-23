using UnityEngine;
using System.Collections;

public class ShrinkTile : MonoBehaviour
{
    // Enum pour définir le type de durée de l'effet
    public enum ScaleEffectDurationType
    {
        Temporary, // L'effet dure un temps défini puis revient à la normale
        Permanent  // L'effet dure indéfiniment (le joueur reste petit)
    }

    [Tooltip("Le Layer du joueur pour la détection.")]
    public LayerMask playerLayer;

    private GameObject playerBall;
    private PlayerMovement playerMovementScript;

    [Header("Paramètres de l'effet ShrinkTile (redimensionnement)")]
    [Tooltip("Type de durée pour l'effet de rétrécissement.")]
    public ScaleEffectDurationType durationType = ScaleEffectDurationType.Temporary; // Nouvelle option
    [Tooltip("Taille que le joueur prendra en tombant sur la tuile rétrécissante.")]
    public float shrinkScale = 0.5f; // Taille spécifique de rétrécissement (ex: 0.5f pour moitié)
    [Tooltip("Durée pendant laquelle le joueur reste à cette taille réduite (seulement si 'Temporary').")]
    public float shrinkDuration = 5.0f; // Durée de l'effet rétrécissant

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
            Debug.Log("Joueur a touché une ShrinkTile ! Application de l'effet rétrécissant...");
            ApplyShrinkEffect(other.gameObject);
        }
    }

    void ApplyShrinkEffect(GameObject player)
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
            float actualDuration = (durationType == ScaleEffectDurationType.Temporary) ? shrinkDuration : -1f; // -1f pour "indéfini"

            playerMovementScript.ChangePlayerScale(shrinkScale, actualDuration); // Appelle la méthode de redimensionnement

            // Le joueur revient à la dernière position sûre après l'effet
            StartCoroutine(RespawnAfterShrinkEffect(player));
        }
        else
        {
            Debug.LogError("Erreur : PlayerMovement script est null lors de l'application de l'effet ShrinkTile.");
        }
    }

    IEnumerator RespawnAfterShrinkEffect(GameObject player)
    {
        // Attendre un très court instant pour laisser l'effet visuel de rétrécissement commencer
        yield return new WaitForSeconds(0.2f); // Délai avant le respawn

        if (playerMovementScript != null)
        {
            player.transform.position = playerMovementScript.lastSafePosition;
            Debug.Log($"Retour à la position sûre après ShrinkTile : {playerMovementScript.lastSafePosition}");
        }
        else
        {
            Debug.LogError("Erreur : PlayerMovement script est null lors de la réinitialisation de la position.");
        }
    }
}