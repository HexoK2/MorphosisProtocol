using UnityEngine;

public class ObjectInfo : MonoBehaviour
{
    [Tooltip("Le point de vue pour la caméra des objets.")]
    public GameObject cameraViewPoint;

    // ✅ NOUVEAU : Une variable booléenne pour dire si cet objet a un panneau
    [Tooltip("Cochez si cet objet doit afficher le panneau des documents.")]
    public bool displaysDocumentPanel = false;

    // ✅ NOUVEAU : Une variable pour le titre de l'info-bulle
    [Tooltip("Le titre à afficher dans l'info-bulle.")]
    public string infoTitle;

    // ✅ NOUVEAU : La description de l'info-bulle
    [Tooltip("La description à afficher dans l'info-bulle.")]
    public string infoDescription;
}