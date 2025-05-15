using UnityEngine;

public class MineNode : GraphNode
{
    [SerializeField] private float _miningTimeMultiplier = 1.0f;
    
    public float GetMiningDuration(float baseTrainMiningTime)
    {
        return baseTrainMiningTime * _miningTimeMultiplier;
    }
}