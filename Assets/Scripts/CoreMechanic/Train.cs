using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(SpriteRenderer))] [RequireComponent(typeof(LineRenderer))]
public class Train : MonoBehaviour
{
    private enum TrainState { Idle, MovingToMine, Mining, MovingToBase, Delivering }

    [Header("Train Parameters")]
    [SerializeField] private float _movementSpeed = 10f;
    [SerializeField] private float _baseMiningTimeSeconds = 5f;

    [Header("Path Visualization")]
    [SerializeField] private Material _pathLineMaterial;
    [SerializeField] private float _pathLineWidth = 0.2f;
    [SerializeField] private int _maxPathSegmentsToShow = 5;

    [Header("Visual Effects")]
    [SerializeField] private float _blinkInterval = 0.25f;
    
    private TrainState _currentState = TrainState.Idle;
    
    private float _currentCargo = 0f;
    private GraphNode _currentNode;
    private GraphNode _targetDestinationNode;
    private List<GraphNode> _currentPathNodes;
    private int _currentPathIndex;
    private Coroutine _activeCoroutine; 
    private MineNode _lastVisitedMine = null;
    private Coroutine _blinkingCoroutine = null; 

    private SpriteRenderer _spriteRenderer;
    private LineRenderer _pathLineRenderer;
    private Color _pathColor = Color.white;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _pathLineRenderer = GetComponent<LineRenderer>();
    }

    public void Initialize(GraphNode startNode, float speed, float miningTime, Color trainPathColor)
    {
        _currentNode = startNode;
        transform.position = startNode.transform.position;
        _movementSpeed = Mathf.Max(0.1f, speed);
        _baseMiningTimeSeconds = Mathf.Max(0.1f, miningTime);
        _pathColor = trainPathColor;
        _currentState = TrainState.Idle;
        _lastVisitedMine = null;

        InitializePathLineRenderer();
        ClearPathLine();
        StopBlinking(); 

        Debug.Log($"{gameObject.name} initialized, Speed: {speed}, MiningTime: {miningTime}");
        DecideNextAction();
    }

    private void InitializePathLineRenderer()
    {
        if (_pathLineRenderer == null) return;

        if (_pathLineMaterial != null)
        {
            _pathLineRenderer.material = _pathLineMaterial;
        }
        else
        {
            var defaultMaterial = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
            _pathLineRenderer.material = defaultMaterial;
            Debug.LogWarning($"Train {gameObject.name}: PathLineMaterial not assigned. Using default. Assign an Unlit/Transparent material.");
        }

        _pathLineRenderer.startColor = _pathColor;
        _pathLineRenderer.endColor = _pathColor;
        _pathLineRenderer.startWidth = _pathLineWidth;
        _pathLineRenderer.endWidth = _pathLineWidth;
        _pathLineRenderer.positionCount = 0;
    }

    private void Update()
    {
        if (_currentState == TrainState.Idle && _activeCoroutine == null)
        {
            if (_blinkingCoroutine != null) StopBlinking();
            DecideNextAction();
        }
        
        if (_currentState == TrainState.MovingToBase || _currentState == TrainState.MovingToMine)
        {
            UpdatePathLine();
        }
    }

    private void DecideNextAction()
    {
        if (_activeCoroutine != null)
        {
            StopCoroutine(_activeCoroutine);
            _activeCoroutine = null;
        }
        
        if (_currentCargo > 0)
        {
            // turn on the blinking effect if the train is carrying cargo
            if (_blinkingCoroutine == null) StartBlinking();

            _targetDestinationNode = FindBestBase();
            
            if (_targetDestinationNode != null)
            {
                StartMovingTo(_targetDestinationNode, TrainState.MovingToBase);
            }
            else
            {
                Debug.LogWarning($"{gameObject.name} has cargo but couldn't find a base. Idling.");
                _currentState = TrainState.Idle;
            }
        }
        else 
        {
            StopBlinking(); 
            _targetDestinationNode = FindBestMine();
            
            if (_targetDestinationNode != null)
            {
                StartMovingTo(_targetDestinationNode, TrainState.MovingToMine);
            }
            else
            {
                Debug.LogWarning($"{gameObject.name} needs resources but couldn't find a mine.");
                _currentState = TrainState.Idle;
            }
        }
    }

    //Determining the optimal base
    private GraphNode FindBestBase()
    {
        if (GameManager.Instance == null || GameManager.Instance.Bases.Count == 0) return null;
        
        BaseNode bestBase = null;
        float bestScore = float.MinValue;
        
        foreach (var baseNode in GameManager.Instance.Bases) 
        {
            float pathCost = Pathfinding.GetPathCost(_currentNode, baseNode);
            if (Mathf.Approximately(pathCost, float.MaxValue)) continue;
            
            float travelTime = pathCost / _movementSpeed;
            if (travelTime <= 0 && pathCost > 0) travelTime = 0.01f;
            else if (travelTime <= 0 && pathCost == 0) travelTime = 0;
            
            float score = (travelTime > 0) ? baseNode.ResourceMultiplier / travelTime : baseNode.ResourceMultiplier * 1000; // Приоритет, если уже на базе
            
            if (score > bestScore) 
            {
                bestScore = score;
                bestBase = baseNode;
            }
        }
        
        return bestBase;
    }

    //Determining the optimal mine
    private GraphNode FindBestMine()
    {
        if (GameManager.Instance == null || GameManager.Instance.Mines.Count == 0) return null;
        
        MineNode bestMine = null;
        float bestScore = float.MinValue;
        int availableMinesCount = 0;
        
        List<MineNode> potentialMines = new List<MineNode>(GameManager.Instance.Mines);
        
        foreach (var mineNode in potentialMines) 
        {
            if (mineNode == _lastVisitedMine && potentialMines.Count(m => m != _lastVisitedMine) > 0) {
                continue;
            }
            
            availableMinesCount++;
            
            float pathCost = Pathfinding.GetPathCost(_currentNode, mineNode);
            if (Mathf.Approximately(pathCost, float.MaxValue)) continue;
            
            float travelTime = pathCost / _movementSpeed;
            float miningDuration = mineNode.GetMiningDuration(_baseMiningTimeSeconds);
            float totalTime = travelTime + miningDuration;
            
            if (totalTime <= 0 && pathCost > 0) totalTime = 0.01f;
            else if (totalTime <= 0 && pathCost == 0) totalTime = miningDuration > 0 ? miningDuration : 0.01f;
            
            float score = (totalTime > 0) ? 1.0f / totalTime : float.MaxValue; 
            
            if (score > bestScore) 
            {
                bestScore = score;
                bestMine = mineNode;
            }
        }
        
        if (bestMine == null && _lastVisitedMine != null && availableMinesCount == 0 && 
            potentialMines.Contains(_lastVisitedMine)) 
        {
            float pathCost = Pathfinding.GetPathCost(_currentNode, _lastVisitedMine);
            
            if (!Mathf.Approximately(pathCost, float.MaxValue)) 
            { 
                bestMine = _lastVisitedMine;
            }
        }
        
        return bestMine;
    }

    private void StartMovingTo(GraphNode destination, TrainState stateAfterArrival)
    {
        ClearPathLine(); 
        
        if (destination == _currentNode) 
        {
            OnArrivedAtNode(stateAfterArrival);
            return;
        }
        
        _currentPathNodes = Pathfinding.FindPath(_currentNode, destination);
        
        if (_currentPathNodes != null && _currentPathNodes.Count > 1) 
        {
            _currentState = (stateAfterArrival == TrainState.MovingToMine) ? TrainState.MovingToMine : TrainState.MovingToBase;
            _currentPathIndex = 0;
            UpdatePathLine(); 
            _activeCoroutine = StartCoroutine(MoveAlongPathCoroutine(stateAfterArrival));
        } 
        else 
        {
            Debug.LogWarning($"{gameObject.name} could not find path.");
            _currentState = TrainState.Idle;
        }
    }

    private IEnumerator MoveAlongPathCoroutine(TrainState stateAfterArrivalAtFinalDestination)
    {
        while (_currentPathIndex < _currentPathNodes.Count - 1) 
        {
            GraphNode nextNodeInPathSegment = _currentPathNodes[_currentPathIndex + 1];
            
            GraphEdge edgeToTraverse = _currentNode.Edges.Find(e =>
                (e.NodeA == _currentNode && e.NodeB == nextNodeInPathSegment) ||
                (e.NodeB == _currentNode && e.NodeA == nextNodeInPathSegment));

            if (edgeToTraverse == null) 
            {
                Debug.LogError($"{gameObject.name} no available edge between nodes.");
                _currentState = TrainState.Idle;
                ClearPathLine();
                DecideNextAction();
                yield break;
            }

            Vector3 startPos = _currentNode.transform.position;
            Vector3 endPos = nextNodeInPathSegment.transform.position;
            float journeyLength = edgeToTraverse.Length;
            float startTime = Time.time;
            float distanceCovered = 0;

            while (distanceCovered < journeyLength) 
            {
                distanceCovered = (Time.time - startTime) * _movementSpeed;
                float fractionOfJourney = Mathf.Clamp01(distanceCovered / journeyLength);
                transform.position = Vector3.Lerp(startPos, endPos, fractionOfJourney);
                yield return null; 
            }
            
            transform.position = endPos;
            _currentNode = nextNodeInPathSegment;
            _currentPathIndex++;
        }
        
        OnArrivedAtNode(stateAfterArrivalAtFinalDestination);
    }

    private void OnArrivedAtNode(TrainState intendedNextStateBasedOnArrival)
    {
        ClearPathLine();
        
        if (_currentNode is MineNode && (intendedNextStateBasedOnArrival == TrainState.MovingToMine || intendedNextStateBasedOnArrival == TrainState.Mining)) 
        {
            _activeCoroutine = StartCoroutine(MineCoroutine(_currentNode as MineNode));
        } 
        else if (_currentNode is BaseNode && (intendedNextStateBasedOnArrival == TrainState.MovingToBase || intendedNextStateBasedOnArrival == TrainState.Delivering)) 
        {
            _activeCoroutine = StartCoroutine(DeliverCoroutine(_currentNode as BaseNode));
        } 
        else 
        {
            _currentState = TrainState.Idle;
            DecideNextAction();
        }
    }

    private IEnumerator MineCoroutine(MineNode mine)
    {
        _currentState = TrainState.Mining;
        ClearPathLine();
        
        float miningDuration = mine.GetMiningDuration(_baseMiningTimeSeconds);
        yield return new WaitForSeconds(miningDuration);
        _currentCargo = 1f;
        _lastVisitedMine = mine;
        
        StartBlinking(); 
        Debug.Log($"{gameObject.name} finished mining. Cargo: {_currentCargo}");
        DecideNextAction();
    }

    private IEnumerator DeliverCoroutine(BaseNode baseStation)
    {
        _currentState = TrainState.Delivering;
        ClearPathLine();
        StopBlinking();
        
        baseStation.ReceiveResources(_currentCargo);
        _currentCargo = 0f; 
        Debug.Log($"{gameObject.name} delivered to base.");

        if (GameManager.Instance.CanVisitIdenticalMines)
        {
            _lastVisitedMine = null; //if it is necessary to allow visiting one mine 2 times one after another
        }
        
        float deliveryPauseDuration = 1.0f;
        yield return new WaitForSeconds(deliveryPauseDuration);
        DecideNextAction();
    }

    private void StartBlinking()
    {
        if (_spriteRenderer == null) return;
        
        if (_blinkingCoroutine == null) 
        {
            _blinkingCoroutine = StartCoroutine(BlinkEffect());
        }
    }

    private void StopBlinking()
    {
        if (_spriteRenderer == null) return;
        
        if (_blinkingCoroutine != null)
        {
            StopCoroutine(_blinkingCoroutine);
            _blinkingCoroutine = null;
        }
        
        _spriteRenderer.enabled = true; 
    }

    private IEnumerator BlinkEffect()
    {
        if (_spriteRenderer == null) yield break;
        
        while (true)
        {
            _spriteRenderer.enabled = !_spriteRenderer.enabled;
            yield return new WaitForSeconds(_blinkInterval);
        }
    }

    private void UpdatePathLine()
    {
        if (_pathLineRenderer == null || _currentPathNodes == null || _currentPathNodes.Count <= 1) 
        {
            ClearPathLine();
            return;
        }
        
        List<Vector3> points = new List<Vector3>();
        points.Add(transform.position);
        
        int nodesAddedToLine = 0;
        
        for (int i = _currentPathIndex + 1; i < _currentPathNodes.Count && nodesAddedToLine < _maxPathSegmentsToShow; i++) 
        {
            if (_currentPathNodes[i] != null) 
            {
                points.Add(_currentPathNodes[i].transform.position);
                nodesAddedToLine++;
            }
        }
        
        if (points.Count > 1) 
        {
            _pathLineRenderer.positionCount = points.Count;
            _pathLineRenderer.SetPositions(points.ToArray());
        }
        else 
        {
            ClearPathLine();
        }
    }

    private void ClearPathLine()
    {
        if (_pathLineRenderer != null) 
        {
            _pathLineRenderer.positionCount = 0;
        }
    }
}