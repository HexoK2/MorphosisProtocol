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
    public GameObject previousGridCube;

    [Header("Caméras")]
    public Camera mainCamera;
    public Camera objectViewCamera;

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

    [Header("Mutation du joueur")]
    [Tooltip("Indique si le joueur est actuellement dans sa forme 'petite'.")]
    public bool _isSmall = false;

    [Header("Mutation Glissante")]
    [Tooltip("Indique si le joueur a la mutation glissante.")]
    public bool IsSlippery = false;

    public bool IsSmall
    {
        get { return _isSmall; }
        set { _isSmall = value; }
    }
    
    [Tooltip("La taille (scale uniforme) du joueur quand il est dans sa forme 'petite'.")]
    public float mutationSmallScale = 0.5f;
    [Tooltip("La taille (scale uniforme) du joueur quand il est dans sa forme 'normale'.")]
    public float mutationNormalScale = 1.0f;

    public bool IsBig = false;
    [Tooltip("Le collider principal du joueur (CharacterController ou CapsuleCollider).")]
    public Collider playerMainCollider;

    private Coroutine scaleChangeCoroutine;

    [Header("État Collant")]
    [Tooltip("Indique si le joueur est actuellement dans un état collant.")]
    public bool IsSticky = false;
    [Tooltip("Multiplicateur de délai pour les plaques qui tombent quand le joueur est collant.")]
    public float stickyFallDelayMultiplier = 1.0f;
    [Tooltip("Multiplicateur de vitesse de saut quand le joueur est collant (0.5f = 50% plus lent).")]
    public float stickyJumpSpeedMultiplier = 0.5f;
    [Tooltip("Multiplicateur de durée de saut quand le joueur est collant (2.0f = 2x plus long).")]
    public float stickyJumpDurationMultiplier = 2.0f;

    private Coroutine stickyEffectCoroutine;

    [Header("Références Grille")]
    [Tooltip("Le LayerMask des objets de la grille (cubes).")]
    public LayerMask gridLayer;
    [Tooltip("Le LayerMask des objets qui sont des obstacles et ne peuvent pas être traversés.")]
    public LayerMask obstacleLayer;
    [Tooltip("La taille d'une case (longueur d'un côté du cube, utilisé pour Gizmos et références visuelles).")]
    public float cellSize = 1f;
    [Tooltip("La distance maximale entre les centres de deux cubes pour qu'ils soient considérés comme des voisins (un saut possible).")]
    public float maxJumpDistance = 2f;

    // ========================================
    // ✨ NOUVEAU : SYSTÈME DE BLOCAGE PAR DÉCORS
    // ========================================
    [Header("Blocage des tuiles par décors")]
    [Tooltip("Le LayerMask des objets qui bloquent la sélection (pièces, caisses, armoires, etc.)")]
    public LayerMask blockingDecorLayer;
    [Tooltip("Tag alternatif pour identifier les décors bloquants (si vous préférez utiliser des tags)")]
    public string blockingDecorTag = "BlockingDecor";
    [Tooltip("Utiliser le LayerMask (true) ou le Tag (false) pour détecter les décors bloquants")]
    public bool useLayerForBlocking = true;
    [Tooltip("Hauteur maximale de détection au-dessus de la tuile pour vérifier les décors")]
    public float decorDetectionHeight = 2.0f;

    [Header("Visualisation de la Sélection")]
    public Material selectedCellMaterial;
    public Material hoveredCellMaterial;
    public Material outOfRangeCellMaterial;

    private Material defaultCellMaterial;
    private GameObject lastSelectedCube;
    private GameObject lastHoveredCube;

    [Header("Debug/Visualisation du Chemin")]
    public bool showPath = true;
    public float lineWidth = 0.1f;

    [Header("Effets de Feedback")]
    public float shakeDuration = 0.1f;
    public float shakeMagnitude = 0.1f;

    private Vector3 initialCameraPosition;

    [Header("Plaques Tombantes")]
    public string fallingPlatformTag = "FallingPlatform";
    public float fallDelay = 0.5f;
    public float fallDuration = 1.5f;
    public float fallDistance = 10f;

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
    public bool isJumping = false;
    public bool pathCalculated = false;

    public GameObject currentGridCube;
    private Dictionary<Vector3, GameObject> gridPositionsToCubes;
    private Dictionary<GameObject, Material> originalCellMaterials = new Dictionary<GameObject, Material>();
    public Vector3 lastSafePosition;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        lr = GetComponent<LineRenderer>();

        if (playerMainCollider == null)
        {
            playerMainCollider = GetComponent<CharacterController>();
            if (playerMainCollider == null)
            {
                playerMainCollider = GetComponent<CapsuleCollider>();
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
            
            if (!currentGridCube.CompareTag("PoisonPit"))
            {
                lastSafePosition = transform.position;
            }
            else
            {
                Debug.LogError("Le joueur démarre sur un PoisonPit ! Veuillez repositionner le joueur ou le PoisonPit.");
            }

            ApplyPlayerMutationSize(IsSmall);

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
        if (enabled)
        {
            HandleHover();
            HandleInput();
        }
        UpdatePathVisualization();

        if (Input.GetKeyDown(KeyCode.T))
        {
            ToggleMutation();
        }
        
        if (shouldRotate)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRotation, rotationSpeed * Time.deltaTime);

            if (Quaternion.Angle(transform.rotation, desiredRotation) < 0.1f)
            {
                transform.rotation = desiredRotation;
                shouldRotate = false;
            }
        }
    }

    void FixedUpdate()
    {
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

    // ========================================
    // ✨ NOUVELLE MÉTHODE : Vérifie si une tuile est bloquée par un décor
    // Cette fonction est le cœur du système de blocage.
    // Elle projette une boîte invisible au-dessus de chaque tuile pour détecter
    // si un objet (pièce, caisse, armoire) empêche le joueur d'y atterrir.
    // ========================================
    private bool IsTileBlockedByDecor(GameObject tile)
    {
        // Si la tuile n'existe pas, on considère qu'elle n'est pas bloquée
        if (tile == null) return false;

        // On commence la détection légèrement au-dessus de la tuile pour éviter
        // de détecter la tuile elle-même
        Vector3 tileCenter = tile.transform.position + Vector3.up * 0.1f;
        
        // On crée une boîte de détection qui s'étend vers le haut.
        // La largeur de la boîte est légèrement plus petite que la cellule (0.4f au lieu de 0.5f)
        // pour éviter les faux positifs sur les bords entre tuiles adjacentes
        Vector3 boxHalfExtents = new Vector3(cellSize * 0.4f, decorDetectionHeight * 0.5f, cellSize * 0.4f);
        Vector3 boxCenter = tileCenter + Vector3.up * (decorDetectionHeight * 0.5f);

        if (useLayerForBlocking)
        {
            // Méthode 1 : Détection par LayerMask (recommandée pour les performances)
            // Cette approche est plus rapide car Unity filtre directement les objets
            // par leur layer sans avoir à vérifier chaque collider individuellement
            Collider[] hitColliders = Physics.OverlapBox(boxCenter, boxHalfExtents, Quaternion.identity, blockingDecorLayer);
            
            if (hitColliders.Length > 0)
            {
                // Feedback optionnel pour le développeur en mode debug
                // Décommentez la ligne suivante si vous voulez voir quel objet bloque
                // Debug.Log($"Tuile {tile.name} bloquée par : {hitColliders[0].gameObject.name}");
                return true;
            }
        }
        else
        {
            // Méthode 2 : Détection par Tag (plus simple à configurer mais moins performante)
            // Cette approche détecte tous les colliders puis filtre par tag
            Collider[] hitColliders = Physics.OverlapBox(boxCenter, boxHalfExtents, Quaternion.identity);
            
            foreach (Collider col in hitColliders)
            {
                // On vérifie que l'objet a le bon tag ET qu'il n'est pas la tuile elle-même
                // Cette double vérification évite que la tuile se bloque elle-même
                if (col.gameObject != tile && col.gameObject.CompareTag(blockingDecorTag))
                {
                    // Debug.Log($"Tuile {tile.name} bloquée par : {col.gameObject.name}");
                    return true;
                }
            }
        }

        // Si aucun décor n'a été détecté, la tuile est libre
        return false;
    }

    // ========================================
    // MÉTHODE MODIFIÉE : HandleHover avec vérification des décors
    // Cette méthode gère l'affichage visuel quand on survole une tuile avec la souris
    // ========================================
    void HandleHover()
    {
        ResetAllCellMaterials();

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        GameObject currentHoveredCube = null;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, gridLayer))
        {
            GameObject potentialHoveredCube = hit.collider.gameObject;

            // ✨ VÉRIFICATION PRIORITAIRE : Si la tuile est bloquée par un décor,
            // on l'affiche en rouge et on arrête le traitement ici.
            // C'est important de faire cette vérification AVANT les autres car
            // un décor rend la tuile inaccessible même si elle serait normalement valide
            if (IsTileBlockedByDecor(potentialHoveredCube))
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
                return; // On quitte la fonction pour empêcher le survol normal
            }

            // Vérification des tuiles spéciales (PoisonPit, ShrinkTile, etc.)
            if (potentialHoveredCube.CompareTag("PoisonPit") || potentialHoveredCube.CompareTag("ShrinkTile") || 
                potentialHoveredCube.CompareTag("SlipperyTile") || potentialHoveredCube.CompareTag("Poison"))
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

    // ========================================
    // MÉTHODE MODIFIÉE : HandleInput avec vérification des décors
    // Cette méthode gère le clic sur une tuile pour déplacer le joueur
    // ========================================
    void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, gridLayer))
            {
                GameObject targetCube = hit.collider.gameObject;

                // ✨ VÉRIFICATION PRIORITAIRE : Bloquage par décor
                // Si un joueur essaie de cliquer sur une tuile bloquée par un décor,
                // on refuse l'action avec un feedback visuel (tremblement d'écran)
                if (IsTileBlockedByDecor(targetCube))
                {
                    Debug.Log("Cette tuile est bloquée par un décor (pièce, caisse, armoire, etc.) !");
                    StartCoroutine(ShakeScreen());
                    ResetAllCellMaterials();
                    return; // Empêche complètement la sélection
                }

                bool isTargetPoisonPit = targetCube.CompareTag("PoisonPit");
                bool isTargetShrinkTile = targetCube.CompareTag("ShrinkTile");
                bool isTargetBoostedTile = targetCube.CompareTag("StickyTile");
                bool isTargetSlipperyTile = targetCube.CompareTag("SlipperyTile");
                bool isTargetMutationWall = targetCube.CompareTag("MutationWall");
                bool isTargetPoison = targetCube.CompareTag("Poison");

                List<GameObject> tempPath = GetShortestPath(currentGridCube, targetCube);

                if (tempPath == null || tempPath.Count == 0)
                {
                    Debug.Log("Cible invalide ou inaccessible !");
                    StartCoroutine(ShakeScreen());
                    ResetAllCellMaterials();
                    return;
                }

                if (tempPath.Count > maxPathLength)
                {
                    Debug.Log("Le chemin est trop long !");
                    StartCoroutine(ShakeScreen());
                    ResetAllCellMaterials();
                    return;
                }

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
            if (newSelectedCube.CompareTag("PoisonPit") || newSelectedCube.CompareTag("ShrinkTile") || 
                newSelectedCube.CompareTag("StickyTile") || newSelectedCube.CompareTag("FallingPlatform") || 
                newSelectedCube.CompareTag("SlipperyTile") || newSelectedCube.CompareTag("Poison"))
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
            Debug.LogWarning("Chemin invalide pour le mouvement !");
            pathCalculated = false;
            ResetAllCellMaterials();
            StartCoroutine(ShakeScreen());
        }
    }

    private IEnumerator ShakeScreen()
    {
        if (mainCamera == null)
        {
            Debug.LogWarning("Main Camera n'est pas assignée pour l'effet de vibration.");
            yield break;
        }
        
        initialCameraPosition = mainCamera.transform.localPosition;
        float elapsed = 0.0f;

        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;
            mainCamera.transform.localPosition = initialCameraPosition + new Vector3(x, y, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }

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

            if (distance[current] >= maxPathLength && !(target.CompareTag("PoisonPit") || target.CompareTag("ShrinkTile") || 
                target.CompareTag("MutationWall") || target.CompareTag("SlipperyTile")))
            {
                continue;
            }

            foreach (GameObject neighbor in GetNeighbors(current))
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                    cameFrom[neighbor] = current;
                    distance[neighbor] = distance[current] + 1;

                    if (neighbor == target && distance[neighbor] > maxPathLength && 
                        !(neighbor.CompareTag("PoisonPit") || neighbor.CompareTag("ShrinkTile") || 
                        neighbor.CompareTag("MutationWall") || neighbor.CompareTag("SlipperyTile")))
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

            if (pathObjects.Count > 0 && pathObjects[0] == start)
            {
                pathObjects.RemoveAt(0);
            }

            if (pathObjects.Count > maxPathLength && !(target.CompareTag("PoisonPit") || target.CompareTag("ShrinkTile") || 
                target.CompareTag("MutationWall") || target.CompareTag("SlipperyTile")))
            {
                return null;
            }

            return pathObjects;
        }
        return null;
    }

    List<GameObject> CalculatePathForHover(GameObject startCube, GameObject targetCube)
    {
        if (targetCube.CompareTag("PoisonPit") || targetCube.CompareTag("ShrinkTile") || 
            targetCube.CompareTag("MutationWall") || targetCube.CompareTag("SlipperyTile")) return null;

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

            RaycastHit hit;
            if (Physics.Linecast(cubePos, potentialNeighbor.transform.position, out hit, obstacleLayer))
            {
                if (hit.collider.gameObject != potentialNeighbor)
                {
                    if (IsSmall)
                    {
                        continue;
                    }
                    else
                    {
                        continue;
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
                    continue;
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
        float currentJumpDuration = jumpDuration * (IsSticky ? stickyJumpDurationMultiplier : 1.0f);
        float currentHorizontalSpeed = horizontalSpeed * (IsSticky ? stickyJumpSpeedMultiplier : 1.0f);

        jumpTimer += Time.fixedDeltaTime;
        float progress = jumpTimer / currentJumpDuration;

        if (progress >= 1f)
        {
            transform.position = targetJumpPosition + Vector3.up * verticalOffsetOnGround;
            rb.linearVelocity = Vector3.zero;
            isJumping = false;

            previousGridCube = currentGridCube;
            currentGridCube = FindNearestGridCube(transform.position);
            if (currentGridCube == null) Debug.LogError("Le joueur a atterri hors grille !");

            currentPathIndex++;

            if (currentGridCube != null &&
                !currentGridCube.CompareTag("PoisonPit") &&
                !currentGridCube.CompareTag("StickyTile") &&
                !currentGridCube.CompareTag("ShrinkTile") &&
                !currentGridCube.CompareTag("SlipperyTile"))
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
                float currentRotationSpeed = rotationSpeed * (IsSticky ? stickyJumpSpeedMultiplier : 1.0f);
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, currentRotationSpeed * Time.fixedDeltaTime);
            }
        }
    }

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
            transform.position = lastSafePosition;
            Debug.Log("Pas de tuile précédente trouvée, retour à lastSafePosition");
        }
    }

    public void PickUpTorch()
    {
        hasTorch = true;
        Debug.Log("Torche ramassée !");
    }

    public void ToggleMutation()
    {
        IsSmall = !IsSmall;
        ApplyPlayerMutationSize(IsSmall);
        Debug.Log("Mutation activée ! Le joueur est maintenant " + (IsSmall ? "petit" : "normal") + ".");
    }

    private void ApplyPlayerMutationSize(bool isCurrentlySmall)
    {
        float targetScale = isCurrentlySmall ? mutationSmallScale : mutationNormalScale;
        Vector3 finalGlobalScale = Vector3.one * targetScale;

        transform.localScale = finalGlobalScale;

        if (playerMainCollider != null)
        {
            if (playerMainCollider is CharacterController characterController)
            {
                characterController.height = isCurrentlySmall ? 1.0f : 2.0f;
                characterController.radius = isCurrentlySmall ? 0.25f : 0.5f;
                characterController.center = new Vector3(0, characterController.height / 2f, 0);
            }
            else if (playerMainCollider is CapsuleCollider capsuleCollider)
            {
                capsuleCollider.height = isCurrentlySmall ? 1.0f : 2.0f;
                capsuleCollider.radius = isCurrentlySmall ? 0.25f : 0.5f;
                capsuleCollider.center = new Vector3(0, capsuleCollider.height / 2f, 0);
            }
            else if (playerMainCollider is BoxCollider boxCollider)
            {
                boxCollider.size = isCurrentlySmall ? new Vector3(0.5f, 1.0f, 0.5f) : new Vector3(1.0f, 2.0f, 1.0f);
                boxCollider.center = isCurrentlySmall ? new Vector3(0, 0.5f, 0) : new Vector3(0, 1.0f, 0);
            }
        }
        else
        {
            Debug.LogWarning("PlayerMainCollider non assigné ou non trouvé. Impossible d'ajuster la taille du collider.");
        }
    }

    public void ChangePlayerScale(float targetUniformScale, float holdDuration)
    {
        if (scaleChangeCoroutine != null)
        {
            StopCoroutine(scaleChangeCoroutine);
        }

        if (targetUniformScale <= mutationSmallScale)
        {
            IsSmall = true;
            IsBig = false;
        }
        else if (targetUniformScale >= mutationNormalScale * 1.5f)
        {
            IsSmall = false;
            IsBig = true;
        }
        else
        {
            IsSmall = false;
            IsBig = false;
        }

        ApplyPlayerMutationSize(IsSmall);
        scaleChangeCoroutine = StartCoroutine(ScalePlayerOverTime(targetUniformScale, holdDuration));
    }

    private IEnumerator ScalePlayerOverTime(float targetUniformScale, float holdDuration)
    {
        Vector3 initialScale = transform.localScale;
        Vector3 finalScale = Vector3.one * targetUniformScale;
        float elapsed = 0f;

        while (elapsed < scaleTransitionDuration)
        {
            transform.localScale = Vector3.Lerp(initialScale, finalScale, elapsed / scaleTransitionDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = finalScale;

        if (holdDuration >= 0f)
        {
            yield return new WaitForSeconds(holdDuration);
            ChangePlayerScale(defaultScale, scaleTransitionDuration);
        }
        
        if (holdDuration >= 0f)
        {
            scaleChangeCoroutine = null;
        }
    }

    public void SetStickyState(bool state, float duration, float newStickyFallDelayMultiplier)
    {
        if (IsSticky == state) return;

        IsSticky = state;
        stickyFallDelayMultiplier = newStickyFallDelayMultiplier;

        if (IsSticky)
        {
            Debug.Log($"🟢 EFFET COLLANT ACTIVÉ ! Vitesse: {stickyJumpSpeedMultiplier}x, Durée: {stickyJumpDurationMultiplier}x");

            if (stickyEffectCoroutine != null)
            {
                StopCoroutine(stickyEffectCoroutine);
            }

            if (duration > 0)
            {
                stickyEffectCoroutine = StartCoroutine(StickyEffectTimer(duration));
            }
        }
        else
        {
            Debug.Log("🔴 EFFET COLLANT DÉSACTIVÉ");
            stickyFallDelayMultiplier = 1.0f;
            if (stickyEffectCoroutine != null)
            {
                StopCoroutine(stickyEffectCoroutine);
            }
            stickyEffectCoroutine = null;
        }
    }

    private IEnumerator StickyEffectTimer(float duration)
    {
        yield return new WaitForSeconds(duration);
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
        else if (collision.gameObject.CompareTag("PoisonPit") || collision.gameObject.CompareTag("ShrinkTile") || 
                 collision.gameObject.CompareTag("SlipperyTile"))
        {
            transform.position = lastSafePosition;
            Debug.Log("Le joueur a touché une tuile dangereuse et respawn à la dernière position sûre.");
        }
    }

    IEnumerator FallPlatform(GameObject platform)
    {
        if (!platform.activeSelf)
        {
            yield break;
        }

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

    // ========================================
    // ✨ MÉTHODE BONUS : Visualisation Gizmos améliorée avec les décors bloquants
    // Cette fonction dessine des repères visuels dans l'éditeur Unity pour faciliter
    // le débogage et la conception de niveau.
    // ========================================
    void OnDrawGizmos()
    {
        if (gridLayer.value == 0) return;

        // Dessine la grille de base en cyan
        if (gridPositionsToCubes != null)
        {
            Gizmos.color = Color.cyan;
            foreach (var entry in gridPositionsToCubes)
            {
                Gizmos.DrawWireCube(new Vector3(entry.Key.x, entry.Value.transform.position.y + 0.05f, entry.Key.z), 
                    new Vector3(cellSize, 0.1f, cellSize));
            }
        }

        // Dessine la position actuelle du joueur en jaune
        if (currentGridCube != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(new Vector3(currentGridCube.transform.position.x, currentGridCube.transform.position.y + 0.1f, 
                currentGridCube.transform.position.z), new Vector3(cellSize, 0.1f, cellSize));
        }

        // Dessine le chemin prévu en vert
        if (showPath && pathCalculated && path.Count > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < path.Count - 1; i++)
            {
                Gizmos.DrawLine(path[i] + Vector3.up * (0.1f + verticalOffsetOnGround), 
                    path[i + 1] + Vector3.up * (0.1f + verticalOffsetOnGround));
                Gizmos.DrawSphere(path[i] + Vector3.up * (0.1f + verticalOffsetOnGround), 0.05f);
            }
            Gizmos.DrawSphere(path[path.Count - 1] + Vector3.up * (0.1f + verticalOffsetOnGround), 0.05f);
        }

        // ✨ NOUVEAU : Visualisation des tuiles bloquées par des décors
        // Dessine des boîtes rouges semi-transparentes au-dessus des tuiles bloquées
        // Ceci permet de voir facilement quelles tuiles sont inaccessibles à cause de décors
        if (gridPositionsToCubes != null && Application.isEditor)
        {
            foreach (var entry in gridPositionsToCubes)
            {
                if (IsTileBlockedByDecor(entry.Value))
                {
                    // Boîte rouge transparente indiquant le blocage
                    Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
                    Vector3 boxCenter = entry.Value.transform.position + Vector3.up * (decorDetectionHeight * 0.5f);
                    Vector3 boxSize = new Vector3(cellSize * 0.8f, decorDetectionHeight, cellSize * 0.8f);
                    Gizmos.DrawCube(boxCenter, boxSize);
                    
                    // Dessine aussi un cadre rouge autour de la tuile au sol
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireCube(new Vector3(entry.Value.transform.position.x, 
                        entry.Value.transform.position.y + 0.05f, entry.Value.transform.position.z), 
                        new Vector3(cellSize, 0.1f, cellSize));
                }
            }
        }
    }

    public void AddGridCube(GameObject cube)
    {
        Vector3 cubePos = new Vector3(
            Mathf.Round(cube.transform.position.x * 1000) / 1000,
            cube.transform.position.y,
            Mathf.Round(cube.transform.position.z * 1000) / 1000);

        if (gridPositionsToCubes != null && !gridPositionsToCubes.ContainsKey(cubePos))
        {
            gridPositionsToCubes.Add(cubePos, cube);
        }
    }

    public void RemoveGridCube(GameObject cube)
    {
        Vector3 cubePos = new Vector3(
            Mathf.Round(cube.transform.position.x * 1000) / 1000,
            cube.transform.position.y,
            Mathf.Round(cube.transform.position.z * 1000) / 1000);

        if (gridPositionsToCubes != null && gridPositionsToCubes.ContainsKey(cubePos))
        {
            gridPositionsToCubes.Remove(cubePos);
        }
    }

    public void TeleportToPosition(Vector3 targetPosition)
    {
        isJumping = false;
        jumpTimer = 0f;
        pathCalculated = false;
        path.Clear();
        currentPathIndex = 0;
        
        if (lr != null)
        {
            lr.positionCount = 0;
        }
        
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        Vector3 teleportPosition = targetPosition;
        teleportPosition.y += verticalOffsetOnGround;
        transform.position = teleportPosition;
        
        GameObject newGridCube = FindNearestGridCube(transform.position);
        if (newGridCube != null)
        {
            previousGridCube = currentGridCube;
            currentGridCube = newGridCube;
            lastSafePosition = teleportPosition;
            Debug.Log($"Téléportation réussie vers {newGridCube.name}");
        }
        else
        {
            Debug.LogError("Aucun cube de grille trouvé près de la position de téléportation !");
        }
        
        ResetAllCellMaterials();
    }
}