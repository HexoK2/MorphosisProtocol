using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    [Tooltip("La caméra principale pour la vue du jeu.")]
    public Camera mainCamera;

    [Tooltip("La caméra pour la vue des objets.")]
    public Camera objectCamera;

    [Tooltip("Le Layer des objets cliquables qui doivent activer l'autre caméra.")]
    public LayerMask clickableLayer;

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
                // Vérifie si l'objet cliqué a un script ObjectInfo
                ObjectInfo info = hit.collider.GetComponent<ObjectInfo>();
                if (info != null && info.cameraViewPoint != null)
                {
                    // Si la mainCamera est active, on bascule vers la vue de l'objet cliqué
                    if (mainCamera.gameObject.activeSelf)
                    {
                        SwitchToObjectView(info.cameraViewPoint);
                    }
                    // Si on clique une deuxième fois sur un objet, on revient à la mainCamera
                    else
                    {
                        SwitchToMainView();
                    }
                }
            }
        }
        
        // Gère le retour à la caméra principale avec la touche Échap
        if (objectCamera.gameObject.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            SwitchToMainView();
        }
    }

    /// <summary>
    /// Active la caméra d'objet et la positionne au point de vue donné.
    /// </summary>
    private void SwitchToObjectView(GameObject cameraViewPoint)
    {
        if (mainCamera != null) mainCamera.gameObject.SetActive(false);
        if (objectCamera != null && cameraViewPoint != null)
        {
            objectCamera.transform.position = cameraViewPoint.transform.position;
            objectCamera.transform.rotation = cameraViewPoint.transform.rotation;
            objectCamera.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Active la caméra principale et désactive la caméra d'objet.
    /// </summary>
    private void SwitchToMainView()
    {
        if (mainCamera != null) mainCamera.gameObject.SetActive(true);
        if (objectCamera != null) objectCamera.gameObject.SetActive(false);
    }
}