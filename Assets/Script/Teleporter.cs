using UnityEngine;
using UnityEngine.SceneManagement;

public class Teleporter : MonoBehaviour
{
    [Header("Destination")]
    public string destinationSceneName;
    public Transform destinationPosition;

    [Header("Conditions")]
    public PlayerMovement playerMovement;

    [Header("Configuration")]
    [Tooltip("Le Layer sur lequel se trouve le téléporteur.")]
    public LayerMask teleporterLayer;

    // NOUVEAU : Une variable pour que le script ne s'exécute qu'une fois
    private bool isActivated = false;

    // Cette méthode est appelée une fois par frame
    void Update()
    {
        // On s'assure que le script n'a pas déjà été activé
        if (isActivated) return;

        // On vérifie si le bouton gauche de la souris est cliqué
        if (Input.GetMouseButtonDown(0))
        {
            // On crée un Raycast depuis la caméra, vers la position de la souris
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // On lance le Raycast, en s'assurant qu'il ne touche QUE le Layer du téléporteur
            if (Physics.Raycast(ray, out hit, 50f, teleporterLayer))
            {
                // On vérifie que l'objet cliqué est bien celui sur lequel ce script est attaché
                if (hit.collider.gameObject == this.gameObject)
                {
                    // Si la condition de mutation est remplie...
                    if (playerMovement != null && playerMovement.IsSmall)
                    {
                        if (destinationPosition != null)
                        {
                            Debug.Log("Condition remplie ! Téléportation du joueur...");
                            TeleportPlayer();
                            isActivated = true; // ⬅️ Le script est désactivé
                        }
                    }
                    else
                    {
                        Debug.Log("Condition non remplie. La sortie est bloquée car le joueur n'est pas dans la bonne forme.");
                    }
                }
            }
        }
    }

    void Start()
    {
        if (playerMovement == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerMovement = player.GetComponent<PlayerMovement>();
            }
        }
    }

    // ✅ CORRECTION PRINCIPALE : Méthode de téléportation complète
    void TeleportPlayer()
    {
        // Ajuste la position du joueur
        Vector3 teleportPosition = destinationPosition.position;
        teleportPosition.y += playerMovement.verticalOffsetOnGround; // Ajoute l'offset vertical
        playerMovement.transform.position = teleportPosition;

        // ✅ CRUCIAL : Met à jour les variables internes du système de mouvement
        UpdatePlayerGridPosition();
        
        // ✅ Arrête tout mouvement en cours
        StopCurrentMovement();
        
        // ✅ Remet à jour la position sûre
        playerMovement.lastSafePosition = teleportPosition;
        
        Debug.Log($"Joueur téléporté à {teleportPosition}. Nouveau cube de grille : {playerMovement.currentGridCube?.name}");
    }

    // ✅ NOUVELLE MÉTHODE : Met à jour la position du joueur sur la grille
    void UpdatePlayerGridPosition()
    {
        // Trouve le cube de grille le plus proche de la nouvelle position
        GameObject newGridCube = playerMovement.FindNearestGridCube(playerMovement.transform.position);
        
        if (newGridCube != null)
        {
            // Met à jour les variables de suivi de position
            playerMovement.previousGridCube = playerMovement.currentGridCube;
            playerMovement.currentGridCube = newGridCube;
            
            Debug.Log($"Position sur grille mise à jour. Nouveau cube : {newGridCube.name}");
        }
        else
        {
            Debug.LogError("Aucun cube de grille trouvé près de la position de téléportation ! Le joueur pourrait être hors grille.");
        }
    }

    // ✅ NOUVELLE MÉTHODE : Arrête le mouvement en cours
    void StopCurrentMovement()
    {
        // On ne peut pas accéder directement aux variables privées, alors on utilise une approche alternative
        
        // Vide le chemin calculé (ces variables sont publiques)
        if (playerMovement.pathCalculated)
        {
            playerMovement.pathCalculated = false;
            playerMovement.path.Clear();
            playerMovement.currentPathIndex = 0;
            
            // Vide la visualisation du chemin
            if (playerMovement.lr != null)
            {
                playerMovement.lr.positionCount = 0;
            }
        }
        
        // Arrête la vélocité du Rigidbody
        if (playerMovement.rb != null)
        {
            playerMovement.rb.linearVelocity = Vector3.zero;
            playerMovement.rb.angularVelocity = Vector3.zero;
        }
        
        // Remet à zéro les matériaux des cellules
        playerMovement.ResetAllCellMaterials();
    }
}