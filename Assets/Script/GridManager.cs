using UnityEngine;
using System.Collections.Generic; // Pour List

public class GridManager : MonoBehaviour
{
    public LayerMask unwalkableMask; // Un LayerMask pour définir ce qui est un obstacle (ex: créer un layer "Obstacle")
    public LayerMask groundTileLayer; // Un LayerMask pour les cases de sol (ex: créer un layer "GroundTileLayer")
    public Vector2 gridWorldSize; // La taille totale de ta grille en unités mondiales (ex: 20x20)
    public float nodeRadius; // Le rayon d'un nœud (la moitié de la taille d'une case, ex: 0.5 pour des cases de 1x1)

    private Node[,] grid; // La grille de nœuds
    private float nodeDiameter; // Le diamètre d'un nœud (taille complète d'une case)
    private int gridSizeX, gridSizeY; // Dimensions de la grille en nombre de cases

    void Awake()
    {
        nodeDiameter = nodeRadius * 2;
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);
        CreateGrid(); // Crée la grille au démarrage
    }

    void CreateGrid()
    {
        grid = new Node[gridSizeX, gridSizeY];
        // Calcule le coin inférieur gauche de la grille en coordonnées mondiales
        Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.forward * gridWorldSize.y / 2;

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                // Calcule la position mondiale du centre de la case
                Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (y * nodeDiameter + nodeRadius);

                // Détermine si cette case est un obstacle (si un collider sur le layer 'unwalkableMask' est présent)
                bool obstaclePresent = Physics.CheckSphere(worldPoint, nodeRadius - 0.05f, unwalkableMask); // Petit ajustement du rayon pour éviter les colliders partiels

                // Détermine si c'est une case de sol (si un collider sur le layer 'groundTileLayer' est présent)
                bool groundTilePresent = Physics.CheckSphere(worldPoint, nodeRadius - 0.05f, groundTileLayer);

                // Une case est praticable si c'est une case de sol ET qu'il n'y a pas d'obstacle dessus
                bool isWalkable = groundTilePresent && !obstaclePresent;

                grid[x, y] = new Node(isWalkable, worldPoint, x, y);
            }
        }
    }

    // Donne la liste des nœuds voisins d'un nœud donné (pour l'algorithme A*)
    public List<Node> GetNeighbors(Node node)
    {
        List<Node> neighbors = new List<Node>();

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue; // Ignore le nœud lui-même

                // Si tu veux seulement les mouvements cardinaux (haut, bas, gauche, droite), décommente la ligne suivante:
                // if (x != 0 && y != 0) continue; // Ignore les diagonales

                int checkX = node.gridX + x;
                int checkY = node.gridY + y;

                // Vérifie si le voisin est dans les limites de la grille
                if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
                {
                    neighbors.Add(grid[checkX, checkY]);
                }
            }
        }
        return neighbors;
    }

    // Convertit une position mondiale en nœud de grille
    public Node NodeFromWorldPoint(Vector3 worldPosition)
    {
        // Calcule le pourcentage de la position dans la grille par rapport à sa taille mondiale
        // transform.position est le centre de la grille, donc on ajuste.
        float percentX = (worldPosition.x - (transform.position.x - gridWorldSize.x / 2)) / gridWorldSize.x;
        float percentY = (worldPosition.z - (transform.position.z - gridWorldSize.y / 2)) / gridWorldSize.y; // Utilise Z pour la coordonnée Y de la grille 2D

        // S'assure que les pourcentages sont entre 0 et 1
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        // Convertit les pourcentages en coordonnées de grille
        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);
        return grid[x, y];
    }

    // --- Visualisation de la grille dans l'éditeur (très utile pour le débogage) ---
    void OnDrawGizmos()
    {
        // Dessine un cube filaire représentant la taille totale de la grille
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1, gridWorldSize.y));

        if (grid != null)
        {
            // Dessine chaque nœud de la grille
            foreach (Node n in grid)
            {
                // Change la couleur en fonction de la praticabilité du nœud
                Gizmos.color = (n.isWalkable) ? Color.white : Color.red; // Blanc pour praticable, rouge pour obstacle
                Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter - .1f)); // Un peu plus petit que le diamètre pour les espaces
            }
        }
    }
}