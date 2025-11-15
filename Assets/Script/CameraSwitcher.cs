using UnityEngine;
using System.Collections;

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

    [Header("Panneaux UI")]
    [Tooltip("Le panneau de documents à afficher.")]
    public GameObject documentsCanvas;

    [Tooltip("L'Animator du panneau de documents.")]
    public Animator documentsAnimator;

    [Tooltip("Le panneau de l'info-bulle.")]
    public GameObject infoBubblePanel;

    private ObjectHighlighter objectHighlighter;

    void Start()
    {
        objectHighlighter = FindObjectOfType<ObjectHighlighter>();

        mainCamera.gameObject.SetActive(true);
        objectCamera.gameObject.SetActive(false);

        if (documentsCanvas != null)
        {
            documentsCanvas.SetActive(false);
        }

        if (infoBubblePanel != null)
        {
            infoBubblePanel.SetActive(false);
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
                        SwitchToObjectView(info.cameraViewPoint, info.displaysDocumentPanel);
                        objectHighlighter.DisableInfoBubble();
                    }
                }
            }
        }

        if (objectCamera.gameObject.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            SwitchToMainView();
        }
    }

    public void SwitchToObjectView(GameObject cameraViewPoint, bool displayPanel)
    {
        if (mainCamera == null || objectCamera == null) return;

        if (infoBubblePanel != null)
        {
            infoBubblePanel.SetActive(false);
        }

        mainCamera.gameObject.SetActive(false);
        objectCamera.transform.position = cameraViewPoint.transform.position;
        objectCamera.transform.rotation = cameraViewPoint.transform.rotation;
        objectCamera.gameObject.SetActive(true);

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

        objectHighlighter.EnableInfoBubble();
    }

    private IEnumerator DeactivateCanvasAfterAnimation(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (documentsCanvas != null)
        {
            documentsCanvas.SetActive(false);
        }
    }
}
