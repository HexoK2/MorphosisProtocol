using UnityEngine;
using System.Collections.Generic; // Pour utiliser les listes si besoin, mais on utilise un tableau ici.

public class TuilesActivator : MonoBehaviour
{
    // Nom du Layer de tes cubes. Pense à le renseigner dans l'Inspecteur !
    [Tooltip("Le nom du Layer des objets à activer si le joueur est grand.")]
    public string layerName = "BigMutationTiles";

    // Tableau pour stocker tous les cubes trouvés
    private GameObject[] cubesToControl;

    // Référence au script du joueur qui contient la variable IsBig
    private PlayerMovement playerMovementScript;

    // Pour éviter de faire la vérification à chaque frame si ce n'est pas nécessaire
    private bool previousIsBigState;

    void Start()
    {
        // 1. Trouver et stocker la référence au script PlayerMovement
        GameObject playerBall = GameObject.FindGameObjectWithTag("Player");
        if (playerBall != null)
        {
            playerMovementScript = playerBall.GetComponent<PlayerMovement>();
            if (playerMovementScript == null)
            {
                Debug.LogError("Le script 'PlayerMovement' est introuvable sur l'objet avec le tag 'Player'. Le script ActiveCubesSiGrand ne fonctionnera pas.");
                enabled = false; // Désactive ce script s'il ne trouve pas le PlayerMovement
                return;
            }
        }
        else
        {
            Debug.LogError("Aucun objet avec le tag 'Player' trouvé. Le script ActiveCubesSiGrand ne fonctionnera pas.");
            enabled = false;
            return;
        }

        // 2. Trouver tous les cubes avec le layer spécifié
        FindCubesByLayer();

        // 3. Initialiser l'état initial des cubes
        if (cubesToControl != null && cubesToControl.Length > 0)
        {
            // On s'assure que l'état initial du joueur est pris en compte
            previousIsBigState = playerMovementScript.IsBig;
            UpdateCubesState();
        }
        else
        {
            Debug.LogWarning("Aucun cube trouvé avec le layer '" + layerName + "'. Le script ne fera rien.");
        }
    }

    void Update()
    {
        // 4. Vérifier si l'état de la variable IsBig a changé
        if (playerMovementScript != null)
        {
            bool currentIsBigState = playerMovementScript.IsBig;

            // On ne met à jour l'état des cubes que si la variable a changé
            if (currentIsBigState != previousIsBigState)
            {
                UpdateCubesState();
                previousIsBigState = currentIsBigState; // On met à jour l'état précédent
            }
        }
    }

    // Fonction pour trouver tous les objets ayant un Layer donné
    private void FindCubesByLayer()
    {
        List<GameObject> tempCubes = new List<GameObject>();
        int layer = LayerMask.NameToLayer(layerName);

        if (layer == -1)
        {
            Debug.LogError("Le layer '" + layerName + "' n'existe pas. Veuillez le créer ou vérifier l'orthographe.");
            return;
        }

        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.layer == layer)
            {
                tempCubes.Add(obj);
            }
        }
        cubesToControl = tempCubes.ToArray();
    }

    // Fonction qui active/désactive tous les cubes
    private void UpdateCubesState()
    {
        if (cubesToControl == null || playerMovementScript == null) return;

        bool shouldBeActive = playerMovementScript.IsBig;

        foreach (GameObject cube in cubesToControl)
        {
            if (cube != null)
            {
                cube.SetActive(shouldBeActive);
            }
            
        }

        Debug.Log("L'état des cubes a été mis à jour. Ils sont maintenant " + (shouldBeActive ? "activés." : "désactivés."));
    }
}