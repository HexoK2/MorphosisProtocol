using UnityEngine;
using System.Collections.Generic;

public class SequenceManager : MonoBehaviour
{
    [Header("Configuration de l'Énigme")]
    [Tooltip("La séquence correcte des dalles (nom des GameObjects)")]
    public string[] correctSequence;

    [Tooltip("Le GameObject à activer/ouvrir une fois la séquence réussie")]
    public GameObject targetDoorOrObject;

    // Liste pour stocker les pressions du joueur
    private List<string> playerSequence = new List<string>();
    private bool isSolved = false;

    void Start()
    {
        if (targetDoorOrObject == null)
        {
            Debug.LogError("La porte/objet cible n'est pas assignée dans le SequenceManager !");
        }
        else
        {
            // Assure-toi que l'objet est fermé au départ
            targetDoorOrObject.SetActive(false);
        }
    }

    /// <summary>
    /// Appelé par chaque dalle quand le joueur l'enfonce.
    /// </summary>
    public void RegisterSlabPress(string slabName)
    {
        if (isSolved) return; // Ne fait rien si c'est déjà résolu

        // 1. Enregistre la dalle enfoncée
        playerSequence.Add(slabName);
        Debug.Log($"Dalle pressée : {slabName}. Séquence actuelle : {string.Join(", ", playerSequence)}");

        // 2. Vérifie si l'étape actuelle est correcte
        int currentStep = playerSequence.Count - 1;

        if (slabName != correctSequence[currentStep])
        {
            Debug.LogWarning("Mauvaise dalle dans la séquence ! Réinitialisation.");
            ResetSequence();
        }
        else if (playerSequence.Count == correctSequence.Length)
        {
            // 3. Vérifie si la séquence complète est correcte
            OnSequenceSolved();
        }
    }

    /// <summary>
    /// Réinitialise la séquence si une erreur est faite.
    /// </summary>
    public void ResetSequence()
    {
        playerSequence.Clear();
        // 🔑 Optionnel : Déclencher un effet sonore ou visuel sur les dalles pour indiquer le reset
    }

    private void OnSequenceSolved()
    {
        isSolved = true;
        targetDoorOrObject.SetActive(true); // Ouvre la porte !

        Debug.Log("🎉 ÉNIGME RÉSOLUE ! La porte est ouverte.");

        // Optionnel : Désactiver toutes les dalles pour qu'elles n'aient plus d'effet
    }
}