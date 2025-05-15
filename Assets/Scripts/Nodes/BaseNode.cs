using UnityEngine;

public class BaseNode : GraphNode
{
    [SerializeField] private float _resourceMultiplier = 1.0f;
    private float _currentResources = 0f;
    
    public float ResourceMultiplier => _resourceMultiplier;
    
    public void ReceiveResources(float amount)
    {
        if (amount <= 0) return;

        float multipliedAmount = amount * _resourceMultiplier;
        _currentResources += multipliedAmount;
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateTotalResources(multipliedAmount);
        }
    
        Debug.Log($"Received {amount} resources, multiplied to {multipliedAmount}. " +
                  $"Total at base: {_currentResources}");
    }
}
