using UnityEngine;

public class StickyTile : MonoBehaviour
{
    [Tooltip("Définit si l'effet collant est permanent (true) ou temporaire (false).")]
    public bool isPermanent = false;
    [Tooltip("Durée de l'effet si non-permanent.")]
    public float effectDuration = 5.0f;
    [Tooltip("Multiplicateur de délai de chute (1.0f pour pas de changement, >1.0f pour plus lent).")]
    public float fallDelayMultiplier = 2.0f;

    void OnCollisionEnter(Collision collision)
    {
        // Vérifie si l'objet qui entre en collision a bien le tag "Player"
        if (collision.gameObject.CompareTag("Player"))
        {
            // Récupère le script PlayerMovement sur le joueur
            PlayerMovement playerMovement = collision.gameObject.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                // Calcule la durée réelle (0 pour permanent si c'est le cas)
                float duration = isPermanent ? 0.0f : effectDuration;
                
                // Appelle la méthode pour activer l'effet collant
                playerMovement.SetStickyState(true, duration, fallDelayMultiplier);
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        // Vérifie si l'objet qui quitte la collision est le joueur
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerMovement playerMovement = collision.gameObject.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                // Si l'effet n'est pas permanent, on l'annule quand le joueur sort de la tuile
                if (!isPermanent)
                {
                    playerMovement.SetStickyState(false, 0, 1.0f);
                }
            }
        }
    }
}