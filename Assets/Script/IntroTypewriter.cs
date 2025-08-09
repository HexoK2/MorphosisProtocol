using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class IntroTypewriter : MonoBehaviour
{
    private TextMeshProUGUI textComponent;

    [Tooltip("La vitesse à laquelle les lettres apparaissent (en secondes par lettre).")]
    public float typeSpeed = 0.05f;
    
    [Tooltip("Le temps d'attente entre chaque ligne.")]
    public float waitBetweenLines = 2.0f;

    [Tooltip("Les phrases à afficher. Chaque élément du tableau sera affiché un par un.")]
    [TextArea(3, 10)]
    public List<string> lines;

    void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
        if (textComponent == null)
        {
            Debug.LogError("IntroTypewriter requires a TextMeshProUGUI component on the same GameObject.");
            enabled = false;
        }
        else
        {
            textComponent.text = "";
        }
    }

    void Start()
    {
        StartCoroutine(ShowLines());
    }

    private IEnumerator ShowLines()
    {
        // Attendre 1 seconde avant de commencer la première ligne pour un meilleur timing
        yield return new WaitForSeconds(1.0f);
        
        // On ne réinitialise le texte qu'une seule fois au début
        textComponent.text = "";

        foreach (string line in lines)
        {
            // Ajoute un saut de ligne si ce n'est pas la première ligne
            if (textComponent.text != "")
            {
                textComponent.text += "\n\n";
            }

            // Commence la coroutine pour afficher la ligne
            yield return StartCoroutine(TypeLine(line));
            
            // Attendre avant de passer à la ligne suivante
            yield return new WaitForSeconds(waitBetweenLines); 
        }
        
        Debug.Log("Toutes les lignes ont été affichées.");
    }

    private IEnumerator TypeLine(string line)
    {
        // MODIFICATION : Au lieu de réinitialiser le texte, on ajoute la ligne
        // La ligne `textComponent.text = "";` a été retirée d'ici
        string currentText = textComponent.text;

        foreach (char letter in line.ToCharArray())
        {
            currentText += letter;
            textComponent.text = currentText;
            yield return new WaitForSeconds(typeSpeed);
        }
    }
}