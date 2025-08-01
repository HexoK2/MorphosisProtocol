using UnityEngine;

public class TorchPickupByClick : MonoBehaviour
{
    public GameObject torchModelToHide;      // Le modèle visible dans la scène (la torche posée)
    public GameObject torchInPlayerHand;     // Le modèle activé dans la main du joueur
    public GameObject uiPrompt;              // Icône ou texte d'interaction
    public AudioClip pickupSound;            // Son de ramassage
    public AudioSource audioSource;          // Source audio (peut être sur le joueur ou sur la torche)

    private bool isHovering = false;

void Update()
{
    if (isHovering && Input.GetMouseButtonDown(0))
    {
        PlayerMovement player = FindObjectOfType<PlayerMovement>();
        if (player != null && !player.hasTorch)
        {
            player.PickUpTorch(); // Active la variable
        }

        // Désactiver l'objet de la scène
        if (torchModelToHide != null)
        {
            torchModelToHide.SetActive(false);
            Debug.Log("Torch Ramassée");
        }

        // Activer la torche en main
        if (torchInPlayerHand != null)
        {
            torchInPlayerHand.SetActive(true);
            Debug.Log("Torch en main activée");
        }

        // Jouer le son
        if (audioSource != null && pickupSound != null)
        {
            audioSource.PlayOneShot(pickupSound);
        }

        // Cacher l'UI
        if (uiPrompt != null)
        {
            uiPrompt.SetActive(false);
        }

        // Détruire ce script (l'interaction n’est plus nécessaire)
        Destroy(this);
    }
}


    void OnMouseEnter()
    {
        isHovering = true;
        if (uiPrompt != null)
            uiPrompt.SetActive(true);
    }

    void OnMouseExit()
    {
        isHovering = false;
        if (uiPrompt != null)
            uiPrompt.SetActive(false);
    }
}
