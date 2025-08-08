using UnityEngine;

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
                
                // NOUVEAU : On affiche la valeur de la variable hasTorch
                Debug.Log("Valeur de hasTorch : " + playerMovement.hasTorch);

                if (mainCamera.gameObject.activeSelf && playerMovement.hasTorch)
                {
                    ObjectInfo info = hit.collider.GetComponent<ObjectInfo>();
                    if (info != null && info.cameraViewPoint != null)
                    {
                        SwitchToObjectView(info.cameraViewPoint);
                    }
                }
            }
        }
        
        if (objectCamera.gameObject.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            SwitchToMainView();
        }
    }

    public void SwitchToObjectView(GameObject cameraViewPoint)
    {
        if (mainCamera == null || objectCamera == null) return;

        mainCamera.gameObject.SetActive(false);
        objectCamera.transform.position = cameraViewPoint.transform.position;
        objectCamera.transform.rotation = cameraViewPoint.transform.rotation;
        objectCamera.gameObject.SetActive(true);
    }

    public void SwitchToMainView()
    {
        if (mainCamera == null || objectCamera == null) return;
        
        mainCamera.gameObject.SetActive(true);
        objectCamera.gameObject.SetActive(false);
    }
}