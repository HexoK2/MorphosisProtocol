using UnityEngine;

public class TorchPickup2 : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player != null && !player.hasTorch)
        {
            player.PickUpTorch(); // Active la variable
            gameObject.SetActive(false); // Cache la torche ramassée
        }
    }
}
