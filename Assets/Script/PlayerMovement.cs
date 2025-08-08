using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(LineRenderer))]
public class PlayerMovement : MonoBehaviour
{
    private Quaternion desiredRotation;
    private bool shouldRotate = false;
    public GameObject previousGridCube; // Nouvelle variable pour tracker la tuile précédente

[Header("Caméras")]
public Camera mainCamera; 
public Camera objectViewCamera;

    // --- Paramètres de Mouvement ---
    [Header("Paramètres de Mouvement")]

    [Tooltip("Facteur de ralentissement global, si 1.0, pas de ralentissement.")]
private float currentSpeedFactor = 1.0f; 
    [Tooltip("Vitesse de rotation du joueur.")]
public float rotationSpeed = 10f;
    [Tooltip("Vitesse de déplacement horizontale de la boule.")]
    public float horizontalSpeed = 5f;
    [Tooltip("Hauteur maximale du saut.")]
    public float jumpHeight = 2f;
    [Tooltip("Durée de chaque saut parabolique.")]
    public float jumpDuration = 0.5f;
    [Tooltip("Hauteur maximale que le joueur peut monter ou descendre en un seul saut.")]
    public float maxVerticalJumpDifference = 1.5f;
    [Tooltip("Offset vertical pour que la boule ne s'enfonce pas dans le sol.")]
    public float verticalOffsetOnGround = 0.5f;
    [Tooltip("Nombre maximal de cases que le joueur peut parcourir en un seul clic.")]
    public int maxPathLength = 3;

    // --- Paramètres de Redimensionnement ---
    [Header("Paramètres de Redimensionnement")]
    [Tooltip("Taille par défaut du joueur (échelle uniforme).")]
    public float defaultScale = 1.0f;
    [Tooltip("Taille du joueur lorsqu'il est boosté par un réactif (tuile verte).")]
    public float boostedScale = 1.5f;
    [Tooltip("Taille du joueur lorsqu'il est réduit par un réactif (tuile jaune).")]
    public float shrunkScale = 0.5f; 
    [Tooltip("Durée de la transition de taille (agrandissement ou réduction).")]
    public float scaleTransitionDuration = 0.3f;
    [Tooltip("Durée par défaut pendant laquelle le joueur reste à taille augmentée (pour les tuiles réactives).")]
    public float defaultBoostedDuration = 5.0f; 
    
    // --- NOUVEAU: Paramètres de Mutation (Petit/Grand) ---
    [Header("Mutation du joueur")]
    [Tooltip("Indique si le joueur est actuellement dans sa forme 'petite'.")]
    public bool _isSmall = false;

    public bool IsSmall
    {
        get { return _isSmall; }
        set { _isSmall = value; } // Setter public
    } // Propriété publique accessible en lecture, privée en écriture
    [Tooltip("La taille (scale uniforme) du joueur quand il est dans sa forme 'petite'.")]
    public float mutationSmallScale = 0.5f; // Scale spécifique pour la mutation "petit"
    [Tooltip("La taille (scale uniforme) du joueur quand il est dans sa forme 'normale'.")]
    public float mutationNormalScale = 1.0f; // Scale spécifique pour la mutation "normal"

    public bool IsBig = false; // Indique si le joueur est actuellement dans sa forme "grande" (pour les effets de PoisonPit)
    [Tooltip("Le collider principal du joueur (CharacterController ou CapsuleCollider).")]
    public Collider playerMainCollider; // Référence au collider du joueur pour ajuster sa taille

    private Coroutine scaleChangeCoroutine; // Pour gérer la coroutine de changement de taille

    
// ✅ AJOUTER ces variables dans la section "Mutation du joueur" de PlayerMovement.cs

[Header("État Collant")]
[Tooltip("Indique si le joueur est actuellement dans un état collant.")]
public bool IsSticky = false;

[Tooltip("Multiplicateur de délai pour les plaques qui tombent quand le joueur est collant.")]
public float stickyFallDelayMultiplier = 1.0f;

private Coroutine stickyEffectCoroutine; // Pour gérer la coroutine d'effet collant


    // --- Références et Grille ---
    [Header("Références Grille")]
    [Tooltip("Le LayerMask des objets de la grille (cubes).")]
    public LayerMask gridLayer;
    [Tooltip("Le LayerMask des objets qui sont des obstacles et ne peuvent pas être traversés.")]
    public LayerMask obstacleLayer;
    [Tooltip("La taille d'une case (longueur d'un côté du cube, utilisé pour Gizmos et références visuelles).")]
    public float cellSize = 1f;
    [Tooltip("La distance maximale entre les centres de deux cubes pour qu'ils soient considérés comme des voisins (un saut possible).")]
    public float maxJumpDistance = 2f;

    // --- Visualisation de la Sélection ---
    [Header("Visualisation de la Sélection")]
    public Material selectedCellMaterial;
    public Material hoveredCellMaterial;
    public Material outOfRangeCellMaterial;

    private Material defaultCellMaterial;
    private GameObject lastSelectedCube;
    private GameObject lastHoveredCube;

