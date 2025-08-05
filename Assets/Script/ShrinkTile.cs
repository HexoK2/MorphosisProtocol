using UnityEngine;
using System.Collections;

public class ShrinkTile : MonoBehaviour
{
    public enum ScaleEffectDurationType
    {
        Temporary,
        Permanent
    }

    [Tooltip("Le Layer du joueur pour la détection.")]
    public LayerMask playerLayer;

    private GameObject playerBall;
    private PlayerMovement playerMovementScript;

    [Header("Paramètres de l'effet ShrinkTile (redimensionnement)")]
    public ScaleEffectDurationType durationType = ScaleEffectDurationType.Temporary;

    [Tooltip("Taille que le joueur prendra en tombant sur la tuile rétrécissante.")]
    public float shrinkScale = 0.5f;

    [Tooltip("Durée pendant laquelle le joueur reste à cette taille réduite.")]
    public float shrinkDuration = 5.0f;

    void Start()
    {
        playerBall = GameObject.FindGameObjectWithTag("Player");
        if (playerBall != null)
        {
            playerMovementScript = playerBall.GetComponent<PlayerMovement>();
            if (playerMovementScript == null)
            {
                Debug.LogError("Le script PlayerMovement est introuvable sur le GameObject 'Player' !");
                enabled = false;
            }
        }
        else
        {
            Debug.LogError("Aucun GameObject avec le tag 'Player' trouvé dans la scène !");
            enabled = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            Debug.Log("Joueur a touché une ShrinkTile ! Application de l'effet...");
            if (playerMovementScript != null)
            {
                playerMovementScript.IsSmall = true; // Marque le joueur comme petit
                ApplyShrinkEffect(other.gameObject);
            }
        }
    }

    void ApplyShrinkEffect(GameObject player)
    {
        if (playerMovementScript != null)
        {
            // Arrêter le mouvement du Rigidbody pour une transition plus douce
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // Déterminer la durée en fonction du type de durée choisi
            float actualDuration = (durationType == ScaleEffectDurationType.Temporary) ? shrinkDuration : -1f;
            
            // Appelle la méthode de redimensionnement pour changer la taille
            playerMovementScript.ChangePlayerScale(shrinkScale, actualDuration);
        }
    }
}