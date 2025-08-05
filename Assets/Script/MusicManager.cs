using UnityEngine;

public class MusicManager : MonoBehaviour
{
    private static MusicManager instance;

    void Awake()
    {
        if (instance == null)
        {
            // S'il n'y a pas d'instance, celle-ci devient l'instance unique
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            // S'il existe déjà une instance, on détruit ce nouvel objet pour éviter le doublon
            Destroy(this.gameObject);
        }
    }
}