    // --- Debug/Visualisation du Chemin ---
    [Header("Debug/Visualisation du Chemin")]
    public bool showPath = true;
    public float lineWidth = 0.1f;

    // --- Effet de Vibration ---
    [Header("Effets de Feedback")]
    public float shakeDuration = 0.1f;
    public float shakeMagnitude = 0.1f;

private Vector3 initialCameraPosition;
    // --- NOUVEAU: Paramètres des Plaques Tombantes ---
    [Header("Plaques Tombantes")]
    public string fallingPlatformTag = "FallingPlatform";
    public float fallDelay = 0.5f;
    public float fallDuration = 1.5f;
    public float fallDistance = 10f;

    // --- NOUVEAU: Gestion de l'équipement ---
    [Header("Gestion de l'équipement")]
    [Tooltip("Indique si le joueur a actuellement une torche. Cochez cette case pour simuler la possession de la torche.")]
    public bool hasTorch = false; 

    public Rigidbody rb;
    public LineRenderer lr;
    public List<Vector3> path = new List<Vector3>();
    public int currentPathIndex = 0;
    private Vector3 startJumpPosition;
    private Vector3 targetJumpPosition;
    private float jumpTimer = 0f;
    private bool isJumping = false;
    public bool pathCalculated = false;

    public GameObject currentGridCube;
    private Dictionary<Vector3, GameObject> gridPositionsToCubes;

    private Dictionary<GameObject, Material> originalCellMaterials = new Dictionary<GameObject, Material>();

    // Variable pour stocker la dernière position sûre
    public Vector3 lastSafePosition;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        lr = GetComponent<LineRenderer>();

        // Tente de récupérer automatiquement le collider principal si non assigné
        if (playerMainCollider == null)
        {
            playerMainCollider = GetComponent<CharacterController>(); // Pour CharacterController
            if (playerMainCollider == null)
            {
                playerMainCollider = GetComponent<CapsuleCollider>(); // Pour CapsuleCollider
            }
            if (playerMainCollider == null)
            {
                Debug.LogWarning("Aucun CharacterController ou CapsuleCollider trouvé sur le joueur. La gestion de la taille du collider ne fonctionnera pas.", this);
            }
        }


        lr.positionCount = 0;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.useWorldSpace = true;

        rb.freezeRotation = true;

        InitializeGridCubes();
        if (currentGridCube != null)
        {
            transform.position = currentGridCube.transform.position + Vector3.up * verticalOffsetOnGround;
            // INITIALISER lastSafePosition à la position de départ
            // Assurez-vous que le cube de départ n'est pas un PoisonPit !
            if (!currentGridCube.CompareTag("PoisonPit"))
            {
                lastSafePosition = transform.position;
            }
            else
            {
                Debug.LogError("Le joueur démarre sur un PoisonPit ! Veuillez repositionner le joueur ou le PoisonPit.");
            }

            // Initialiser la taille par défaut du joueur (et la scale du collider)
            // Utilisez la scale de mutation normale comme scale initiale
            ApplyPlayerMutationSize(IsSmall); // Utilise la propriété IsSmall

            Renderer cubeRenderer = currentGridCube.GetComponent<Renderer>();
            if (cubeRenderer != null && cubeRenderer.sharedMaterial != null)
            {
                defaultCellMaterial = cubeRenderer.sharedMaterial;
            }
            else
            {
                Debug.LogWarning("Le cube de départ n'a pas de Renderer ou de Material. La sélection/survol visuel pourrait ne pas fonctionner.");
            }
        }
        else
        {
            Debug.LogError("Le joueur n'est pas placé sur un cube de la grille au démarrage !");
        }

