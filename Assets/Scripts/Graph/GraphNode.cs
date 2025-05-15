using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(SpriteRenderer))]
public abstract class GraphNode : MonoBehaviour
{
    [Header("Connections")]
    public List<GraphEdge> Edges = new List<GraphEdge>(); 
}

