using UnityEngine;

public class ObjectInfo : MonoBehaviour
{
    [Tooltip("Un GameObject vide qui définit la position et la rotation de la caméra pour cette vue.")]
    public GameObject cameraViewPoint;

      // ✅ NOUVEAU : Référence au composant Animation de cet objet
    public Animation animationComponent; 
}