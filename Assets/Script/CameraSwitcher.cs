using UnityEngine;
using UnityEngine.UI;

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

    [Tooltip("Le Canvas qui affiche les documents.")]
    public GameObject documentsCanvas;
    // ✅ NOUVEAU : Référence au composant Animator du Canvas
    public Animator documentsAnimator;
    
    void Start()
    {
        mainCamera.gameObject.SetActive(true);
        objectCamera.gameObject.SetActive(false);
        if (documentsCanvas != null)
        {
            documentsCanvas.SetActive(false);
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
                if (mainCamera.gameObject.activeSelf && playerMovement.hasTorch)
                {
                    ObjectInfo info = hit.collider.GetComponent<ObjectInfo>();
                    if (info != null && info.cameraViewPoint != null)
                    {
                        // ✅ NOUVEAU : On passe le booléen à la méthode
                        SwitchToObjectView(info.cameraViewPoint, info.displaysDocumentPanel);
                    }
                }
            }
        }
        
        if (objectCamera.gameObject.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            SwitchToMainView();
        }
    }

    // ✅ MODIFICATION : La méthode reçoit le booléen
    public void SwitchToObjectView(GameObject cameraViewPoint, bool displayPanel)
    {
        if (mainCamera == null || objectCamera == null) return;
        
        mainCamera.gameObject.SetActive(false);
        objectCamera.transform.position = cameraViewPoint.transform.position;
        objectCamera.transform.rotation = cameraViewPoint.transform.rotation;
        objectCamera.gameObject.SetActive(true);
        
        // ✅ NOUVEAU : On vérifie si on doit activer le panneau
        if (displayPanel && documentsCanvas != null)
        {
            documentsCanvas.SetActive(true);
            if (documentsAnimator != null)
            {
                documentsAnimator.SetTrigger("ShowPanel");
            }
        }
    }

    public void SwitchToMainView()
    {
        if (mainCamera == null || objectCamera == null) return;
        
        // ✅ NOUVEAU : On désactive le panneau si il est actif
        if (documentsCanvas != null && documentsCanvas.activeSelf)
        {
            if (documentsAnimator != null)
            {
                documentsAnimator.SetTrigger("HidePanelAnimation");
            }
            StartCoroutine(DeactivateCanvasAfterAnimation(1.0f));
        }
        
        objectCamera.gameObject.SetActive(false);
        mainCamera.gameObject.SetActive(true);
    }

    private System.Collections.IEnumerator DeactivateCanvasAfterAnimation(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (documentsCanvas != null)
        {
            documentsCanvas.SetActive(false);
        }
    }
}
