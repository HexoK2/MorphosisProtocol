using UnityEngine;
using System.Collections;
using System.Reflection;

public class SlabActivator : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Le GameObject à activer/désactiver (la tuile de grille)")]
    public GameObject targetObject;

    [Tooltip("Le nom de la dalle de pression à surveiller")]
    public string slabName = "PressurePlate";

    [Header("Options")]
    [Tooltip("Si vrai, le GameObject sera actif quand la dalle est enfoncée")]
    public bool activateWhenPressed = true;

    [Header("Délai et Persistence")]
    [Tooltip("Délai en secondes avant que la tuile ne se désactive quand le joueur quitte la dalle")]
    public float resetDelay = 2f;

    [Tooltip("Si vrai, la tuile reste active même après que le joueur quitte la dalle")]
    public bool stayActiveAfterLeaving = false;

    // Références privées
    private Slab slabScript;
    private bool wasPressed = false;
    private Coroutine resetCoroutine;

    // 🔑 LIGNE CORRIGÉE (Ancien code : private GridManager gridManager;)
    private PlayerMovement playerMovement;


    void Start()
    {
        // 1. Trouver la dalle
        GameObject slabObject = GameObject.Find(slabName);
        if (slabObject != null)
        {
            slabScript = slabObject.GetComponent<Slab>();
            if (slabScript == null)
                Debug.LogError($"Le GameObject '{slabName}' n'a pas de script Slab !");
        }
        else
        {
            Debug.LogError($"Aucun GameObject nommé '{slabName}' trouvé dans la scène !");
        }

        // 2. 🔑 CORRECTION : Trouver le PlayerMovement (qui gère la grille)
        // L'erreur venait du fait que le nom de la variable (playerMovement) n'était pas
        // cohérent avec le type (GridManager) dans le code précédent.
        playerMovement = FindObjectOfType<PlayerMovement>();
        if (playerMovement == null)
            Debug.LogError("Le script PlayerMovement (qui contient la grille) n'a pas été trouvé. La grille ne sera pas mise à jour.");

        // 3. Vérifier la cible et initialiser
        if (targetObject == null)
            Debug.LogError("Aucun GameObject cible assigné dans l'Inspector !");
        else
            targetObject.SetActive(!activateWhenPressed);

        // État initial de la grille (retirer si initialement inactive)
        if (playerMovement != null && !targetObject.activeSelf)
        {
            playerMovement.RemoveGridCube(targetObject);
        }
    }

    void Update()
    {
        if (slabScript != null && targetObject != null)
        {
            // Utilise la propriété publique IsActive
            bool isPressed = slabScript.IsActive;

            // Si l'état a changé
            if (isPressed != wasPressed)
            {
                if (isPressed)
                {
                    OnSlabPressed();
                }
                else
                {
                    OnSlabReleased();
                }

                wasPressed = isPressed;
            }
        }
    }

    private void OnSlabPressed()
    {
        // Annule le reset en cours
        if (resetCoroutine != null)
        {
            StopCoroutine(resetCoroutine);
            resetCoroutine = null;
        }

        // L'état actif est déterminé par 'activateWhenPressed'
        bool activate = activateWhenPressed;
        targetObject.SetActive(activate);

        // MISE À JOUR DE LA GRILLE
        if (playerMovement != null)
        {
            if (activate)
            {
                playerMovement.AddGridCube(targetObject);
            }
            else
            {
                playerMovement.RemoveGridCube(targetObject);
            }
        }

        Debug.Log($"✅ {targetObject.name} mis à jour par la dalle {slabName}");
    }

    private void OnSlabReleased()
    {
        if (stayActiveAfterLeaving)
        {
            Debug.Log($"🔒 {targetObject.name} reste dans son état actuel");
            return;
        }

        Debug.Log($"⏱️ Désactivation de {targetObject.name} dans {resetDelay} secondes...");
        resetCoroutine = StartCoroutine(ResetAfterDelay());
    }

    private IEnumerator ResetAfterDelay()
    {
        yield return new WaitForSeconds(resetDelay);

        // L'état final désiré est l'opposé de l'état "pressé"
        bool shouldBeActive = !activateWhenPressed;
        targetObject.SetActive(shouldBeActive);

        // MISE À JOUR DE LA GRILLE (l'inverse de ce qui s'est passé à la pression)
        if (playerMovement != null)
        {
            if (shouldBeActive)
            {
                playerMovement.AddGridCube(targetObject);
            }
            else
            {
                playerMovement.RemoveGridCube(targetObject);
            }
        }

        Debug.Log($"🔄 {targetObject.name} réinitialisé après {resetDelay}s");
        resetCoroutine = null;
    }

    public void ForceReset()
    {
        if (resetCoroutine != null)
        {
            StopCoroutine(resetCoroutine);
        }

        targetObject.SetActive(!activateWhenPressed);

        // MISE À JOUR DE LA GRILLE LORS DU RESET MANUEL
        if (playerMovement != null)
        {
            if (activateWhenPressed)
            {
                playerMovement.RemoveGridCube(targetObject);
            }
            else
            {
                playerMovement.AddGridCube(targetObject);
            }
        }

        Debug.Log($"🔄 {targetObject.name} réinitialisé manuellement");
    }
}