using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Assurez-vous que cette ligne est présente

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
    [Tooltip("Nombre maximal de cases que le joueur peut parcourir en un seul clic.")] // NOUVEAU PARAMÈTRE
    public int maxPathLength = 3;

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
    [Tooltip("Le Material à appliquer au cube sélectionné (clic).")]
    public Material selectedCellMaterial;
    [Tooltip("Le Material à appliquer au cube survolé (hover).")]
    public Material hoveredCellMaterial;
    [Tooltip("Le Material à appliquer aux cases hors de portée du mouvement.")] // NOUVEAU MATERIAL
    public Material outOfRangeCellMaterial;

    private Material defaultCellMaterial;
    private GameObject lastSelectedCube;
    private GameObject lastHoveredCube;

    // --- Debug/Visualisation du Chemin ---
    [Header("Visualisation du Chemin")]
    [Tooltip("Indique si le chemin doit être affiché par le LineRenderer.")]
    public bool showPath = true;
    [Tooltip("Largeur de la ligne de rendu du chemin.")]
    public float lineWidth = 0.1f;

    // --- Effet de Vibration ---
    [Header("Effets de Feedback")]
    [Tooltip("Durée de la vibration de l'écran.")]
    public float shakeDuration = 0.1f; // Courte durée pour une vibration rapide
    [Tooltip("Intensité de la vibration de l'écran.")]
    public float shakeMagnitude = 0.1f; // Petite valeur pour une vibration subtile

    private Rigidbody rb;
    private LineRenderer lr;
    private List<Vector3> path = new List<Vector3>();
    private int currentPathIndex = 0;
    private Vector3 startJumpPosition;
    private Vector3 targetJumpPosition;
    private float jumpTimer = 0f;
    private bool isJumping = false;
    private bool pathCalculated = false;

    private GameObject currentGridCube;
    private Dictionary<Vector3, GameObject> gridPositionsToCubes;

    // Pour suivre l'état des matériaux des cellules quand on les survole/sélectionne
    private Dictionary<GameObject, Material> originalCellMaterials = new Dictionary<GameObject, Material>();

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

        // Vérification des materials
        if (selectedCellMaterial == null) Debug.LogWarning("Le Material de sélection n'est pas assigné !");
        if (hoveredCellMaterial == null) Debug.LogWarning("Le Material de survol n'est pas assigné !");
        if (outOfRangeCellMaterial == null) Debug.LogWarning("Le Material 'hors de portée' n'est pas assigné !");
    }

    void Update()
    {
        HandleHover();
        HandleInput();
        UpdatePathVisualization();
    }

    void FixedUpdate()
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
            ResetAllCellMaterials(); // Réinitialiser tous les matériaux une fois le chemin terminé
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
                    // Sauvegarde le matériau original de chaque cube lors de l'initialisation
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

    GameObject FindNearestGridCube(Vector3 position)
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
        // Réinitialise tous les matériaux pour les cases précédemment survolées/hors de portée
        ResetAllCellMaterials();

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        GameObject currentHoveredCube = null;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, gridLayer))
        {
            GameObject potentialHoveredCube = hit.collider.gameObject;

            // NOUVEAU: Vérifie la validité du chemin pour le survol
            List<GameObject> tempPath = CalculatePathForHover(currentGridCube, potentialHoveredCube);

            if (tempPath != null && tempPath.Count > 0 && tempPath.Count <= maxPathLength)
            {
                currentHoveredCube = potentialHoveredCube;
            }
            // MODIFICATION: Si la case est un obstacle ou hors de portée, change son material en 'outOfRangeCellMaterial'
            else if (tempPath == null || tempPath.Count > maxPathLength)
            {
                // Si c'est un obstacle, ou hors de portée, applique le material rouge transparent
                Renderer cubeRenderer = potentialHoveredCube.GetComponent<Renderer>();
                if (cubeRenderer != null && outOfRangeCellMaterial != null)
                {
                    // Sauvegarde l'original avant de changer
                    if (!originalCellMaterials.ContainsKey(potentialHoveredCube))
                    {
                        originalCellMaterials.Add(potentialHoveredCube, cubeRenderer.sharedMaterial);
                    }
                    cubeRenderer.material = outOfRangeCellMaterial;
                }
            }
        }

        // Applique le material de survol si la case est valide et survolée
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
            lastHoveredCube = null; // Aucune case valide survolée
        }

        // Réapplique le material de sélection si une case est déjà sélectionnée
        if (lastSelectedCube != null)
        {
            Renderer selectedRenderer = lastSelectedCube.GetComponent<Renderer>();
            if (selectedRenderer != null && selectedCellMaterial != null)
            {
                selectedRenderer.material = selectedCellMaterial;
            }
        }
    }


    void ResetAllCellMaterials()
    {
        foreach (var entry in originalCellMaterials)
        {
            if (entry.Key != null) // Assurez-vous que l'objet n'a pas été détruit
            {
                Renderer renderer = entry.Key.GetComponent<Renderer>();
                if (renderer != null && renderer.sharedMaterial != entry.Value) // Change seulement si le material n'est pas déjà l'original
                {
                    renderer.material = entry.Value;
                }
            }
        }
        originalCellMaterials.Clear(); // Videz le dictionnaire après réinitialisation
        
        // S'assurer que les cubes sélectionnés et survolés sont aussi réinitialisés
        if (lastSelectedCube != null)
        {
            Renderer selectedRenderer = lastSelectedCube.GetComponent<Renderer>();
            if (selectedRenderer != null && selectedRenderer.material != selectedCellMaterial) // Seulement si pas déjà sélectionné
            {
                if (originalCellMaterials.ContainsKey(lastSelectedCube)) // Réinitialise à l'original si dispo
                    selectedRenderer.material = originalCellMaterials[lastSelectedCube];
                else
                    selectedRenderer.material = defaultCellMaterial; // Sinon au défaut
            }
        }
        lastSelectedCube = null; // Réinitialise pour éviter des problèmes de référence
        lastHoveredCube = null;
    }


    void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Le raycast de clic doit aussi ignorer les obstacles pour la sélection valide
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, gridLayer))
            {
                GameObject targetCube = hit.collider.gameObject;

                // NOUVEAU: Vérifie si la cible est un obstacle ou hors de portée
                List<GameObject> tempPath = CalculatePathForHover(currentGridCube, targetCube);

                if (((1 << targetCube.layer) & obstacleLayer) != 0 || tempPath == null || tempPath.Count == 0 || tempPath.Count > maxPathLength)
                {
                    Debug.Log("Cible invalide (obstacle ou chemin trop long) !");
                    StartCoroutine(ShakeScreen()); // Faire vibrer l'écran
                    ResetAllCellMaterials(); // Réinitialiser visuel
                    return;
                }

                ResetAllCellMaterials(); // Réinitialise les matériaux des cellules hors portée avant de sélectionner
                UpdateSelectedCubeVisual(targetCube);
                CalculatePathForMovement(targetCube); // Utilise la nouvelle fonction de calcul de chemin
            }
        }
    }

    void UpdateSelectedCubeVisual(GameObject newSelectedCube)
    {
        // Avant de changer la sélection, réinitialise l'ancienne sélection à son matériau original
        if (lastSelectedCube != null && originalCellMaterials.ContainsKey(lastSelectedCube))
        {
            Renderer oldRenderer = lastSelectedCube.GetComponent<Renderer>();
            if (oldRenderer != null) oldRenderer.material = originalCellMaterials[lastSelectedCube];
        }

        if (newSelectedCube != null)
        {
            Renderer newCubeRenderer = newSelectedCube.GetComponent<Renderer>();
            if (newCubeRenderer != null && selectedCellMaterial != null)
            {
                // Sauvegarde le matériau original si ce n'est pas déjà fait
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
            lastSelectedCube = null; // Aucune sélection valide
        }
    }

    void ResetLastSelectedCubeMaterial()
    {
        // C'est maintenant géré par ResetAllCellMaterials et UpdateSelectedCubeVisual
    }

    // Ancienne fonction CalculatePath renommée et modifiée pour le mouvement
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

        // Utilise la nouvelle fonction de calcul de chemin pour obtenir le chemin final
        List<GameObject> calculatedGameObjectsPath = GetShortestPath(currentGridCube, targetCube);

        if (calculatedGameObjectsPath != null && calculatedGameObjectsPath.Count > 0 && calculatedGameObjectsPath.Count <= maxPathLength)
        {
            // Convertit le chemin de GameObjects en Vector3 pour le LineRenderer et le mouvement
            path = calculatedGameObjectsPath.Select(cube => cube.transform.position).ToList();
            pathCalculated = true;
        }
        else
        {
            Debug.LogWarning("Chemin invalide pour le mouvement (trop long ou obstacle non détecté plus tôt) !");
            pathCalculated = false;
            ResetAllCellMaterials();
            StartCoroutine(ShakeScreen()); // Faire vibrer l'écran si le chemin est quand même invalide à ce stade
        }
    }

    // Nouvelle fonction pour calculer le chemin (réutilisable pour le survol et le clic)
    List<GameObject> GetShortestPath(GameObject start, GameObject target)
    {
        if (start == null || target == null) return null;

        Queue<GameObject> queue = new Queue<GameObject>();
        Dictionary<GameObject, GameObject> cameFrom = new Dictionary<GameObject, GameObject>();
        Dictionary<GameObject, int> distance = new Dictionary<GameObject, int>(); // Pour la limite de pas
        HashSet<GameObject> visited = new HashSet<GameObject>();

        queue.Enqueue(start);
        visited.Add(start);
        cameFrom[start] = null;
        distance[start] = 0; // Distance du point de départ

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

            // Si la distance est déjà trop grande pour atteindre la cible, ne pas explorer plus loin
            if (distance[current] >= maxPathLength && target != current) // Permet d'atteindre la case 'maxPathLength' elle-même
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
                    distance[neighbor] = distance[current] + 1; // Incrémente la distance

                    // Si le voisin est la cible et sa distance est > maxPathLength, on ne le considère pas valide
                    if (neighbor == target && distance[neighbor] > maxPathLength)
                    {
                        foundPath = false; // Le chemin vers la cible est trop long
                        // Il faut vider la queue pour ne pas continuer à trouver ce chemin invalide
                        queue.Clear();
                        break; // Sortir de la boucle des voisins
                    }
                }
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

            // S'assurer que le premier point du chemin n'est pas le cube de départ.
            if (pathObjects.Count > 0 && pathObjects[0] == start)
            {
                pathObjects.RemoveAt(0);
            }
            return pathObjects;
        }
        return null; // Aucun chemin valide trouvé ou chemin trop long
    }

    // Nouvelle fonction pour calculer le chemin pour le survol (similaire mais ne modifie pas les variables de mouvement)
    List<GameObject> CalculatePathForHover(GameObject startCube, GameObject targetCube)
    {
        // Si la cible est un obstacle, retourne null
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
        // ResetAllCellMaterials(); // Géré à la fin du mouvement
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

            currentPathIndex++;

            if (currentPathIndex >= path.Count)
            {
                // ResetAllCellMaterials(); // Géré dans FixedUpdate quand pathCalculated devient false
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

    // Nouvelle coroutine pour faire vibrer l'écran
    System.Collections.IEnumerator ShakeScreen()
    {
        // Sauvegarder la position originale de la caméra
        Vector3 originalCameraPosition = Camera.main.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;

            Camera.main.transform.localPosition = originalCameraPosition + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null; // Attend la prochaine frame
        }

        // Réinitialiser la position de la caméra après la vibration
        Camera.main.transform.localPosition = originalCameraPosition;
    }

    // Ancienne fonction GetCellCenter pas nécessaire telle quelle
    // void GetCellCenter(Vector3 worldPosition) { ... }

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

        if (showPath && path.Count > 0)
        {
            Gizmos.color = Color.blue;
            if (currentGridCube != null)
            {
                Gizmos.DrawLine(currentGridCube.transform.position + Vector3.up * (0.1f + verticalOffsetOnGround), path[0] + Vector3.up * (0.1f + verticalOffsetOnGround));
            }

            for (int i = 0; i < path.Count; i++)
            {
                Gizmos.DrawWireCube(path[i] + Vector3.up * (0.1f + verticalOffsetOnGround), new Vector3(cellSize, 0.1f, cellSize));
                if (i > 0)
                {
                    Gizmos.DrawLine(path[i - 1] + Vector3.up * (0.1f + verticalOffsetOnGround), path[i] + Vector3.up * (0.1f + verticalOffsetOnGround));
                }
            }
        }

        Gizmos.color = Color.green; // Connexions entre voisins
        if (currentGridCube != null)
        {
            foreach (GameObject neighbor in GetNeighbors(currentGridCube))
            {
                Gizmos.DrawLine(currentGridCube.transform.position, neighbor.transform.position);
            }
        }

        // Ces Gizmos seront moins pertinents avec le nouveau système de materials, mais peuvent rester pour le debug si besoin
        /*
        if (lastSelectedCube != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(new Vector3(lastSelectedCube.transform.position.x, lastSelectedCube.transform.position.y + 0.15f, lastSelectedCube.transform.position.z), new Vector3(cellSize * 1.1f, 0.2f, cellSize * 1.1f));
        }

        if (lastHoveredCube != null && lastHoveredCube != lastSelectedCube)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(new Vector3(lastHoveredCube.transform.position.x, lastHoveredCube.transform.position.y + 0.12f, lastHoveredCube.transform.position.z), new Vector3(cellSize * 1.05f, 0.15f, cellSize * 1.05f));
        }
        */
    }
}