        if (selectedCellMaterial == null) Debug.LogWarning("Le Material de sélection n'est pas assigné !");
        if (hoveredCellMaterial == null) Debug.LogWarning("Le Material de survol n'est pas assigné !");
        if (outOfRangeCellMaterial == null) Debug.LogWarning("Le Material 'hors de portée' n'est pas assigné !");
    }

    void Update()
    {
        // Seulement gérer le survol et l'input si le script est activé (i.e. non désactivé par PoisonPit)
        if (enabled)
        {
            HandleHover();
            HandleInput();
        }
        UpdatePathVisualization();

        // Exemple: Basculer la mutation avec une touche (par exemple 'T' pour "Transform")
        if (Input.GetKeyDown(KeyCode.T))
        {
            ToggleMutation();
        }
        if (shouldRotate)
{
    transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRotation, rotationSpeed * Time.deltaTime);

    // Si la rotation est quasiment atteinte, on arrête
    if (Quaternion.Angle(transform.rotation, desiredRotation) < 0.1f)
    {
        transform.rotation = desiredRotation;
        shouldRotate = false;
    }
}

    }
    
    void FixedUpdate()
    {
        // Seulement effectuer les sauts si le script est activé
        if (enabled)
        {
            if (isJumping)
            {
                PerformJump();
            }
            else if (pathCalculated && currentPathIndex < path.Count)
            {
                StartNextJump();
            }
            else if (pathCalculated && currentPathIndex >= path.Count)
            {
                pathCalculated = false;
                path.Clear();
                lr.positionCount = 0;
                rb.linearVelocity = Vector3.zero;
                transform.position = currentGridCube.transform.position + Vector3.up * verticalOffsetOnGround;
                ResetAllCellMaterials();
            }
        }
    }

    void InitializeGridCubes()
    {
        gridPositionsToCubes = new Dictionary<Vector3, GameObject>();
        Collider[] gridColliders = Physics.OverlapSphere(Vector3.zero, 500f, gridLayer);

        foreach (Collider col in gridColliders)
        {
            if (((1 << col.gameObject.layer) & obstacleLayer) == 0) 
            {
                Vector3 cubePos = new Vector3(Mathf.Round(col.transform.position.x * 1000) / 1000, col.transform.position.y, Mathf.Round(col.transform.position.z * 1000) / 1000);
                if (!gridPositionsToCubes.ContainsKey(cubePos))
                {
                    gridPositionsToCubes.Add(cubePos, col.gameObject);
                    Renderer renderer = col.gameObject.GetComponent<Renderer>();
                    if (renderer != null && renderer.sharedMaterial != null && !originalCellMaterials.ContainsKey(col.gameObject))
                    {
                        originalCellMaterials.Add(col.gameObject, renderer.sharedMaterial);
                    }
                }
            }
        }
        currentGridCube = FindNearestGridCube(transform.position);
    }

    public GameObject FindNearestGridCube(Vector3 position)
    {
        GameObject nearestCube = null;
        float minDistance = float.MaxValue;

        foreach (var entry in gridPositionsToCubes)
        {
            float dist = Vector2.Distance(new Vector2(position.x, position.z), new Vector2(entry.Key.x, entry.Key.z));
            if (dist < minDistance)
            {
                minDistance = dist;
                nearestCube = entry.Value;
            }
        }
        return nearestCube;
    }

    Vector3 SnapToNearestGridPosition(Vector3 pos)
    {
        return new Vector3(Mathf.Round(pos.x * 1000) / 1000, pos.y, Mathf.Round(pos.z * 1000) / 1000);
    }

    void HandleHover()
    {
        ResetAllCellMaterials();

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        GameObject currentHoveredCube = null;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, gridLayer))
        {
            GameObject potentialHoveredCube = hit.collider.gameObject;

            // Vérifier si c'est un PoisonPit ou un ShrinkTile pour l'affichage "hors de portée" au survol
            if (potentialHoveredCube.CompareTag("PoisonPit") || potentialHoveredCube.CompareTag("ShrinkTile")) 
            {
                Renderer cubeRenderer = potentialHoveredCube.GetComponent<Renderer>();
                if (cubeRenderer != null && outOfRangeCellMaterial != null)
                {
                    if (!originalCellMaterials.ContainsKey(potentialHoveredCube))
                    {
                        originalCellMaterials.Add(potentialHoveredCube, cubeRenderer.sharedMaterial);
                    }
                    cubeRenderer.material = outOfRangeCellMaterial; 
                }
                return;
            }

            List<GameObject> tempPath = CalculatePathForHover(currentGridCube, potentialHoveredCube);

            if (tempPath != null && tempPath.Count > 0 && tempPath.Count <= maxPathLength)
            {
                currentHoveredCube = potentialHoveredCube;
            }
            else if (tempPath == null || tempPath.Count > maxPathLength)
            {
                Renderer cubeRenderer = potentialHoveredCube.GetComponent<Renderer>();
                if (cubeRenderer != null && outOfRangeCellMaterial != null)
                {
                    if (!originalCellMaterials.ContainsKey(potentialHoveredCube))
                    {
                        originalCellMaterials.Add(potentialHoveredCube, cubeRenderer.sharedMaterial);
                    }
                    cubeRenderer.material = outOfRangeCellMaterial;
                }
            }
        }

        if (currentHoveredCube != null && currentHoveredCube != lastSelectedCube)
        {
            Renderer cubeRenderer = currentHoveredCube.GetComponent<Renderer>();
            if (cubeRenderer != null && hoveredCellMaterial != null)
            {
                if (!originalCellMaterials.ContainsKey(currentHoveredCube))
                {
                    originalCellMaterials.Add(currentHoveredCube, cubeRenderer.sharedMaterial);
                }
                cubeRenderer.material = hoveredCellMaterial;
                lastHoveredCube = currentHoveredCube;
            }
        }
        else
        {
            lastHoveredCube = null;
        }

        if (lastSelectedCube != null)
        {
            Renderer selectedRenderer = lastSelectedCube.GetComponent<Renderer>();
            if (selectedRenderer != null && selectedCellMaterial != null)
            {
                selectedRenderer.material = selectedCellMaterial;
            }
        }
    }


    public void ResetAllCellMaterials()
    {
        foreach (var entry in originalCellMaterials)
        {
            if (entry.Key != null)
            {
                Renderer renderer = entry.Key.GetComponent<Renderer>();
                if (renderer != null && renderer.sharedMaterial != entry.Value)
                {
                    renderer.material = entry.Value;
                }
            }
        }
        originalCellMaterials.Clear();

        if (lastSelectedCube != null)
        {
            Renderer selectedRenderer = lastSelectedCube.GetComponent<Renderer>();
            if (selectedRenderer != null && selectedRenderer.material != selectedCellMaterial)
            {
                if (originalCellMaterials.ContainsKey(lastSelectedCube))
                    selectedRenderer.material = originalCellMaterials[lastSelectedCube];
                else
                    selectedRenderer.material = defaultCellMaterial;
            }
        }
        lastSelectedCube = null;
        lastHoveredCube = null;
    }

    


