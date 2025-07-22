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
    [Tooltip("Durée de la transition de taille (agrandissement ou réduction).")]
    public float scaleTransitionDuration = 0.3f;
    [Tooltip("Durée par défaut pendant laquelle le joueur reste à taille augmentée (pour les tuiles réactives).")]
    public float defaultBoostedDuration = 5.0f; 
    private Coroutine scaleChangeCoroutine; // Pour gérer la coroutine de changement de taille

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
    [Header("Visualisation du Chemin")]
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

            // Initialiser la taille par défaut du joueur
            transform.localScale = Vector3.one * defaultScale;

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

            if (potentialHoveredCube.CompareTag("PoisonPit"))
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

                List<GameObject> tempPath = GetShortestPath(currentGridCube, targetCube);

                bool isTargetPoisonPit = targetCube.CompareTag("PoisonPit");

                if (((1 << targetCube.layer) & obstacleLayer) != 0 || tempPath == null || tempPath.Count == 0 || (tempPath.Count > maxPathLength && !isTargetPoisonPit))
                {
                    Debug.Log("Cible invalide (obstacle, chemin trop long, ou inaccessible) !");
                    StartCoroutine(ShakeScreen());
                    ResetAllCellMaterials();
                    return;
                }
                
                if (isTargetPoisonPit)
                {
                    Debug.Log("Attention: Vous vous déplacez vers un PoisonPit !");
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
            if (newSelectedCube.CompareTag("PoisonPit"))
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
                // Si la cible est un PoisonPit, le chemin peut être plus long que maxPathLength
                // Nous permettons d'atteindre un PoisonPit même s'il est au-delà du maxPathLength "normal".
                // Cependant, si la distance est trop grande *et* ce n'est PAS un PoisonPit, alors ce n'est pas un chemin valide.
                // La logique précédente qui mettait foundPath à false si PoisonPit et distance > maxPathLength était incorrecte pour l'intention de l'utilisateur.
                foundPath = true; // Si on arrive à la cible, le chemin est trouvé. La longueur est vérifiée après.
                break;
            }

            // Si la distance actuelle est déjà maxPathLength et que la cible n'est PAS un PoisonPit
            // alors nous ne devrions pas explorer davantage depuis ce nœud pour les chemins "normaux".
            if (distance[current] >= maxPathLength && !target.CompareTag("PoisonPit"))
            {
                continue;
            }

            foreach (GameObject neighbor in GetNeighbors(current))
            {
                if (((1 << neighbor.layer) & obstacleLayer) != 0) continue;

                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                    cameFrom[neighbor] = current;
                    distance[neighbor] = distance[current] + 1;

                    // Condition pour les tuiles non-PoisonPit : si le chemin est trop long, on ne le prend pas
                    // Si on atteint la cible et que la distance dépasse maxPathLength *ET* ce n'est pas un PoisonPit
                    if (neighbor == target && distance[neighbor] > maxPathLength && !neighbor.CompareTag("PoisonPit"))
                    {
                        foundPath = false; // Ce chemin vers cette cible est invalide car trop long
                        queue.Clear(); // Vider la queue pour arrêter la recherche
                        break; // Sortir de la boucle des voisins
                    }
                }
            }
            // Si la queue est vide et on n'a pas atteint la cible, le chemin n'a pas été trouvé.
            // La condition précédente pour PoisonPit ici était redondante ou mal placée.
            if (queue.Count == 0 && current != target)
            {
                foundPath = false; // Pas de chemin valide trouvé.
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

            // Dernière vérification de la longueur du chemin pour les non-PoisonPits
            // Si le chemin est trop long ET que la cible n'est PAS un PoisonPit, alors le chemin est invalide.
            if (pathObjects.Count > maxPathLength && !target.CompareTag("PoisonPit"))
            {
                return null;
            }

            return pathObjects;
        }
        return null;
    }

    List<GameObject> CalculatePathForHover(GameObject startCube, GameObject targetCube)
    {
        // Un PoisonPit ne devrait pas être affiché comme une cible valide pour le survol
        if (targetCube.CompareTag("PoisonPit")) return null;

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

            if (((1 << potentialNeighbor.layer) & obstacleLayer) != 0) continue;

            float horizontalDistance = Vector2.Distance(new Vector2(cubePos.x, cubePos.z), new Vector2(potentialNeighbor.transform.position.x, potentialNeighbor.transform.position.z));
            float verticalDifference = Mathf.Abs(cubePos.y - potentialNeighbor.transform.position.y);

            if (horizontalDistance <= maxJumpDistance && verticalDifference <= maxVerticalJumpDifference)
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

            // Si la case sur laquelle on vient d'atterrir est un PoisonPit,
            // alors on ne met PAS à jour lastSafePosition et on avance DÉJÀ
            // à l'index suivant du chemin pour déclencher le prochain saut immédiatement.
            // Le grossissement est géré par le PoisonPit lui-même via OnTriggerEnter.
            if (currentGridCube != null && currentGridCube.CompareTag("PoisonPit"))
            {
                Debug.Log("Player landed on a PoisonPit (transit). Moving to next path segment immediately.");
                currentPathIndex++;
            }
            else // Sinon, si c'est une case normale (non-poison) ou un réactif
            {
                currentPathIndex++;
                lastSafePosition = transform.position; // Mettre à jour la lastSafePosition
                Debug.Log($"Last Safe Position updated to: {lastSafePosition}");

                // Si la case sur laquelle le joueur atterrit a le tag "ReactiveTile"
                if (currentGridCube != null && currentGridCube.CompareTag("ReactiveTile")) // Assurez-vous d'avoir ce tag sur vos tuiles vertes
                {
                    Debug.Log("Player landed on a ReactiveTile. Increasing player size.");
                    // Appel de la méthode de changement de taille avec la durée par défaut pour les réactifs
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

    // --- NOUVELLES/MODIFIÉES MÉTHODES POUR LA TAILLE DE LA BOULE ---

    // Méthode publique pour changer la taille, maintenant avec une durée paramétrable.
    // Une 'duration' de -1f signifie "indéfiniment".
    public void ChangePlayerScale(float targetScale, float duration)
    {
        // Arrête toute coroutine de changement de taille en cours
        if (scaleChangeCoroutine != null)
        {
            StopCoroutine(scaleChangeCoroutine);
        }
        // Démarre la nouvelle coroutine de changement de taille avec la durée spécifiée
        scaleChangeCoroutine = StartCoroutine(ScalePlayerOverTime(targetScale, duration));
    }

    // Coroutine modifiée pour inclure une durée de maintien paramétrable et la gestion de "indéfini"
    private IEnumerator ScalePlayerOverTime(float targetScale, float holdDuration)
    {
        Vector3 initialScale = transform.localScale;
        Vector3 finalScale = Vector3.one * targetScale;
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
        // de la coroutine pour revenir à la taille normale. Si on voulait l'arrêter manuellement plus tard,
        // la référence existerait toujours. Pour des effets permanents, c'est généralement OK.
        // Si vous avez besoin de forcer un retour à la normale pour un effet permanent (par exemple, si le joueur
        // touche un autre objet qui annule l'effet), vous devrez appeler ChangePlayerScale(defaultScale, ...) manuellement.
        if (holdDuration >= 0f) // Seulement si l'effet n'est PAS permanent
        {
            scaleChangeCoroutine = null; // La coroutine est terminée
        }
    }

    // --- FIN NOUVELLES MÉTHODES POUR LA TAILLE DE LA BOULE ---

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