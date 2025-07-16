using UnityEngine; // Nécessaire pour Vector3

// Node est une classe simple, pas un MonoBehaviour
public class Node
{
    public bool isWalkable; // Vrai si la case est praticable, faux si c'est un obstacle
    public Vector3 worldPosition; // La position mondiale du centre de cette case
    public int gridX; // Coordonnée X dans la grille
    public int gridY; // Coordonnée Y (ou Z dans un monde 3D vu du dessus) dans la grille

    // Variables pour l'algorithme A*
    public int gCost; // Coût depuis le nœud de départ jusqu'à ce nœud
    public int hCost; // Coût heuristique estimé depuis ce nœud jusqu'au nœud cible
    public Node parent; // Le nœud précédent dans le chemin (pour reconstruire le chemin trouvé)

    // Coût total (fCost = gCost + hCost)
    public int fCost {
        get {
            return gCost + hCost;
        }
    }

    public Node(bool _isWalkable, Vector3 _worldPos, int _gridX, int _gridY)
    {
        isWalkable = _isWalkable;
        worldPosition = _worldPos;
        gridX = _gridX;
        gridY = _gridY;
    }
}