void HandleInput()
{
    if (Input.GetMouseButtonDown(0))
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, gridLayer))
        {
            GameObject targetCube = hit.collider.gameObject;

            // Définir les tuiles qui ne doivent pas être des obstacles mais peuvent avoir un comportement spécial
            bool isTargetPoisonPit = targetCube.CompareTag("PoisonPit");
            bool isTargetShrinkTile = targetCube.CompareTag("ShrinkTile");
            bool isTargetBoostedTile = targetCube.CompareTag("StickyTile"); // J'ai ajouté le Tag de la tuile verte
            bool isTargetMutationWall = targetCube.CompareTag("MutationWall");

            // Vérifier si le chemin est valide et dans les limites de la portée
            List<GameObject> tempPath = GetShortestPath(currentGridCube, targetCube);

            if (tempPath == null || tempPath.Count == 0)
            {
                Debug.Log("Cible invalide ou inaccessible !");
                StartCoroutine(ShakeScreen());
                ResetAllCellMaterials();
                return;
            }

            // La vraie vérification de la longueur du chemin pour TOUTES les tuiles
            if (tempPath.Count > maxPathLength)
            {
                Debug.Log("Le chemin est trop long !");
                StartCoroutine(ShakeScreen());
                ResetAllCellMaterials();
                return;
            }
            
            // Vérifier si le joueur est trop grand pour passer un mur de mutation
            if (isTargetMutationWall && !IsSmall)
            {
                Debug.Log("Je suis trop grand pour passer ici !");
                StartCoroutine(ShakeScreen());
                ResetAllCellMaterials();
                return;
            }

            ResetAllCellMaterials();
            UpdateSelectedCubeVisual(targetCube);
            CalculatePathForMovement(targetCube);
        }
    }
}

  void UpdateSelectedCubeVisual(GameObject newSelectedCube)
{
    if (lastSelectedCube != null && originalCellMaterials.ContainsKey(lastSelectedCube))
    {
        Renderer oldRenderer = lastSelectedCube.GetComponent<Renderer>();
        if (oldRenderer != null) oldRenderer.material = originalCellMaterials[lastSelectedCube];
    }

    if (newSelectedCube != null)
    {
        // Ne pas sélectionner visuellement les tuiles spéciales ou les murs de mutation
        if (newSelectedCube.CompareTag("PoisonPit") || newSelectedCube.CompareTag("ShrinkTile") || newSelectedCube.CompareTag("StickyTile") || newSelectedCube.CompareTag("FallingPlatform")) 
        {
            lastSelectedCube = null;
            return;
        }

        Renderer newCubeRenderer = newSelectedCube.GetComponent<Renderer>();
        if (newCubeRenderer != null && selectedCellMaterial != null)
        {
            if (!originalCellMaterials.ContainsKey(newSelectedCube))
            {
                originalCellMaterials.Add(newSelectedCube, newCubeRenderer.sharedMaterial);
            }

            newCubeRenderer.material = selectedCellMaterial;
            lastSelectedCube = newSelectedCube;
        }
    }
    else
    {
        lastSelectedCube = null;
    }
}


    void CalculatePathForMovement(GameObject targetCube)
    {
        if (isJumping || pathCalculated || targetCube == null || currentGridCube == null) return;

        path.Clear();
        currentPathIndex = 0;

        if (targetCube == currentGridCube)
        {
            ResetAllCellMaterials();
            return;
        }

        List<GameObject> calculatedGameObjectsPath = GetShortestPath(currentGridCube, targetCube);

        if (calculatedGameObjectsPath != null && calculatedGameObjectsPath.Count > 0)
        {
            path = calculatedGameObjectsPath.Select(cube => cube.transform.position).ToList();
            pathCalculated = true;
        }
        else
        {
            Debug.LogWarning("Chemin invalide pour le mouvement (trop long ou obstacle non détecté plus tôt) !");
            pathCalculated = false;
            ResetAllCellMaterials();
            StartCoroutine(ShakeScreen());
        }
    }

    // Coroutine pour faire vibrer l'écran
