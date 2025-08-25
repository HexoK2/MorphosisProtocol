using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class ObjectHighlighter : MonoBehaviour
{
    [Tooltip("Le LayerMask des objets que ce script peut interagir.")]
    public LayerMask interactiveLayers;

    [Header("UI")]
    [Tooltip("Le panneau (GameObject) de l'info-bulle.")]
    public GameObject infoBubblePanel;
    // ✅ NOUVEAU : Référence au titre et à la description
    [Tooltip("Le TextMeshPro qui affichera le titre.")]
    public TextMeshProUGUI infoTitleText;
    [Tooltip("Le TextMeshPro qui affichera la description.")]
    public TextMeshProUGUI infoDescriptionText;

    [Tooltip("Décalage du panneau par rapport à la souris.")]
    public Vector3 mouseOffset = new Vector3(50, -50, 0); // Décale de 50px à droite et 50px vers le bas

    private GameObject currentHoveredObject = null;

    void Start()
    {
        if (infoBubblePanel == null || infoTitleText == null || infoDescriptionText == null)
        {
            Debug.LogError("Le panneau ou les textes de l'info-bulle ne sont pas assignés !");
            enabled = false;
            return;
        }
        infoBubblePanel.SetActive(false);
    }

    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, interactiveLayers))
        {
            GameObject hitObject = hit.collider.gameObject;
            ObjectInfo info = hitObject.GetComponent<ObjectInfo>();

            if (info != null && info.infoTitle != "" && info.infoDescription != "")
            {
                ShowInfoBubble(info.infoTitle, info.infoDescription);
                currentHoveredObject = hitObject;
            }
            else
            {
                HideInfoBubble();
            }
        }
        else
        {
            HideInfoBubble();
        }

        // ✅ NOUVEAU : On fait suivre le panneau avec le décalage
        infoBubblePanel.transform.position = Input.mousePosition + mouseOffset;
    }
    
    

    // ✅ MODIFICATION : La méthode reçoit le titre et la description
    private void ShowInfoBubble(string title, string description)
    {
        infoBubblePanel.SetActive(true);
        infoTitleText.text = title;
        infoDescriptionText.text = description;
    }

    // ✅ NOUVEAU : Une méthode publique pour afficher l'info-bulle
    public void ShowInfoOnHover(string title, string description)
    {
        // On s'assure que le panel est activé
        infoBubblePanel.SetActive(true);
        // On met à jour les textes
        infoTitleText.text = title;
        infoDescriptionText.text = description;
    }

    // ✅ NOUVEAU : Une méthode publique pour cacher l'info-bulle
    public void HideInfoBubble()
    {
        if (infoBubblePanel.activeSelf)
        {
            infoBubblePanel.SetActive(false);
        }
    }


}