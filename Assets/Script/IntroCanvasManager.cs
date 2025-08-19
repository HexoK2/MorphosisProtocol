using UnityEngine;
using UnityEngine.UI;

public class IntroCanvasManager : MonoBehaviour
{
    // Référence à ton Canvas d'introduction
    public GameObject introCanvas;

    // ✅ NOUVEAU : Référence au script de mouvement du joueur
    public PlayerMovement playerMovementScript;

    void Start()
    {
        // Au démarrage, on s'assure que le canvas est bien actif
        if (introCanvas != null)
        {
            introCanvas.SetActive(true);
        }

        // ✅ NOUVEAU : On désactive le script de mouvement du joueur
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = false;
        }
    }

    void Update()
    {
        // On vérifie si la touche 'E' est pressée
        if (Input.GetKeyDown(KeyCode.E))
        {
            // On désactive le canvas
            if (introCanvas != null)
            {
                introCanvas.SetActive(false);
            }
            
            // ✅ NOUVEAU : On réactive le script de mouvement du joueur
            if (playerMovementScript != null)
            {
                playerMovementScript.enabled = true;
            }

            // On désactive ce script pour qu'il ne se relance pas
            this.enabled = false;
        }
    }
}