private IEnumerator ShakeScreen()
{
    // Sauvegarder la position initiale de la caméra
    initialCameraPosition = mainCamera.transform.localPosition;
    float elapsed = 0.0f;

    while (elapsed < shakeDuration)
    {
        // Génère un vecteur de vibration aléatoire dans un cercle
        float x = Random.Range(-1f, 1f) * shakeMagnitude;
        float y = Random.Range(-1f, 1f) * shakeMagnitude;

        // Applique la vibration à la position de la caméra
        mainCamera.transform.localPosition = initialCameraPosition + new Vector3(x, y, 0);

        elapsed += Time.deltaTime;
        yield return null;
    }

    // Réinitialise la position de la caméra à sa position initiale après la vibration
    mainCamera.transform.localPosition = initialCameraPosition;
}

    List<GameObject> GetShortestPath(GameObject start, GameObject target)
    {
        if (start == null || target == null) return null;

        Queue<GameObject> queue = new Queue<GameObject>();
        Dictionary<GameObject, GameObject> cameFrom = new Dictionary<GameObject, GameObject>();
        Dictionary<GameObject, int> distance = new Dictionary<GameObject, int>();
        HashSet<GameObject> visited = new HashSet<GameObject>();

        queue.Enqueue(start);
        visited.Add(start);
        cameFrom[start] = null;
        distance[start] = 0;

        GameObject current = null;
        bool foundPath = false;

        while (queue.Count > 0)
        {
            current = queue.Dequeue();

            if (current == target)
            {
                foundPath = true; 
                break;
            }

            // Si la distance actuelle est déjà maxPathLength et que la cible n'est PAS une tuile spéciale ou un mur de mutation
            if (distance[current] >= maxPathLength && !(target.CompareTag("PoisonPit") || target.CompareTag("ShrinkTile") || target.CompareTag("MutationWall"))) 
            {
                continue;
            }

            foreach (GameObject neighbor in GetNeighbors(current)) // Appelle la méthode GetNeighbors modifiée
            {
                // La logique de vérification des obstacles et murs de mutation est maintenant dans GetNeighbors
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                    cameFrom[neighbor] = current;
                    distance[neighbor] = distance[current] + 1;

                    // Condition pour les tuiles non-spéciales et non-murs de mutation : si le chemin est trop long, on ne le prend pas
                    if (neighbor == target && distance[neighbor] > maxPathLength && !(neighbor.CompareTag("PoisonPit") || neighbor.CompareTag("ShrinkTile") || neighbor.CompareTag("MutationWall"))) 
                    {
                        foundPath = false; 
                        queue.Clear(); 
                        break; 
                    }
                }
            }
            if (queue.Count == 0 && current != target)
            {
                foundPath = false; 
                break;
            }
        }

        if (foundPath)
        {
            List<GameObject> pathObjects = new List<GameObject>();
            current = target;
            while (current != null)
            {
                pathObjects.Add(current);
                current = cameFrom[current];
            }
            pathObjects.Reverse();

            // S'assurer que le chemin n'inclut pas le point de départ
            if (pathObjects.Count > 0 && pathObjects[0] == start)
            {
                pathObjects.RemoveAt(0);
            }

            // Dernière vérification de la longueur du chemin pour les non-tuiles spéciales et non-murs de mutation
            if (pathObjects.Count > maxPathLength && !(target.CompareTag("PoisonPit") || target.CompareTag("ShrinkTile") || target.CompareTag("MutationWall"))) 
            {
                return null;
            }

            return pathObjects;
        }
        return null;
    }

    List<GameObject> CalculatePathForHover(GameObject startCube, GameObject targetCube)
    {
        // Une tuile spéciale ou un mur de mutation ne devrait pas être affichée comme une cible valide pour le survol
        if (targetCube.CompareTag("PoisonPit") || targetCube.CompareTag("ShrinkTile") || targetCube.CompareTag("MutationWall")) return null; 

        if (((1 << targetCube.layer) & obstacleLayer) != 0) return null;

        List<GameObject> tempPath = GetShortestPath(startCube, targetCube);
        return tempPath;
    }


List<GameObject> GetNeighbors(GameObject cube)
{
    List<GameObject> neighbors = new List<GameObject>();
    Vector3 cubePos = cube.transform.position;

    foreach (var entry in gridPositionsToCubes)
    {
        GameObject potentialNeighbor = entry.Value;
        if (potentialNeighbor == cube) continue;

        float horizontalDistance = Vector2.Distance(
            new Vector2(cubePos.x, cubePos.z),
            new Vector2(potentialNeighbor.transform.position.x, potentialNeighbor.transform.position.z)
        );
        float verticalDifference = Mathf.Abs(cubePos.y - potentialNeighbor.transform.position.y);

        if (horizontalDistance > maxJumpDistance || verticalDifference > maxVerticalJumpDifference)
        {
            continue;
        }

        // Vérification d'obstacle sur la trajectoire du saut
        RaycastHit hit;
        if (Physics.Linecast(cubePos, potentialNeighbor.transform.position, out hit, obstacleLayer))
        {
            if (hit.collider.gameObject != potentialNeighbor)
            {
                // Vérifier si le joueur est petit et peut passer sous l'obstacle
                if (IsSmall)
                {
                    continue; // Le joueur est petit et peut passer sous l'obstacle
                }
                else
                {
                    continue; // Trajectoire bloquée, ce n'est pas un voisin valide
                }
            }
        }

        bool isMutationWall = potentialNeighbor.CompareTag("MutationWall");
        bool isObstacleLayer = ((1 << potentialNeighbor.layer) & obstacleLayer) != 0;

        if (isObstacleLayer && !isMutationWall)
        {
            continue;
        }

        if (isMutationWall)
        {
            if (!IsSmall)
            {
                continue; // Le joueur est trop grand pour considérer le mur comme une destination
            }
        }

        neighbors.Add(potentialNeighbor);
    }

    return neighbors;
}




