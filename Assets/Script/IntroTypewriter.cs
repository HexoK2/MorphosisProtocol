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
    
    public bool IsFinished { get; private set; } = false;

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

    public IEnumerator ShowLines()
    {
        IsFinished = false;
        
        // La ligne de délai a été retirée d'ici
        
        textComponent.text = "";

        foreach (string line in lines)
        {
            if (textComponent.text != "")
            {
                textComponent.text += "\n\n";
            }
            yield return StartCoroutine(TypeLine(line));
            yield return new WaitForSeconds(waitBetweenLines); 
        }
        
        IsFinished = true;
    }

    private IEnumerator TypeLine(string line)
    {
        string currentText = textComponent.text;

        foreach (char letter in line.ToCharArray())
        {
            currentText += letter;
            textComponent.text = currentText;
            yield return new WaitForSeconds(typeSpeed);
        }
    }
}