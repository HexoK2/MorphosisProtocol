using UnityEngine;
using System.Collections; // Nécessaire pour les Coroutines
using System.Collections.Generic; // Nécessaire pour les Listes

[RequireComponent(typeof(Rigidbody))] // S'assure qu'un Rigidbody est présent sur l'objet
public class PlayerMovement : MonoBehaviour
{
    // Paramètres de mouvement et de saut
    public float moveSpeed = 5f;
    public float jumpHeight = 2f;
    public float jumpDuration = 0.5f;
    public float stopThreshold = 0.05f;

    // Pour l'indicateur de survol de la case
    public Material highlightMaterial;
    public float highlightWidth = 0.1f;
    private LineRenderer tileLineRenderer;

    // --- NOUVEAUX CHAMPS POUR LE PATHFINDING ---
    public Pathfinding pathfinding; // Référence à ton script Pathfinding
    private List<Node> currentPath; // Le chemin actuel que la boule doit suivre
    private int currentWaypointIndex; // L'index du waypoint actuel dans le chemin
    private Coroutine followPathCoroutine; // Pour pouvoir arrêter le mouvement si un nouveau chemin est demandé

    // Variables d'état interne
    private Vector3 targetPosition; // La position du waypoint actuel vers laquelle le joueur se dirige
    private bool isMoving = false;
    private bool isJumping = false;
    private Vector3 startJumpPosition;
    private float jumpTimer = 0f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody is missing on PlayerMovement object! Please add a Rigidbody component.");
        }

        // Initialisation du LineRenderer (pour le highlight)
        tileLineRenderer = gameObject.AddComponent<LineRenderer>();
        tileLineRenderer.positionCount = 0;
        tileLineRenderer.startWidth = highlightWidth;
        tileLineRenderer.endWidth = highlightWidth;
        tileLineRenderer.material = highlightMaterial;
        tileLineRenderer.useWorldSpace = true;
        tileLineRenderer.enabled = false;

        // --- Récupération de la référence au Pathfinding (NOUVEAU) ---
        if (pathfinding == null)
        {
            pathfinding = FindObjectOfType<Pathfinding>();
            if (pathfinding == null)
            {
                Debug.LogError("Pathfinding script not found in scene!");
            }
        }
    }

    void Start()
    {
        targetPosition = transform.position;
    }

    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // --- Détection du survol de la souris pour l'indicateur ---
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.CompareTag("GroundTile"))
            {
                if (tileLineRenderer != null)
                {
                    Bounds bounds = hit.collider.bounds;
                    float yOffset = 0.02f;

                    Vector3 p1 = new Vector3(bounds.min.x, bounds.max.y + yOffset, bounds.min.z);
                    Vector3 p2 = new Vector3(bounds.max.x, bounds.max.y + yOffset, bounds.min.z);
                    Vector3 p3 = new Vector3(bounds.max.x, bounds.max.y + yOffset, bounds.max.z);
                    Vector3 p4 = new Vector3(bounds.min.x, bounds.max.y + yOffset, bounds.max.z);

                    tileLineRenderer.positionCount = 5;
                    tileLineRenderer.SetPosition(0, p1);
                    tileLineRenderer.SetPosition(1, p2);
                    tileLineRenderer.SetPosition(2, p3);
                    tileLineRenderer.SetPosition(3, p4);
                    tileLineRenderer.SetPosition(4, p1);

                    tileLineRenderer.enabled = true;
                }
            }
            else
            {
                if (tileLineRenderer != null)
                {
                    tileLineRenderer.enabled = false;
                }
            }
        }
        else
        {
            if (tileLineRenderer != null)
            {
                tileLineRenderer.enabled = false;
            }
        }

        // --- Gérer l'entrée de la souris pour définir la nouvelle position cible (clic) ---
        if (Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.CompareTag("GroundTile"))
                {
                    Vector3 tileCenter = hit.collider.bounds.center;
                    // La destination finale est le centre de la case cliquée
                    Vector3 finalDestination = new Vector3(tileCenter.x, hit.point.y, tileCenter.z);

                    // --- DEMANDE DE CHEMIN AU PATHFINDING (NOUVEAU) ---
                    if (pathfinding != null)
                    {
                        // Si une coroutine de suivi de chemin est déjà en cours, on l'arrête
                        if (followPathCoroutine != null)
                        {
                            StopCoroutine(followPathCoroutine);
                        }

                        // On demande un nouveau chemin au Pathfinding
                        currentPath = pathfinding.FindPath(rb.position, finalDestination);

                        if (currentPath != null && currentPath.Count > 0)
                        {
                            currentWaypointIndex = 0; // Réinitialise l'index du waypoint
                            isMoving = true; // Démarre le mouvement
                            isJumping = false; // Réinitialise l'état de saut
                            // Démarre la coroutine qui va faire suivre le chemin à la boule
                            followPathCoroutine = StartCoroutine(FollowPath());
                        }
                        else
                        {
                            // Si aucun chemin n'a été trouvé (ex: cible inaccessible)
                            isMoving = false;
                            Debug.Log("No path found to destination!");
                        }
                    }
                }
            }
        }
    }

    // --- Coroutine pour suivre le chemin waypoint par waypoint (NOUVEAU) ---
    IEnumerator FollowPath()
    {
        if (currentPath == null || currentPath.Count == 0)
        {
            isMoving = false;
            yield break; // Quitte la coroutine si le chemin est vide
        }

        // Tant qu'il y a des waypoints à atteindre
        while (currentWaypointIndex < currentPath.Count)
        {
            Node currentWaypointNode = currentPath[currentWaypointIndex];
            Vector3 currentWaypointWorldPosition = currentWaypointNode.worldPosition;

            // Ajuste la hauteur du waypoint cible pour qu'elle corresponde à la hauteur
            // de la case. On suppose que Node.worldPosition.y est la hauteur de la surface de la case.
            targetPosition = currentWaypointWorldPosition; // Met à jour la cible pour le FixedUpdate

            // Attend jusqu'à ce que la boule soit suffisamment proche du waypoint actuel
            while (Vector3.Distance(rb.position, targetPosition) > stopThreshold)
            {
                // Attend la prochaine exécution de FixedUpdate (cycle physique)
                yield return new WaitForFixedUpdate();
            }

            // Une fois le waypoint atteint, passe au suivant
            currentWaypointIndex++;
            isJumping = false; // Réinitialise l'état de saut au cas où le waypoint précédent était un saut
        }

        // Le chemin est entièrement parcouru
        isMoving = false; // Arrête le mouvement
        rb.position = targetPosition; // S'assure que la boule est précisément sur la dernière cible
        currentPath = null; // Nettoie le chemin
        Debug.Log("Path follower completed.");
    }

    // --- FixedUpdate modifié pour fonctionner avec la coroutine FollowPath ---
    // Son rôle est maintenant de déplacer la boule vers la `targetPosition` qui est définie
    // par la coroutine `FollowPath` (le waypoint actuel).
    void FixedUpdate()
    {
        if (isMoving)
        {
            Vector3 currentPosition = rb.position;
            float heightDifference = targetPosition.y - currentPosition.y;

            // Déclenche le saut si la cible est plus haute et qu'on ne saute pas déjà
            if (heightDifference > 0.1f && !isJumping && currentPosition.y <= targetPosition.y + 0.01f)
            {
                startJumpPosition = currentPosition;
                isJumping = true;
                jumpTimer = 0f;
            }

            Vector3 newPos;
            if (isJumping)
            {
                jumpTimer += Time.fixedDeltaTime;
                float progress = Mathf.Clamp01(jumpTimer / jumpDuration);

                float currentJumpHeight = Mathf.Sin(progress * Mathf.PI) * jumpHeight;

                // Cible horizontale pour le saut (sur le plan Y de départ)
                Vector3 currentHorizontalTargetForJump = new Vector3(targetPosition.x, startJumpPosition.y, targetPosition.z);
                Vector3 currentHorizontalPosition = Vector3.Lerp(startJumpPosition, currentHorizontalTargetForJump, progress);

                // Interpolation du Y vers la cible réelle, avec la hauteur de saut ajoutée
                float interpolatedTargetY = Mathf.Lerp(startJumpPosition.y, targetPosition.y, progress);
                newPos = new Vector3(currentHorizontalPosition.x, interpolatedTargetY + currentJumpHeight, currentHorizontalPosition.z);

                if (progress >= 1f)
                {
                    newPos = targetPosition; // Snap final à la fin du saut
                    isJumping = false;
                    // isMoving reste vrai car le mouvement de pathfinding continue
                }
            }
            else // Mouvement horizontal simple ou descente
            {
                newPos = Vector3.MoveTowards(currentPosition, targetPosition, moveSpeed * Time.fixedDeltaTime);
                // La coroutine FollowPath est responsable de passer au prochain waypoint,
                // donc FixedUpdate fait juste un pas vers la cible.
            }
            rb.MovePosition(newPos);
        }
    }
}