void StartNextJump()
{
    isJumping = true;
    jumpTimer = 0f;
    startJumpPosition = transform.position;
    targetJumpPosition = path[currentPathIndex];

    // Préparer la rotation vers la direction du saut
    Vector3 direction = (targetJumpPosition - startJumpPosition).normalized;
    direction.y = 0f;

    if (direction != Vector3.zero)
    {
        desiredRotation = Quaternion.LookRotation(direction);
        shouldRotate = true;
    }
}



    void PerformJump()
    {
        jumpTimer += Time.fixedDeltaTime;
        float progress = jumpTimer / jumpDuration;

        if (progress >= 1f)
        {
            transform.position = targetJumpPosition + Vector3.up * verticalOffsetOnGround;
            rb.linearVelocity = Vector3.zero;
            isJumping = false;

            // ✅ NOUVEAU : Sauvegarder la tuile actuelle comme tuile précédente AVANT de changer
            previousGridCube = currentGridCube;

            currentGridCube = FindNearestGridCube(transform.position);
            if (currentGridCube == null) Debug.LogError("Le joueur a atterri hors grille !");

            currentPathIndex++;

            // ✅ MODIFICATION : Ne mettre à jour lastSafePosition que si ce n'est PAS une tuile réactive
            if (currentGridCube != null &&
                !currentGridCube.CompareTag("PoisonPit") &&
                !currentGridCube.CompareTag("StickyTile") &&
                !currentGridCube.CompareTag("ShrinkTile"))
                
            {
                lastSafePosition = transform.position;
                Debug.Log($"Last Safe Position updated to: {lastSafePosition}");
            }

            if (currentPathIndex >= path.Count)
            {
                pathCalculated = false;
                path.Clear();
                lr.positionCount = 0;
                rb.linearVelocity = Vector3.zero;
                transform.position = currentGridCube.transform.position + Vector3.up * verticalOffsetOnGround;
                ResetAllCellMaterials();
                Debug.Log("Path completed.");
            }
        }
        else
        {
            Vector3 currentPosHorizontal = Vector3.Lerp(
                new Vector3(startJumpPosition.x, 0, startJumpPosition.z),
                new Vector3(targetJumpPosition.x, 0, targetJumpPosition.z),
                progress
            );

            float yInterpolated = Mathf.Lerp(startJumpPosition.y, targetJumpPosition.y + verticalOffsetOnGround, progress);
            float yParabolaOffset = jumpHeight * (4f * progress * (1f - progress));

            rb.MovePosition(new Vector3(currentPosHorizontal.x, yInterpolated + yParabolaOffset, currentPosHorizontal.z));

            Vector3 direction = (targetJumpPosition - transform.position);
            direction.y = 0f;
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            }
        }
    }

