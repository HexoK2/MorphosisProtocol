using UnityEngine;
using System.Collections.Generic;

public class BigMutationTileActivator : MonoBehaviour
{
    [Tooltip("Le nom du Layer où se trouvent les tuiles qui doivent apparaître/disparaître.")]
    public string targetLayerName = "BigMutationTiles";

    private PlayerMovement playerMovementScript;
    private List<GameObject> bigMutationTiles = new List<GameObject>();
    private bool isPlayerBig;

    void Start()
    {
        // 1. Trouver la référence au script PlayerMovement
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerMovementScript = player.GetComponent<PlayerMovement>();
            if (playerMovementScript == null)
            {
                Debug.LogError("PlayerMovement script introuvable sur le GameObject avec le tag 'Player'.");
                enabled = false;
                return;
            }
        }
        else
        {
            Debug.LogError("Aucun GameObject avec le tag 'Player' trouvé dans la scène.");
            enabled = false;
            return;
        }

        // 2. Trouver toutes les tuiles sur le Layer spécifié
        int targetLayer = LayerMask.NameToLayer(targetLayerName);
        if (targetLayer == -1)
        {
            Debug.LogError("Le Layer '" + targetLayerName + "' n'existe pas ! Veuillez le créer dans Unity.");
            enabled = false;
            return;
        }

        // Utilise FindObjectsOfType (moins performant, mais simple pour l'exemple) ou une meilleure approche pour un grand jeu.
        foreach (GameObject tile in FindObjectsOfType<GameObject>())
        {
            if (tile.layer == targetLayer)
            {
                bigMutationTiles.Add(tile);
            }
        }

        // 3. Initialiser l'état des tuiles au démarrage
        if (bigMutationTiles.Count > 0)
        {
            isPlayerBig = playerMovementScript.IsBig;
            foreach (GameObject tile in bigMutationTiles)
            {
                tile.SetActive(isPlayerBig);
            }
        }
        
        // 4. Mettre à jour la grille au démarrage
        playerMovementScript.RefreshGrid();
    }

    void Update()
    {
        // Surveiller l'état de mutation du joueur
        if (isPlayerBig != playerMovementScript.IsBig)
        {
            isPlayerBig = playerMovementScript.IsBig;
            ToggleTiles(isPlayerBig);
        }
    }

    void ToggleTiles(bool shouldBeActive)
    {
        foreach (GameObject tile in bigMutationTiles)
        {
            if (tile != null)
            {
                tile.SetActive(shouldBeActive);
            }
        }
        
        // 5. Demander au PlayerMovement de rafraîchir sa grille après le changement
        playerMovementScript.RefreshGrid();
    }
}