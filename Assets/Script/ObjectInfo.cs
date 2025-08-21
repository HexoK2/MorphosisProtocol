using UnityEngine;

public class ObjectInfo : MonoBehaviour
{
    [Tooltip("Le point de vue pour la caméra des objets.")]
    public GameObject cameraViewPoint;

    // ✅ NOUVEAU : Une variable booléenne pour dire si cet objet a un panneau
    [Tooltip("Cochez si cet objet doit afficher le panneau des documents.")]
    public bool displaysDocumentPanel = false;
}