// ✅ NOUVELLE MÉTHODE : Pour revenir à la tuile précédente
public void ReturnToPreviousTile()
{
    if (previousGridCube != null)
    {
        Vector3 targetPosition = previousGridCube.transform.position + Vector3.up * verticalOffsetOnGround;
        transform.position = targetPosition;
        currentGridCube = previousGridCube;
        Debug.Log($"Joueur retourné à la tuile précédente : {previousGridCube.name}");
    }
    else
    {
        // Fallback sur lastSafePosition si pas de tuile précédente
        transform.position = lastSafePosition;
        Debug.Log("Pas de tuile précédente trouvée, retour à lastSafePosition");
    }
}


    // --- NOUVELLES/MODIFIÉES MÉTHODES POUR LA TAILLE DE LA BOULE ---

    // Méthode publique pour activer/désactiver la torche
    public void PickUpTorch()
    {
        hasTorch = true;
        Debug.Log("Torche ramassée !");
    }

    // Nouvelle fonction pour basculer la mutation (Petit/Normal)
    public void ToggleMutation()
    {
        IsSmall = !IsSmall; // Inverse l'état via la propriété
        ApplyPlayerMutationSize(IsSmall);   // Applique la nouvelle taille de mutation
        Debug.Log("Mutation activée ! Le joueur est maintenant " + (IsSmall ? "petit" : "normal") + ".");
    }

    // Applique la taille du joueur en fonction de sa mutation et ajuste le collider
    private void ApplyPlayerMutationSize(bool isCurrentlySmall)
    {
        float targetScale = isCurrentlySmall ? mutationSmallScale : mutationNormalScale;
        Vector3 finalGlobalScale = Vector3.one * targetScale;

        // Applique la scale au GameObject
        transform.localScale = finalGlobalScale;

        // Ajuste la taille du collider du joueur
        if (playerMainCollider != null)
        {
            if (playerMainCollider is CharacterController characterController)
            {
                // Ces valeurs doivent être ajustées pour correspondre à votre modèle de joueur
                characterController.height = isCurrentlySmall ? 1.0f : 2.0f; // Exemple de hauteur
                characterController.radius = isCurrentlySmall ? 0.25f : 0.5f; // Exemple de rayon
                // Assurez-vous que le centre du CharacterController est correct (généralement height / 2)
                characterController.center = new Vector3(0, characterController.height / 2f, 0);
            }
            else if (playerMainCollider is CapsuleCollider capsuleCollider)
            {
                capsuleCollider.height = isCurrentlySmall ? 1.0f : 2.0f; // Exemple de hauteur
                capsuleCollider.radius = isCurrentlySmall ? 0.25f : 0.5f; // Exemple de rayon
                capsuleCollider.center = new Vector3(0, capsuleCollider.height / 2f, 0);
            }
            else if (playerMainCollider is BoxCollider boxCollider)
            {
                // Si c'est un BoxCollider, ajustez sa taille (size)
                boxCollider.size = isCurrentlySmall ? new Vector3(0.5f, 1.0f, 0.5f) : new Vector3(1.0f, 2.0f, 1.0f); // Exemple de taille
                boxCollider.center = isCurrentlySmall ? new Vector3(0, 0.5f, 0) : new Vector3(0, 1.0f, 0); // Exemple de centre
            }
            // Ajoutez d'autres types de colliders si nécessaire
        }
        else
        {
            Debug.LogWarning("PlayerMainCollider non assigné ou non trouvé. Impossible d'ajuster la taille du collider.");
        }
    }


    // Méthode publique pour changer la taille, maintenant avec une durée paramétrable.
    // Une 'holdDuration' de -1f signifie "indéfiniment".
