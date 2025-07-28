using UnityEngine;

public class TorchPickup : MonoBehaviour
{
    public GameObject torchModelToHide; // le modèle à désactiver visuellement
    public Light pickupGlow; // facultatif : une lumière verte à éteindre
    public GameObject uiPrompt; // icône d'interaction à cacher

    private bool isPlayerNearby = false;

    void Update()
    {
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.E)) // touche d'interaction
        {
            PlayerMovement player = FindObjectOfType<PlayerMovement>();
            if (player != null)
            {
                player.PickUpTorch();
            }

            if (torchModelToHide != null) torchModelToHide.SetActive(false);
            if (pickupGlow != null) pickupGlow.enabled = false;
            if (uiPrompt != null) uiPrompt.SetActive(false);

            Destroy(gameObject); // Supprime le script ou l'objet complet
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            if (uiPrompt != null) uiPrompt.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            if (uiPrompt != null) uiPrompt.SetActive(false);
        }
    }
}
