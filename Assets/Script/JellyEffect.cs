using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class JellyEffect : MonoBehaviour
{
    // Variables pour l'effet de gélatine
    public float bounceSpeed = 4f;
    public float stretchFactor = 0.1f;
    public float squashFactor = 0.1f;
    
    private Mesh mesh;
    private Vector3[] originalVertices;
    private Vector3[] displacedVertices;
    private Vector3[] vertexVelocities;

    // Cette fonction est appelée au démarrage du jeu
    void Start()
    {
        // Récupère le composant MeshFilter du GameObject
        mesh = GetComponent<MeshFilter>().mesh;
        
        // Sauvegarde les sommets d'origine du maillage
        originalVertices = mesh.vertices;
        
        // Crée des copies pour les sommets déplacés et les vitesses
        displacedVertices = new Vector3[originalVertices.Length];
        System.Array.Copy(originalVertices, displacedVertices, originalVertices.Length);
        
        vertexVelocities = new Vector3[originalVertices.Length];
    }

    // Cette fonction est appelée à chaque frame
    void FixedUpdate()
    {
        // Déplace les sommets déplacés vers leur position d'origine
        for (int i = 0; i < originalVertices.Length; i++)
        {
            // Calcule la distance entre la position actuelle et la position d'origine
            Vector3 distance = displacedVertices[i] - originalVertices[i];
            
            // Fait rebondir le sommet en fonction de la vitesse et de la distance
            vertexVelocities[i] -= distance * bounceSpeed * Time.deltaTime;
            
            // Met à jour la position du sommet déplacé
            displacedVertices[i] += vertexVelocities[i] * Time.deltaTime;
        }

        // Met à jour les sommets du maillage réel avec les sommets déplacés
        mesh.vertices = displacedVertices;
        
        // Recalcule les normales pour que la lumière s'affiche correctement
        mesh.RecalculateNormals();
    }
    
    // Cette fonction est appelée lors d'une collision
    void OnCollisionEnter(Collision collision)
    {
        // Pour chaque point de contact lors de la collision
        foreach (ContactPoint contact in collision.contacts)
        {
            // Calcule la force de l'impact
            float impactForce = collision.relativeVelocity.magnitude;

            // Déplace les sommets proches du point de contact
            for (int i = 0; i < originalVertices.Length; i++)
            {
                // Vérifie si le sommet est proche du point de contact
                if ((displacedVertices[i] - contact.point).magnitude < 0.5f)
                {
                    // Applique un "coup" sur le sommet dans la direction opposée à la collision
                    vertexVelocities[i] += contact.normal * impactForce * squashFactor;
                    
                    // Étire le sommet dans la direction de la collision
                    displacedVertices[i] += contact.normal * impactForce * stretchFactor;
                }
            }
        }
    }
}