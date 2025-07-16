using UnityEngine;
using System.Collections.Generic;

public class Pathfinding : MonoBehaviour
{
    public GridManager gridManager; // Référence à ton GridManager

    void Awake()
    {
        if (gridManager == null)
        {
            gridManager = FindObjectOfType<GridManager>();
            if (gridManager == null)
            {
                Debug.LogError("GridManager not found in scene for Pathfinding script!");
            }
        }
    }

    // Fonction principale pour trouver un chemin
    public List<Node> FindPath(Vector3 startPos, Vector3 targetPos)
    {
        Node startNode = gridManager.NodeFromWorldPoint(startPos);
        Node targetNode = gridManager.NodeFromWorldPoint(targetPos);

        if (!targetNode.isWalkable || !startNode.isWalkable)
        {
            Debug.LogWarning("Start or Target position is not walkable. Pathfinding aborted.");
            return null; // Impossible d'atteindre une destination non praticable ou de partir d'un obstacle
        }

        List<Node> openSet = new List<Node>(); // Nœuds à évaluer
        HashSet<Node> closedSet = new HashSet<Node>(); // Nœuds déjà évalués (pour des recherches plus rapides)

        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < currentNode.fCost || (openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost))
                {
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode == targetNode)
            {
                return RetracePath(startNode, targetNode); // Reconstruit et retourne le chemin
            }

            foreach (Node neighbor in gridManager.GetNeighbors(currentNode))
            {
                if (!neighbor.isWalkable || closedSet.Contains(neighbor))
                {
                    continue;
                }

                int newMovementCostToNeighbor = currentNode.gCost + GetDistance(currentNode, neighbor);
                if (newMovementCostToNeighbor < neighbor.gCost || !openSet.Contains(neighbor))
                {
                    neighbor.gCost = newMovementCostToNeighbor;
                    neighbor.hCost = GetDistance(neighbor, targetNode);
                    neighbor.parent = currentNode;

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }
        Debug.LogWarning("No path found!");
        return null;
    }

    List<Node> RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        path.Reverse(); // Inverse le chemin pour qu'il soit du départ à la cible
        return path;
    }

    // Calcule la "distance" entre deux nœuds (coût pour se déplacer entre eux)
    // Utilise un coût de 10 pour le cardinal et 14 pour la diagonale (environ sqrt(2)*10)
    int GetDistance(Node nodeA, Node nodeB)
    {
        int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);

        if (dstX > dstY)
            return 14 * dstY + 10 * (dstX - dstY);
        return 14 * dstX + 10 * (dstY - dstX);
    }
}