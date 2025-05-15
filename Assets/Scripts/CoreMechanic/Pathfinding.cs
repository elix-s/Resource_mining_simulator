using System.Collections.Generic;
using System.Linq;
using UnityEngine; 

public static class Pathfinding
{
    public static List<GraphNode> FindPath(GraphNode startNode, GraphNode endNode)
    {
        if (startNode == null || endNode == null)
        {
            Debug.LogError("Start or End node is null.");
            return null;
        }

        var distances = new Dictionary<GraphNode, float>();
        var previousNodes = new Dictionary<GraphNode, GraphNode>();
        var unvisitedNodes = new List<GraphNode>();
        
        List<GraphNode> allNodes = new List<GraphNode>();
        
        if (GameManager.Instance != null)
        {
            allNodes.AddRange(GameManager.Instance.Bases); 
            allNodes.AddRange(GameManager.Instance.Mines); 
            allNodes.AddRange(GameManager.Instance.Waypoints);
            allNodes = allNodes.Distinct().ToList();
        }
        else
        {
            return null;
        }
        
        foreach (var node in allNodes)
        {
            distances[node] = float.MaxValue;
            previousNodes[node] = null;
            unvisitedNodes.Add(node);
        }

        if (!distances.ContainsKey(startNode))
        {
            Debug.LogError("StartNode not found in the graph's node list");
            return null; 
        }
        
        distances[startNode] = 0;

        while (unvisitedNodes.Count > 0)
        {
            unvisitedNodes = unvisitedNodes.OrderBy(node => distances[node]).ToList();
            GraphNode currentNode = unvisitedNodes[0];
            unvisitedNodes.RemoveAt(0);

            if (currentNode == endNode) 
            {
                var path = new List<GraphNode>();
                GraphNode u = endNode;
                
                while (u != null)
                {
                    path.Add(u);
                    
                    if (!previousNodes.TryGetValue(u, out u)) 
                    {
                         break;
                    }
                }
                
                path.Reverse();
                return path;
            }
            
            if (Mathf.Approximately(distances[currentNode], float.MaxValue))
            {
                break;
            }

            if (currentNode.Edges == null)
            {
                continue;
            }

            foreach (var edge in currentNode.Edges)
            {
                if (edge == null || edge.NodeA == null || edge.NodeB == null)
                {
                    continue;
                }

                GraphNode neighbor = (edge.NodeA == currentNode) ? edge.NodeB : edge.NodeA;
                
                if (!distances.ContainsKey(neighbor))
                {
                    continue;
                }
                
                float tentativeDistance = distances[currentNode] + edge.Length;

                if (tentativeDistance < distances[neighbor])
                {
                    distances[neighbor] = tentativeDistance;
                    previousNodes[neighbor] = currentNode;
                }
            }
        }
        
        return null; 
    }

    public static float GetPathCost(GraphNode startNode, GraphNode endNode)
    {
        if (startNode == endNode) return 0; 

        List<GraphNode> path = FindPath(startNode, endNode);
        if (path == null || path.Count < 2) return float.MaxValue;

        float cost = 0;
        
        for (int i = 0; i < path.Count - 1; i++)
        {
            GraphNode u = path[i];
            GraphNode v = path[i + 1];
            GraphEdge edge = u.Edges.FirstOrDefault(e =>
                (e.NodeA == u && e.NodeB == v) || (e.NodeB == u && e.NodeA == v));

            if (edge != null)
            {
                cost += edge.Length;
            }
            else
            {
                return float.MaxValue;
            }
        }
        
        return cost;
    }
}
