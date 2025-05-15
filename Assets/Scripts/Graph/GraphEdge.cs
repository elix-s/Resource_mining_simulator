using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class GraphEdge : MonoBehaviour
{
    [Header("Edge Configuration")]
    [SerializeField] private float _length = 10f;
    public GraphNode NodeA;
    public GraphNode NodeB;
    
    private LineRenderer _lineRenderer;
    
    public float Length => _length;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.positionCount = 2;
    }

    private void Start()
    {
        DrawEdge();
        
        if (GameManager.Instance != null && !GameManager.Instance.Edges.Contains(this))
        {
            GameManager.Instance.Edges.Add(this);
        }
    }
    
    private void DrawEdge()
    {
        if (NodeA == null || NodeB == null || _lineRenderer == null)
        {
            return;
        }
        
        _lineRenderer.SetPosition(0, NodeA.transform.position);
        _lineRenderer.SetPosition(1, NodeB.transform.position);
    }
    
    private void OnValidate()
    {
        if (_lineRenderer == null) _lineRenderer = GetComponent<LineRenderer>();
        DrawEdge(); 
    }
}