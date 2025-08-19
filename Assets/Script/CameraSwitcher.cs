using UnityEngine;
using UnityEngine.UI; // Ajoute cette ligne

public class CameraSwitcher : MonoBehaviour
{
    [Tooltip("La caméra principale pour la vue du jeu.")]
    public Camera mainCamera;

    [Tooltip("La caméra pour la vue des objets.")]
    public Camera objectCamera;

    [Tooltip("Le Layer des objets cliquables qui doivent activer l'autre caméra.")]
    public LayerMask clickableLayer;
    
    [Tooltip("Le script de mouvement du joueur pour vérifier si la torche est équipée.")]
    public PlayerMovement playerMovement;

    private GameObject currentAnimatedObject = null; // ✅ NOUVEAU : pour suivre l'objet animé

    void Start()
    {
        if (mainCamera != null)
        {
            mainCamera.gameObject.SetActive(true);
        }
        if (objectCamera != null)
        {
            objectCamera.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, clickableLayer))
            {
                Debug.Log("Raycast a touché l'objet : " + hit.collider.gameObject.name);
                
                Debug.Log("Valeur de hasTorch : " + playerMovement.hasTorch);

                if (mainCamera.gameObject.activeSelf && playerMovement.hasTorch)
                {
                    ObjectInfo info = hit.collider.GetComponent<ObjectInfo>();
                    if (info != null && info.cameraViewPoint != null)
                    {
                        // ✅ MODIFICATION : on passe l'objet cliqué à la méthode
                        SwitchToObjectView(info.cameraViewPoint, hit.collider.gameObject);
                    }
                }
            }
        }
        
        if (objectCamera.gameObject.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            SwitchToMainView();
        }
    }

    // ✅ MODIFICATION : On a maintenant un paramètre pour l'objet cliqué
    public void SwitchToObjectView(GameObject cameraViewPoint, GameObject clickedObject)
    {
        if (mainCamera == null || objectCamera == null) return;

        mainCamera.gameObject.SetActive(false);
        objectCamera.transform.position = cameraViewPoint.transform.position;
        objectCamera.transform.rotation = cameraViewPoint.transform.rotation;
        objectCamera.gameObject.SetActive(true);

        // ✅ NOUVEAU : On récupère l'animation et on la lance
        ObjectInfo info = clickedObject.GetComponent<ObjectInfo>();
        if (info != null && info.animationComponent != null)
        {
            info.animationComponent.Play();
            currentAnimatedObject = clickedObject; // On garde une référence à l'objet pour l'arrêter plus tard
        }
    }

    public void SwitchToMainView()
    {
        if (mainCamera == null || objectCamera == null) return;
        
        // ✅ NOUVEAU : On arrête l'animation avant de quitter la vue
        if (currentAnimatedObject != null)
        {
            Animation anim = currentAnimatedObject.GetComponent<Animation>();
            if (anim != null)
            {
                anim.Stop();
            }
            currentAnimatedObject = null;
        }
        
        mainCamera.gameObject.SetActive(true);
        objectCamera.gameObject.SetActive(false);
    }
}