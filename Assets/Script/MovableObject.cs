using UnityEngine;
using System.Collections;

public class MovableObject : MonoBehaviour
{
    [Header("Paramètres de l'objet mobile")]
    [Tooltip("Tag des plateformes sur lesquelles cet objet peut être posé (ex: 'PoisonPit').")]
    public string placementPlatformTag = "PoisonPit";
    
    [Tooltip("Hauteur au-dessus de la plateforme pour poser l'objet.")]
    public float heightAbovePlatform = 0.5f;
    
    [Tooltip("Durée de l'animation de déplacement de l'objet.")]
    public float moveDuration = 0.5f;

    [Tooltip("Layer que l'objet doit avoir une fois posé pour être considéré comme une tuile de grille.")]
    public LayerMask gridLayer;

    [Tooltip("Tag que l'objet doit avoir une fois posé (ex: 'GridCube').")]
    public string gridTag = "GridCube";

    [Header("Gestion de la plateforme poison")]
    [Tooltip("Si activé, désactive le PoisonPit sous l'objet quand il est posé.")]
    public bool disablePoisonPitWhenPlaced = true;

    [Header("Rotation de l'objet")]
    [Tooltip("Rotation cible quand l'objet est posé (ex: pour le mettre à plat).")]
    public Vector3 placedRotation = Vector3.zero;

    [Header("Feedback visuel")]
    [Tooltip("Matériau quand l'objet est sélectionné.")]
    public Material selectedMaterial;
    
    [Tooltip("Couleur du Gizmo pour les plateformes valides en mode sélection.")]
    public Color validPlacementGizmoColor = Color.green;

    private Material originalMaterial;
    private Renderer objectRenderer;
    private bool isSelected = false;
    private bool isMoving = false;
    private bool isPlaced = false;
    
    private static MovableObject currentlySelectedObject = null;
    private PlayerMovement playerMovement;
    private GameObject targetPlatform = null; // Stocke la plateforme ciblée

    void Start()
    {
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            originalMaterial = objectRenderer.material;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();
        }
        else
        {
            Debug.LogWarning("Impossible de trouver le joueur avec le tag 'Player'.");
        }
    }

    void Update()
    {
        if (isSelected && !isMoving)
        {
            HandlePlacement();
        }
    }

    void OnMouseDown()
    {
        if (isPlaced) return;
        if (isMoving) return;

        if (currentlySelectedObject != null && currentlySelectedObject != this)
        {
            currentlySelectedObject.Deselect();
        }

        if (isSelected)
        {
            Deselect();
        }
        else
        {
            Select();
        }
    }

    void Select()
    {
        isSelected = true;
        currentlySelectedObject = this;
        
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
            Debug.Log("🚫 Mouvements du joueur bloqués pendant le déplacement de l'objet.");
        }
        
        if (objectRenderer != null && selectedMaterial != null)
        {
            objectRenderer.material = selectedMaterial;
        }
        
        Debug.Log($"📦 Objet {gameObject.name} sélectionné ! Cliquez sur un PoisonPit (cube rouge) pour le placer.");
    }

    void Deselect()
    {
        isSelected = false;
        if (currentlySelectedObject == this)
        {
            currentlySelectedObject = null;
        }
        
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
            Debug.Log("✅ Mouvements du joueur débloqués.");
        }
        
        if (objectRenderer != null && originalMaterial != null)
        {
            objectRenderer.material = originalMaterial;
        }
        
        targetPlatform = null;
        Debug.Log($"📦 Objet {gameObject.name} désélectionné.");
    }

    void HandlePlacement()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            GameObject hitObject = hit.collider.gameObject;
            
            if (hitObject.CompareTag(placementPlatformTag))
            {
                targetPlatform = hitObject;

                if (Input.GetMouseButtonDown(0))
                {
                    PlaceObjectOnPlatform(hitObject);
                }
            }
            else
            {
                targetPlatform = null;
            }
        }
        else
        {
            targetPlatform = null;
        }

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            Deselect();
        }
    }

    void PlaceObjectOnPlatform(GameObject platform)
    {
        if (isMoving) return;

        Vector3 targetPosition = platform.transform.position + Vector3.up * heightAbovePlatform;
        StartCoroutine(MoveAndRotateToPosition(targetPosition, Quaternion.Euler(placedRotation), platform));

        Debug.Log($"📍 Placement de {gameObject.name} sur le PoisonPit {platform.name}");
    }

    IEnumerator MoveAndRotateToPosition(Vector3 targetPosition, Quaternion targetRotation, GameObject platform)
    {
        isMoving = true;
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            float t = elapsed / moveDuration;
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition;
        transform.rotation = targetRotation;
        isMoving = false;
        isPlaced = true;

        // Désactiver le PoisonPit sous l'objet
        if (disablePoisonPitWhenPlaced && platform != null)
        {
            // Retirer le PoisonPit de la grille de navigation
            if (playerMovement != null)
            {
                playerMovement.RemoveGridCube(platform);
            }

            // Désactiver le collider du PoisonPit pour qu'il ne tue plus le joueur
            Collider platformCollider = platform.GetComponent<Collider>();
            if (platformCollider != null)
            {
                platformCollider.enabled = false;
            }

            // Optionnel : Désactiver complètement le PoisonPit
            // platform.SetActive(false);

            // Optionnel : Rendre le PoisonPit invisible
            Renderer platformRenderer = platform.GetComponent<Renderer>();
            if (platformRenderer != null)
            {
                platformRenderer.enabled = false;
            }

            Debug.Log($"☠️ PoisonPit {platform.name} neutralisé !");
        }

        // Changer le layer et le tag de l'objet posé
        int gridLayerIndex = LayerMaskToLayerIndex(gridLayer);
        if (gridLayerIndex >= 0)
        {
            gameObject.layer = gridLayerIndex;
        }

        if (!string.IsNullOrEmpty(gridTag))
        {
            gameObject.tag = gridTag;
        }

        // Ajouter l'objet à la grille de navigation
        if (playerMovement != null)
        {
            playerMovement.AddGridCube(gameObject);
            Debug.Log($"✅ {gameObject.name} ajouté à la grille de navigation !");
        }

        Deselect();

        Debug.Log($"✅ {gameObject.name} placé avec succès ! Le PoisonPit est maintenant sûr.");
    }

    private int LayerMaskToLayerIndex(LayerMask layerMask)
    {
        int layerNumber = 0;
        int layer = layerMask.value;
        while (layer > 0)
        {
            layer = layer >> 1;
            layerNumber++;
        }
        return layerNumber - 1;
    }

    public void ForceDeselect()
    {
        if (isSelected)
        {
            Deselect();
        }
    }

    void OnDrawGizmos()
    {
        if (isSelected)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, 0.3f);

            // Highlight la plateforme ciblée
            if (targetPlatform != null)
            {
                Gizmos.color = validPlacementGizmoColor;
                Gizmos.DrawWireCube(targetPlatform.transform.position, targetPlatform.transform.localScale * 1.1f);
            }
        }

        if (isPlaced)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.2f);
        }
    }
}