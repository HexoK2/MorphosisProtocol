using UnityEngine;

public class StickyTile : MonoBehaviour
{
    [Header("Configuration de l'Effet Collant")]
    [Tooltip("Définit si l'effet collant est permanent (true) ou temporaire (false).")]
    public bool isPermanent = false;
    
    [Tooltip("Durée de l'effet si non-permanent.")]
    public float effectDuration = 5.0f;
    
    [Tooltip("Multiplicateur de délai de chute pour les plateformes (1.0f pour pas de changement, >1.0f pour plus lent).")]
    public float fallDelayMultiplier = 2.0f;

    [Header("Effet Visuel")]
    [Tooltip("Matériel à appliquer quand le joueur est sur la tuile (optionnel).")]
    public Material activeMaterial;
    
    private Material originalMaterial;
    private Renderer tileRenderer;
    private bool playerOnTile = false;

    void Start()
    {
        // Récupère le renderer de la tuile pour les effets visuels
        tileRenderer = GetComponent<Renderer>();
        if (tileRenderer != null)
        {
            originalMaterial = tileRenderer.material;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Vérifie si l'objet qui entre en collision a bien le tag "Player"
        if (collision.gameObject.CompareTag("Player"))
        {
            playerOnTile = true;
            
            // Récupère le script PlayerMovement sur le joueur
            PlayerMovement playerMovement = collision.gameObject.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                // Calcule la durée réelle (0 pour permanent si c'est le cas)
                float duration = isPermanent ? 0.0f : effectDuration;
                
                // Appelle la méthode pour activer l'effet collant
                playerMovement.SetStickyState(true, duration, fallDelayMultiplier);
                
                Debug.Log($"🟡 Joueur entre sur StickyTile - Permanent: {isPermanent}, Durée: {duration}s");
            }

            // ✅ AJOUT : Effet visuel quand le joueur marche sur la tuile
            ApplyVisualEffect(true);
        }
    }

    void OnCollisionExit(Collision collision)
    {
        // Vérifie si l'objet qui quitte la collision est le joueur
        if (collision.gameObject.CompareTag("Player"))
        {
            playerOnTile = false;
            
            PlayerMovement playerMovement = collision.gameObject.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                // Si l'effet n'est pas permanent, on l'annule quand le joueur sort de la tuile
                if (!isPermanent)
                {
                    playerMovement.SetStickyState(false, 0, 1.0f);
                    Debug.Log("🟡 Joueur quitte StickyTile - Effet désactivé");
                }
                else
                {
                    Debug.Log("🟡 Joueur quitte StickyTile - Effet permanent maintenu");
                }
            }

            // ✅ AJOUT : Restaurer l'apparence normale
            ApplyVisualEffect(false);
        }
    }

    // ✅ NOUVELLE MÉTHODE : Gestion des effets visuels
    private void ApplyVisualEffect(bool isActive)
    {
        if (tileRenderer != null)
        {
            if (isActive && activeMaterial != null)
            {
                tileRenderer.material = activeMaterial;
            }
            else if (!isActive && originalMaterial != null)
            {
                tileRenderer.material = originalMaterial;
            }
        }
    }

    // ✅ MÉTHODE UTILITAIRE : Pour forcer l'arrêt de l'effet (si besoin depuis l'extérieur)
    public void ForceStopStickyEffect()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.SetStickyState(false, 0, 1.0f);
                Debug.Log("🟡 StickyTile: Effet forcé d'arrêter");
            }
        }
    }

    // ✅ GIZMO pour visualiser la tuile dans l'éditeur
    void OnDrawGizmos()
    {
        Gizmos.color = playerOnTile ? Color.yellow : Color.green;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
        
        // Affiche une icône différente selon le type
        if (isPermanent)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.6f, 0.2f);
        }
    }
}