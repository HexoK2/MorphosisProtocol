using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(LineRenderer))]
public class PlayerMovement : MonoBehaviour
{
    // --- Paramètres de Mouvement ---
    [Header("Paramètres de Mouvement")]
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
    
    // --- Paramètres de Mutation (Petit/Grand) ---
    [Header("Mutation du joueur")]
    [Tooltip("Indique si le joueur est actuellement dans sa forme 'petite'.")]
    public bool IsSmall = false;
    [Tooltip("Indique si le joueur est actuellement dans sa forme 'grande'.")]
    public bool IsBig = false; 
    [Tooltip("La taille (scale uniforme) du joueur quand il est dans sa forme 'petite'.")]
    public float mutationSmallScale = 0.5f; 
    [Tooltip("La taille (scale uniforme) du joueur quand il est dans sa forme 'normale'.")]
    public float mutationNormalScale = 1.0f; 
    [Tooltip("La taille du collider du joueur quand il est dans sa forme 'grande'.")]
    public float mutationBigScale = 1.5f; 
    [Tooltip("Le collider principal du joueur (CharacterController ou CapsuleCollider).")]
    public Collider playerMainCollider; 

    private Coroutine scaleChangeCoroutine; 


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

        gridPositionsToCubes = new Dictionary<Vector3, GameObject>();
        RefreshGrid(); 

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

            ApplyPlayerMutationSize(); 

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

    // Méthode publique pour rafraîchir la grille depuis d'autres scripts
    public void RefreshGrid()
    {
        InitializeGridCubes();
    }

