using UnityEngine;
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

    // --- Références et Grille ---
    [Header("Références Grille")]
    [Tooltip("Le LayerMask des objets de la grille (cubes).")]
    public LayerMask gridLayer;
    [Tooltip("La taille d'une case (longueur d'un côté du cube, utilisé pour Gizmos et références visuelles).")]
    public float cellSize = 1f;
    [Tooltip("La distance maximale entre les centres de deux cubes pour qu'ils soient considérés comme des voisins (un saut possible).")]
    public float maxJumpDistance = 2f;

    // --- Visualisation de la Sélection ---
    [Header("Visualisation de la Sélection")]
    [Tooltip("Le Material à appliquer au cube sélectionné (clic).")]
    public Material selectedCellMaterial;
    [Tooltip("Le Material à appliquer au cube survolé (hover).")] // NOUVEAU
    public Material hoveredCellMaterial; // NOUVEAU

    private Material defaultCellMaterial;
    private GameObject lastSelectedCube;
    private GameObject lastHoveredCube; // NOUVEAU

    // --- Debug/Visualisation du Chemin ---
    [Header("Visualisation du Chemin")]
    [Tooltip("Indique si le chemin doit être affiché par le LineRenderer.")]
    public bool showPath = true;
    [Tooltip("Largeur de la ligne de rendu du chemin.")]
    public float lineWidth = 0.1f;

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
            transform.position = currentGridCube.transform.position;
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
        if (selectedCellMaterial == null)
        {
            Debug.LogWarning("Le Material de sélection n'est pas assigné dans l'Inspecteur !");
        }
        if (hoveredCellMaterial == null) // NOUVEAU
        {
            Debug.LogWarning("Le Material de survol n'est pas assigné dans l'Inspecteur !");
        }
    }

    void Update()
    {
        HandleHover(); // NOUVEAU : Gère le survol avant le clic
        HandleInput(); // Gère le clic
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
            rb.velocity = Vector3.zero;
            transform.position = currentGridCube.transform.position;
            ResetLastSelectedCubeMaterial();
        }
    }

    void InitializeGridCubes()
    {
        gridPositionsToCubes = new Dictionary<Vector3, GameObject>();
        Collider[] gridColliders = Physics.OverlapSphere(Vector3.zero, 500f, gridLayer);

        foreach (Collider col in gridColliders)
        {
            Vector3 cubePos = SnapToNearestGridPosition(col.transform.position);
            if (!gridPositionsToCubes.ContainsKey(cubePos))
            {
                gridPositionsToCubes.Add(cubePos, col.gameObject);
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
            float dist = Vector3.Distance(position, entry.Key);
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
        return new Vector3(Mathf.Round(pos.x * 1000) / 1000, 0, Mathf.Round(pos.z * 1000) / 1000);
    }

    // NOUVEAU : Gère la détection de survol de la souris
    void HandleHover()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        GameObject currentHoveredCube = null;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, gridLayer))
        {
            currentHoveredCube = hit.collider.gameObject;
        }

        // Si le cube survolé est différent du précédent
        if (currentHoveredCube != lastHoveredCube)
        {
            // Réinitialiser le material du cube précédemment survolé
            ResetLastHoveredCubeMaterial();

            // Si un nouveau cube est survolé et n'est PAS le cube actuellement sélectionné
            if (currentHoveredCube != null && currentHoveredCube != lastSelectedCube)
            {
                Renderer cubeRenderer = currentHoveredCube.GetComponent<Renderer>();
                if (cubeRenderer != null && hoveredCellMaterial != null)
                {
                    cubeRenderer.material = hoveredCellMaterial;
                    lastHoveredCube = currentHoveredCube;
                }
            }
        }
    }

    // NOUVEAU : Réinitialise le material du cube survolé
    void ResetLastHoveredCubeMaterial()
    {
        // Ne réinitialise que si ce n'est pas le cube actuellement sélectionné (cliqué)
        if (lastHoveredCube != null && lastHoveredCube != lastSelectedCube && defaultCellMaterial != null)
        {
            Renderer cubeRenderer = lastHoveredCube.GetComponent<Renderer>();
            if (cubeRenderer != null)
            {
                cubeRenderer.material = defaultCellMaterial;
            }
        }
        lastHoveredCube = null; // Il n'y a plus de case survolée activement
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
                
                // Mettre à jour la visualisation de la sélection
                // NOTE: On désélectionne le survol ici, car le clic prend le dessus
                ResetLastHoveredCubeMaterial();
                UpdateSelectedCubeVisual(targetCube); 

                CalculatePath(targetCube);
            }
        }
    }

    void UpdateSelectedCubeVisual(GameObject newSelectedCube)
    {
        ResetLastSelectedCubeMaterial();

        if (newSelectedCube != null && newSelectedCube != lastSelectedCube)
        {
            Renderer newCubeRenderer = newSelectedCube.GetComponent<Renderer>();
            if (newCubeRenderer != null && selectedCellMaterial != null)
            {
                if (defaultCellMaterial == null)
                {
                    defaultCellMaterial = newCubeRenderer.sharedMaterial;
                }
                newCubeRenderer.material = selectedCellMaterial;
                lastSelectedCube = newSelectedCube;
            }
        }
    }

    void ResetLastSelectedCubeMaterial()
    {
        if (lastSelectedCube != null && defaultCellMaterial != null)
        {
            Renderer lastCubeRenderer = lastSelectedCube.GetComponent<Renderer>();
            if (lastCubeRenderer != null)
            {
                lastCubeRenderer.material = defaultCellMaterial;
            }
        }
        lastSelectedCube = null;
    }

    void CalculatePath(GameObject targetCube)
    {
        if (isJumping || pathCalculated || targetCube == null || currentGridCube == null) return;

        path.Clear();
        currentPathIndex = 0;

        if (targetCube == currentGridCube)
        {
            ResetLastSelectedCubeMaterial();
            return;
        }

        Queue<GameObject> queue = new Queue<GameObject>();
        Dictionary<GameObject, GameObject> cameFrom = new Dictionary<GameObject, GameObject>();
        HashSet<GameObject> visited = new HashSet<GameObject>();

        queue.Enqueue(currentGridCube);
        visited.Add(currentGridCube);
        cameFrom[currentGridCube] = null;

        GameObject current = null;
        bool foundPath = false;

        while (queue.Count > 0)
        {
            current = queue.Dequeue();

            if (current == targetCube)
            {
                foundPath = true;
                break;
            }

            foreach (GameObject neighbor in GetNeighbors(current))
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                    cameFrom[neighbor] = current;
                }
            }
        }

        if (foundPath)
        {
            List<Vector3> tempPath = new List<Vector3>();
            current = targetCube;
            while (current != null)
            {
                tempPath.Add(new Vector3(current.transform.position.x, 0, current.transform.position.z));
                current = cameFrom[current];
            }
            tempPath.Reverse();

            if (tempPath.Count > 0 && Vector3.Distance(tempPath[0], new Vector3(currentGridCube.transform.position.x, 0, currentGridCube.transform.position.z)) < 0.1f)
            {
                 path = tempPath.Skip(1).ToList();
            }
            else
            {
                path = tempPath;
            }

            pathCalculated = true;
        }
        else
        {
            Debug.LogWarning("Aucun chemin trouvé vers la cible !");
            pathCalculated = false;
            ResetLastSelectedCubeMaterial();
            ResetLastHoveredCubeMaterial(); // NOUVEAU: Réinitialise aussi le survol si pas de chemin
        }
    }

    List<GameObject> GetNeighbors(GameObject cube)
    {
        List<GameObject> neighbors = new List<GameObject>();
        Vector3 cubePos = cube.transform.position;

        foreach (var entry in gridPositionsToCubes)
        {
            GameObject potentialNeighbor = entry.Value;
            if (potentialNeighbor == cube) continue;

            float distance = Vector2.Distance(new Vector2(cubePos.x, cubePos.z), new Vector2(potentialNeighbor.transform.position.x, potentialNeighbor.transform.position.z));

            if (distance <= maxJumpDistance)
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
        // S'assurer que le cube sélectionné n'est pas le cube survolé quand le saut commence
        ResetLastHoveredCubeMaterial();
    }

    void PerformJump()
    {
        jumpTimer += Time.fixedDeltaTime;
        float progress = jumpTimer / jumpDuration;

        if (progress >= 1f)
        {
            transform.position = targetJumpPosition;
            rb.velocity = Vector3.zero;
            isJumping = false;

            currentGridCube = FindNearestGridCube(transform.position);
            if (currentGridCube == null) Debug.LogError("Le joueur a atterri hors grille !");

            currentPathIndex++;

            if (currentPathIndex >= path.Count)
            {
                ResetLastSelectedCubeMaterial();
                // Assurez-vous que le survol est également réinitialisé après le chemin complet
                ResetLastHoveredCubeMaterial();
            }
        }
        else
        {
            Vector3 currentPosHorizontal = Vector3.Lerp(startJumpPosition, targetJumpPosition, progress);
            float yOffset = jumpHeight * (4f * progress * (1f - progress));
            rb.MovePosition(new Vector3(currentPosHorizontal.x, startJumpPosition.y + yOffset, currentPosHorizontal.z));
        }
    }

    void UpdatePathVisualization()
    {
        if (showPath && pathCalculated && path.Count > 0)
        {
            lr.positionCount = path.Count + 1;
            lr.SetPosition(0, currentGridCube.transform.position + Vector3.up * 0.1f);
            for (int i = 0; i < path.Count; i++)
            {
                lr.SetPosition(i + 1, path[i] + Vector3.up * 0.1f);
            }
        }
        else
        {
            lr.positionCount = 0;
        }
    }

    Vector3 GetCellCenter(Vector3 worldPosition)
    {
        return new Vector3(worldPosition.x, 0, worldPosition.z);
    }

    void OnDrawGizmos()
    {
        if (gridLayer.value == 0) return;

        if (gridPositionsToCubes != null)
        {
            Gizmos.color = Color.cyan;
            foreach (var entry in gridPositionsToCubes)
            {
                Gizmos.DrawWireCube(new Vector3(entry.Key.x, entry.Key.y + 0.05f, entry.Key.z), new Vector3(cellSize, 0.1f, cellSize));
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
                Gizmos.DrawLine(currentGridCube.transform.position, path[0]);
            }

            for (int i = 0; i < path.Count; i++)
            {
                Gizmos.DrawWireCube(new Vector3(path[i].x, path[i].y + 0.1f, path[i].z), new Vector3(cellSize, 0.1f, cellSize));
                if (i > 0)
                {
                    Gizmos.DrawLine(path[i - 1], path[i]);
                }
            }
        }

        Gizmos.color = Color.green;
        if (currentGridCube != null)
        {
            foreach(GameObject neighbor in GetNeighbors(currentGridCube))
            {
                Gizmos.DrawLine(currentGridCube.transform.position, neighbor.transform.position);
            }
        }

        if (lastSelectedCube != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(new Vector3(lastSelectedCube.transform.position.x, lastSelectedCube.transform.position.y + 0.15f, lastSelectedCube.transform.position.z), new Vector3(cellSize * 1.1f, 0.2f, cellSize * 1.1f));
        }

        // NOUVEAU GIZMO pour le cube survolé
        if (lastHoveredCube != null && lastHoveredCube != lastSelectedCube) // Ne pas dessiner si déjà sélectionné
        {
            Gizmos.color = Color.magenta; // Couleur distincte pour le survol
            Gizmos.DrawWireCube(new Vector3(lastHoveredCube.transform.position.x, lastHoveredCube.transform.position.y + 0.12f, lastHoveredCube.transform.position.z), new Vector3(cellSize * 1.05f, 0.15f, cellSize * 1.05f));
        }
    }
}