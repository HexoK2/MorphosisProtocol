using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class SlipperyTile : MonoBehaviour
{
    [Tooltip("Le Layer du joueur pour la détection.")]
    public LayerMask playerLayer;

    private GameObject playerBall;
    private PlayerMovement playerMovementScript;

    [Header("Paramètres de l'effet SlipperyTile")]
    [Tooltip("Nombre de glissades consécutives après l'atterrissage.")]
    public int numberOfSlides = 1;

    [Tooltip("Délai avant la première glissade (en secondes).")]
    public float delayBeforeSlide = 0.1f;

    [Header("Directions de glissade")]
    [Tooltip("Activer la glissade vers la gauche.")]
    public bool allowLeft = true;
    
    [Tooltip("Activer la glissade vers la droite.")]
    public bool allowRight = true;
    
    [Tooltip("Activer la glissade vers l'avant.")]
    public bool allowForward = true;
    
    [Tooltip("Activer la glissade vers l'arrière.")]
    public bool allowBackward = true;

    [Header("État de la mutation")]
    [Tooltip("Indique si cette tuile a déjà appliqué la mutation glissante au joueur.")]
    private bool hasAppliedMutation = false;

    private bool isProcessingSlide = false;

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

    // ✅ Détection quand le joueur ATTERRIT sur la tuile
    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            Debug.Log("Joueur a touché une SlipperyTile !");
            
            // Appliquer la mutation permanente si ce n'est pas déjà fait
            if (!hasAppliedMutation && playerMovementScript != null)
            {
                ApplySlipperyMutation();
            }
            
            // Déclencher l'effet de glissade seulement si on ne glisse pas déjà
            if (!isProcessingSlide)
            {
                ApplySlipperyEffect(other.gameObject);
            }
        }
    }

    // ✅ Détection continue pendant que le joueur est SUR la tuile (pendant le saut)
    void OnTriggerStay(Collider other)
    {
        if (((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            // Appliquer la mutation permanente si ce n'est pas déjà fait
            if (!hasAppliedMutation && playerMovementScript != null)
            {
                ApplySlipperyMutation();
            }
        }
    }

    // ✅ NOUVELLE MÉTHODE : Applique la mutation glissante de façon permanente
    void ApplySlipperyMutation()
    {
        if (playerMovementScript != null)
        {
            // Marquer que la mutation a été appliquée
            hasAppliedMutation = true;
            
            // Activer le flag "IsSlippery" dans PlayerMovement
            // Note: Tu devras ajouter cette variable publique dans PlayerMovement.cs
            // public bool IsSlippery = false;
            
            Debug.Log("🧊 MUTATION GLISSANTE APPLIQUÉE ! Le joueur glissera désormais en permanence.");
            
            // Optionnel : Effet visuel ou sonore pour indiquer la mutation
        }
    }

    void ApplySlipperyEffect(GameObject player)
    {
        if (playerMovementScript != null && !isProcessingSlide)
        {
            // Lancer la coroutine de glissade
            StartCoroutine(PerformSlideSequence(player));
        }
    }

    IEnumerator PerformSlideSequence(GameObject player)
    {
        isProcessingSlide = true;

        // Attendre que le joueur termine son mouvement actuel
        while (playerMovementScript.isJumping || playerMovementScript.pathCalculated)
        {
            yield return null;
        }

        // Petit délai avant la première glissade
        yield return new WaitForSeconds(delayBeforeSlide);

        // Effectuer les glissades
        for (int i = 0; i < numberOfSlides; i++)
        {
            GameObject targetTile = GetRandomAdjacentTile();
            
            if (targetTile != null)
            {
                Debug.Log($"🧊 Glissade {i + 1}/{numberOfSlides} vers {targetTile.name}");
                
                // Créer un chemin avec seulement la tuile cible
                playerMovementScript.path.Clear();
                playerMovementScript.path.Add(targetTile.transform.position);
                playerMovementScript.currentPathIndex = 0;
                playerMovementScript.pathCalculated = true;
                
                // Attendre que le mouvement soit terminé
                while (playerMovementScript.isJumping || playerMovementScript.pathCalculated)
                {
                    yield return null;
                }
                
                // Si c'est une autre SlipperyTile, laisser cette tuile gérer la suite
                if (targetTile.CompareTag("SlipperyTile"))
                {
                    Debug.Log("Glissade sur une autre SlipperyTile détectée. Arrêt de cette séquence.");
                    break;
                }
            }
            else
            {
                Debug.Log($"Aucune tuile adjacente valide trouvée pour la glissade {i + 1}. Arrêt de la séquence.");
                break;
            }
        }

        isProcessingSlide = false;
        Debug.Log("Séquence de glissade terminée.");
    }

    GameObject GetRandomAdjacentTile()
    {
        if (playerMovementScript == null || playerMovementScript.currentGridCube == null)
        {
            Debug.LogWarning("Impossible de trouver la position actuelle du joueur.");
            return null;
        }

        Vector3 currentPos = playerMovementScript.currentGridCube.transform.position;
        List<GameObject> validNeighbors = new List<GameObject>();

        // Définir les directions possibles (gauche, droite, avant, arrière)
        List<Vector3> directions = new List<Vector3>();
        
        if (allowLeft) directions.Add(Vector3.left);
        if (allowRight) directions.Add(Vector3.right);
        if (allowForward) directions.Add(Vector3.forward);
        if (allowBackward) directions.Add(Vector3.back);

        // Si aucune direction n'est autorisée, retourner null
        if (directions.Count == 0)
        {
            Debug.LogWarning("Aucune direction de glissade n'est autorisée !");
            return null;
        }

        // Pour chaque direction, chercher une tuile valide
        foreach (Vector3 direction in directions)
        {
            // Calculer la position de la tuile adjacente
            Vector3 targetPos = currentPos + direction * playerMovementScript.cellSize;
            
            // Chercher une tuile à cette position
            GameObject adjacentTile = FindTileAtPosition(targetPos);
            
            if (adjacentTile != null && IsValidSlideTarget(adjacentTile, currentPos))
            {
                validNeighbors.Add(adjacentTile);
            }
        }

        // Si aucun voisin valide n'est trouvé, retourner null
        if (validNeighbors.Count == 0)
        {
            Debug.Log("Aucune tuile adjacente valide trouvée pour glisser.");
            return null;
        }

        // Retourner une tuile aléatoire parmi les voisins valides
        int randomIndex = Random.Range(0, validNeighbors.Count);
        return validNeighbors[randomIndex];
    }

    GameObject FindTileAtPosition(Vector3 position)
    {
        // Normaliser la position pour correspondre au système de grille
        Vector3 normalizedPos = new Vector3(
            Mathf.Round(position.x * 1000) / 1000,
            position.y,
            Mathf.Round(position.z * 1000) / 1000
        );

        // Chercher dans un petit rayon autour de la position
        Collider[] colliders = Physics.OverlapSphere(normalizedPos, 0.5f, playerMovementScript.gridLayer);
        
        foreach (Collider col in colliders)
        {
            // Vérifier que ce n'est pas un obstacle
            if (((1 << col.gameObject.layer) & playerMovementScript.obstacleLayer) == 0)
            {
                return col.gameObject;
            }
        }

        return null;
    }

    bool IsValidSlideTarget(GameObject tile, Vector3 currentPos)
    {
        if (tile == null) return false;

        // Ne pas glisser vers la tuile actuelle
        if (tile == playerMovementScript.currentGridCube) return false;

        // Ne pas glisser vers un obstacle
        if (((1 << tile.layer) & playerMovementScript.obstacleLayer) != 0) return false;

        // Éviter les tuiles dangereuses (PoisonPit, ShrinkTile)
        if (tile.CompareTag("PoisonPit") || tile.CompareTag("ShrinkTile"))
        {
            return false;
        }

        // Ne pas glisser vers un mur de mutation si le joueur est trop grand
        if (tile.CompareTag("MutationWall") && !playerMovementScript.IsSmall)
        {
            return false;
        }

        // Vérifier la distance horizontale et verticale
        float horizontalDistance = Vector2.Distance(
            new Vector2(currentPos.x, currentPos.z),
            new Vector2(tile.transform.position.x, tile.transform.position.z)
        );
        float verticalDifference = Mathf.Abs(currentPos.y - tile.transform.position.y);

        if (horizontalDistance > playerMovementScript.maxJumpDistance || 
            verticalDifference > playerMovementScript.maxVerticalJumpDifference)
        {
            return false;
        }

        // Vérifier qu'il n'y a pas d'obstacle sur la trajectoire
        RaycastHit hit;
        if (Physics.Linecast(currentPos, tile.transform.position, out hit, playerMovementScript.obstacleLayer))
        {
            if (hit.collider.gameObject != tile)
            {
                // Si le joueur est petit, il peut passer sous certains obstacles
                if (!playerMovementScript.IsSmall)
                {
                    return false;
                }
            }
        }

        return true;
    }
}