    // L'ancienne méthode InitializeGridCubes, maintenant privée, appelée par RefreshGrid
    private void InitializeGridCubes()
    {
        gridPositionsToCubes.Clear(); // Nettoyer l'ancienne grille avant de la reconstruire
        
        Collider[] gridColliders = Physics.OverlapSphere(Vector3.zero, 500f, gridLayer);

        foreach (Collider col in gridColliders)
        {
            // On vérifie que le collider appartient à une tuile active
            if (col.gameObject.activeInHierarchy && ((1 << col.gameObject.layer) & obstacleLayer) == 0)
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

            List<GameObject> tempPath = CalculatePathForHover(currentGridCube, potentialHoveredCube);

            if (tempPath != null && tempPath.Count > 0 && tempPath.Count <= maxPathLength)
            {
                currentHoveredCube = potentialHoveredCube;
            }
            else
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
                
                List<GameObject> tempPath = GetShortestPath(currentGridCube, targetCube);

                bool isTargetSpecialTile = targetCube.CompareTag("PoisonPit") || targetCube.CompareTag("ShrinkTile") || targetCube.CompareTag("ReactiveTile"); 
                
                if (tempPath == null || tempPath.Count == 0 || (tempPath.Count > maxPathLength && !isTargetSpecialTile))
                {
                    Debug.Log("Cible invalide (obstacle, chemin trop long, ou inaccessible) !");
                    StartCoroutine(ShakeScreen());
                    ResetAllCellMaterials();
                    return;
                }
                
                if (isTargetSpecialTile)
                {
                    Debug.Log("Attention: Vous vous déplacez vers une tuile spéciale !");
                    StartCoroutine(ShakeScreen());
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
            if (newSelectedCube.CompareTag("PoisonPit") || newSelectedCube.CompareTag("ShrinkTile") || ((1 << newSelectedCube.layer) & obstacleLayer) != 0) 
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

            if (distance[current] >= maxPathLength && !current.CompareTag("PoisonPit") && !current.CompareTag("ShrinkTile") && ((1 << current.layer) & obstacleLayer) == 0)
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
                }
            }
            
            // Si on ne trouve pas le chemin mais que la queue est vide et la cible pas atteinte, on sort
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

            if (pathObjects.Count > maxPathLength && !target.CompareTag("PoisonPit") && !target.CompareTag("ShrinkTile")) 
            {
                return null;
            }

            return pathObjects;
        }
        return null;
    }

    List<GameObject> CalculatePathForHover(GameObject startCube, GameObject targetCube)
    {
        // Une tuile spéciale ne devrait pas être affichée comme une cible valide pour le survol
        if (targetCube.CompareTag("PoisonPit") || targetCube.CompareTag("ShrinkTile")) return null; 

        // Si la tuile est inactive (cachée), on ne l'affiche pas
        if (!targetCube.activeInHierarchy) return null;
        
        // Si c'est un obstacle (et pas un mur de mutation actif), on l'ignore
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

            // Si le voisin est sur une couche d'obstacle, on ne l'ajoute pas.
            // Grâce au BigMutationTileActivator, les tuiles de mutation sont déjà actives/inactives.
            // On a pas besoin de logique de tag ici.
            if (((1 << potentialNeighbor.layer) & obstacleLayer) != 0)
            {
                continue;
            }
            
            // Si le voisin est actif et qu'il n'est pas un obstacle, c'est un voisin valide.
            if (potentialNeighbor.activeInHierarchy)
            {
                neighbors.Add(potentialNeighbor);
            }
        }
        return neighbors;
    }


    void StartNextJump()
    {
        isJumping = true;
        jumpTimer = 0f;
        startJumpPosition = transform.position;
        targetJumpPosition = path[currentPathIndex];
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

            currentGridCube = FindNearestGridCube(transform.position);
            if (currentGridCube == null) Debug.LogError("Le joueur a atterri hors grille !");

            if (currentGridCube != null && (currentGridCube.CompareTag("PoisonPit") || currentGridCube.CompareTag("ShrinkTile"))) 
            {
                Debug.Log("Player landed on a special tile (transit). Moving to next path segment immediately.");
                currentPathIndex++;
            }
            else
            {
                currentPathIndex++;
                lastSafePosition = transform.position;
                Debug.Log($"Last Safe Position updated to: {lastSafePosition}");

                if (currentGridCube != null && currentGridCube.CompareTag("ReactiveTile")) 
                {
                    Debug.Log("Player landed on a ReactiveTile. Increasing player size.");
                    ChangePlayerScale(boostedScale, defaultBoostedDuration);
                }
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
        }
    }


    public void PickUpTorch()
    {
        hasTorch = true;
        Debug.Log("Torche ramassée !");
    }

    public void ToggleMutation()
    {
        if (IsBig)
        {
            IsBig = false;
            IsSmall = false; 
        }
        else if (IsSmall)
        {
            IsSmall = false;
            IsBig = true;
        }
        else
        {
            IsBig = false;
            IsSmall = true;
        }
        
        ApplyPlayerMutationSize(); 
        RefreshGrid(); // Important : rafraîchir la grille après avoir changé la taille du joueur.
    }

    private void ApplyPlayerMutationSize()
    {
        float targetScale = mutationNormalScale;
        if (IsSmall)
        {
            targetScale = mutationSmallScale;
        }
        else if (IsBig)
        {
            targetScale = mutationBigScale;
        }

        Vector3 finalGlobalScale = Vector3.one * targetScale;
        transform.localScale = finalGlobalScale;

        if (playerMainCollider != null)
        {
            // Ajustement de la taille du collider en fonction de la taille
            if (playerMainCollider is CharacterController characterController)
            {
                characterController.height = IsSmall ? 1.0f : IsBig ? 3.0f : 2.0f; 
                characterController.radius = IsSmall ? 0.25f : IsBig ? 0.75f : 0.5f; 
                characterController.center = new Vector3(0, characterController.height / 2f, 0);
            }
            else if (playerMainCollider is CapsuleCollider capsuleCollider)
            {
                capsuleCollider.height = IsSmall ? 1.0f : IsBig ? 3.0f : 2.0f;
                capsuleCollider.radius = IsSmall ? 0.25f : IsBig ? 0.75f : 0.5f;
                capsuleCollider.center = new Vector3(0, capsuleCollider.height / 2f, 0);
            }
            else if (playerMainCollider is BoxCollider boxCollider)
            {
                boxCollider.size = IsSmall ? new Vector3(0.5f, 1.0f, 0.5f) : IsBig ? new Vector3(1.5f, 3.0f, 1.5f) : new Vector3(1.0f, 2.0f, 1.0f);
                boxCollider.center = new Vector3(0, boxCollider.size.y / 2f, 0);
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
            if (transform.position.y > collision.gameObject.transform.position.y - 0.1f)
            {
                StartCoroutine(FallPlatform(collision.gameObject));
            }
        }
    }

    IEnumerator FallPlatform(GameObject platform)
    {
        if (!platform.activeSelf)
        {
            yield break;
        }

        yield return new WaitForSeconds(fallDelay);

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

    System.Collections.IEnumerator ShakeScreen()
    {
        Vector3 originalCameraLocalPosition = Camera.main.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;

            Camera.main.transform.localPosition = originalCameraLocalPosition + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        Camera.main.transform.localPosition = originalCameraLocalPosition;
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
            Gizmos.color = Color.green;
            for (int i = 0; i < path.Count - 1; i++)
            {
                Gizmos.DrawLine(path[i] + Vector3.up * (0.1f + verticalOffsetOnGround), path[i+1] + Vector3.up * (0.1f + verticalOffsetOnGround));
                Gizmos.DrawSphere(path[i] + Vector3.up * (0.1f + verticalOffsetOnGround), 0.05f);
            }
            Gizmos.DrawSphere(path[path.Count - 1] + Vector3.up * (0.1f + verticalOffsetOnGround), 0.05f);
        }
    }
}