// Ajoute cette méthode dans PlayerMovement.cs ou modifie ChangePlayerScale
public void ChangePlayerScale(float targetUniformScale, float holdDuration)
{
    // Arrête toute coroutine de changement de taille en cours
    if (scaleChangeCoroutine != null)
    {
        StopCoroutine(scaleChangeCoroutine);
    }
    
    // ✅ AJOUT : Applique immédiatement la mutation du collider
    // Si on grossit (targetUniformScale > mutationNormalScale), on n'est plus petit
    // Si on rétrécit (targetUniformScale < mutationNormalScale), on devient petit
    if (targetUniformScale <= mutationSmallScale)
    {
        IsSmall = true;
        IsBig = false;
    }
    else if (targetUniformScale >= mutationNormalScale * 1.5f) // Seuil pour "gros"
    {
        IsSmall = false;
        IsBig = true;
    }
    else
    {
        IsSmall = false;
        IsBig = false;
    }
    
    // Applique immédiatement la taille du collider
    ApplyPlayerMutationSize(IsSmall);
    
    // Démarre la coroutine pour l'effet visuel
    scaleChangeCoroutine = StartCoroutine(ScalePlayerOverTime(targetUniformScale, holdDuration));
    }

    // Coroutine modifiée pour inclure une durée de maintien paramétrable et la gestion de "indéfini"
    private IEnumerator ScalePlayerOverTime(float targetUniformScale, float holdDuration)
    {
        Vector3 initialScale = transform.localScale;
        Vector3 finalScale = Vector3.one * targetUniformScale; // Utilise la targetUniformScale pour toutes les dimensions
        float elapsed = 0f;

        // Transition d'agrandissement/réduction
        while (elapsed < scaleTransitionDuration)
        {
            transform.localScale = Vector3.Lerp(initialScale, finalScale, elapsed / scaleTransitionDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = finalScale; // S'assurer d'atteindre la taille cible exacte

        // Si la durée de maintien est positive (non -1f), alors on attend et on revient à la taille par défaut
        if (holdDuration >= 0f) // IMPORTANT : Vérifier si ce n'est PAS un effet permanent
        {
            yield return new WaitForSeconds(holdDuration);
            // Revenir à la taille par défaut
            ChangePlayerScale(defaultScale, scaleTransitionDuration); // Utiliser la transitionDuration pour le retour
        }
        // Si holdDuration est -1f, la coroutine se termine ici et la taille reste à 'finalScale' indéfiniment.
        // On ne met pas scaleChangeCoroutine = null; ici si c'est permanent, car il n'y a pas de fin "naturelle"
        // de la coroutine pour revenir à la normale. Si vous avez besoin de forcer un retour à la normale pour un effet permanent
        // (par exemple, si le joueur touche un autre objet qui annule l'effet), vous devrez appeler ChangePlayerScale(defaultScale, ...) manuellement.
        if (holdDuration >= 0f) // Seulement si l'effet n'est PAS permanent
        {
            scaleChangeCoroutine = null; // La coroutine est terminée
        }
    }

    // --- FIN NOUVELLES MÉTHODES POUR LA TAILLE DE LA BOULE ---

    // ✅ NOUVELLE MÉTHODE : Pour activer/désactiver l'état collant
public void SetStickyState(bool isSticky, float duration, float fallDelayMultiplier)
{
    // Arrête toute coroutine d'effet collant en cours
    if (stickyEffectCoroutine != null)
    {
        StopCoroutine(stickyEffectCoroutine);
    }

    IsSticky = isSticky;
    stickyFallDelayMultiplier = fallDelayMultiplier;

    if (isSticky)
    {
        Debug.Log($"Le joueur devient collant pour {(duration > 0 ? duration.ToString() + " secondes" : "indéfiniment")} (multiplicateur: {fallDelayMultiplier}x)");
        
        // Si la durée est positive (non permanente), démarrer la coroutine pour désactiver l'effet
        if (duration > 0)
        {
            stickyEffectCoroutine = StartCoroutine(StickyEffectTimer(duration));
        }
    }
    else
    {
        Debug.Log("Le joueur n'est plus collant");
        stickyFallDelayMultiplier = 1.0f; // Remet le multiplicateur par défaut
    }
}

// ✅ NOUVELLE COROUTINE : Pour gérer la durée de l'effet collant
private IEnumerator StickyEffectTimer(float duration)
{
    yield return new WaitForSeconds(duration);
    
    // Désactive l'effet collant après la durée spécifiée
    SetStickyState(false, 0, 1.0f);
    stickyEffectCoroutine = null;
}

    void UpdatePathVisualization()
    {
        if (showPath && pathCalculated && path.Count > 0)
        {
            lr.positionCount = path.Count + 1;
            lr.SetPosition(0, currentGridCube.transform.position + Vector3.up * (0.1f + verticalOffsetOnGround));
            for (int i = 0; i < path.Count; i++)
            {
                lr.SetPosition(i + 1, path[i] + Vector3.up * (0.1f + verticalOffsetOnGround));
            }
        }
        else
        {
            lr.positionCount = 0;
        }
    }

void OnCollisionEnter(Collision collision)
{
    if (collision.gameObject.CompareTag(fallingPlatformTag))
    {
        StartCoroutine(FallPlatform(collision.gameObject));
    }
    else if (collision.gameObject.CompareTag("PoisonPit") || collision.gameObject.CompareTag("ShrinkTile"))
    {
        // Respawn à la dernière position sûre
        transform.position = lastSafePosition;
        Debug.Log("Le joueur a touché une tuile dangereuse et respawn à la dernière position sûre.");
    }
}


  // ✅ MODIFIER la méthode FallPlatform existante pour prendre en compte l'effet collant
IEnumerator FallPlatform(GameObject platform)
{
    if (!platform.activeSelf)
    {
        yield break;
    }

    // ✅ Appliquer le multiplicateur de délai si le joueur est collant
    float adjustedFallDelay = fallDelay * (IsSticky ? stickyFallDelayMultiplier : 1.0f);
    
    Debug.Log($"Plaque qui tombe - Délai: {adjustedFallDelay}s (normal: {fallDelay}s, collant: {IsSticky})");
    
    yield return new WaitForSeconds(adjustedFallDelay);

    Vector3 startPos = platform.transform.position;
    Vector3 endPos = platform.transform.position - Vector3.up * fallDistance;
    float elapsed = 0f;

    Collider platformCollider = platform.GetComponent<Collider>();
    if (platformCollider != null)
    {
        platformCollider.enabled = false;
    }

    while (elapsed < fallDuration)
    {
        platform.transform.position = Vector3.Lerp(startPos, endPos, elapsed / fallDuration);
        elapsed += Time.deltaTime;
        yield return null;
    }

    platform.transform.position = endPos;
    platform.SetActive(false);
}
    

    void OnDrawGizmos()
    {
        if (gridLayer.value == 0) return;

        if (gridPositionsToCubes != null)
        {
            Gizmos.color = Color.cyan;
            foreach (var entry in gridPositionsToCubes)
            {
                Gizmos.DrawWireCube(new Vector3(entry.Key.x, entry.Value.transform.position.y + 0.05f, entry.Key.z), new Vector3(cellSize, 0.1f, cellSize));
            }
        }

        if (currentGridCube != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(new Vector3(currentGridCube.transform.position.x, currentGridCube.transform.position.y + 0.1f, currentGridCube.transform.position.z), new Vector3(cellSize, 0.1f, cellSize));
        }

        if (showPath && pathCalculated && path.Count > 0)
        {
            Gizmos.color = Color.green; // Couleur pour le chemin
            for (int i = 0; i < path.Count - 1; i++)
            {
                Gizmos.DrawLine(path[i] + Vector3.up * (0.1f + verticalOffsetOnGround), path[i+1] + Vector3.up * (0.1f + verticalOffsetOnGround));
                Gizmos.DrawSphere(path[i] + Vector3.up * (0.1f + verticalOffsetOnGround), 0.05f);
            }
            Gizmos.DrawSphere(path[path.Count - 1] + Vector3.up * (0.1f + verticalOffsetOnGround), 0.05f); // Dernier point
        }
    }
}