using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Graph Elements")]
    public List<BaseNode> Bases = new List<BaseNode>();
    public List<MineNode> Mines = new List<MineNode>();
    public List<WaypointNode> Waypoints = new List<WaypointNode>();
    public List<GraphEdge> Edges = new List<GraphEdge>();

    [Header("Train Settings")]
    [SerializeField] private GameObject _trainPrefab;
    [SerializeField] private Transform _trainsParent;
    [SerializeField] private int _numberOfTrainsToSpawn = 3;
    [SerializeField] private bool _canVisitIdenticalMines = false; //is it possible to visit 2 identical mines in a row or not

    [Header("Train Specific Configurations")]
    [SerializeField] private float _train1Speed = 200f;
    [SerializeField] private float _train1BaseMiningTime = 20f;
    [SerializeField] private float _train2Speed = 5f;
    [SerializeField] private float _train2BaseMiningTime = 1f;
    [SerializeField] private float _train3Speed = 10f;
    [SerializeField] private float _train3BaseMiningTime = 10f;

    [Header("Train Visuals")]
    [SerializeField] private Color[] _trainPathColors = new Color[] { Color.cyan, new Color(0,1,0,1), Color.yellow, Color.magenta, Color.red, Color.blue }; // Явный зеленый

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI _totalResourcesText;
    
    private List<Train> _trains = new List<Train>();
    private float _totalResources = 0f;
    
    public bool CanVisitIdenticalMines => _canVisitIdenticalMines;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        //search for nodes if they are not assigned in the inspector
        if (Bases.Count == 0) Bases.AddRange(FindObjectsOfType<BaseNode>());
        if (Mines.Count == 0) Mines.AddRange(FindObjectsOfType<MineNode>());
        if (Waypoints.Count == 0) Waypoints.AddRange(FindObjectsOfType<WaypointNode>());
        if (Edges.Count == 0) Edges.AddRange(FindObjectsOfType<GraphEdge>());

        Bases = Bases.Distinct().ToList();
        Mines = Mines.Distinct().ToList();
        Waypoints = Waypoints.Distinct().ToList();
        Edges = Edges.Distinct().ToList();

        if (Bases.Count == 0 && Mines.Count == 0)
        {
            Debug.LogError("No bases or mines found.");
        }
    }

    private void Start()
    {
        InitializeTrains();
        UpdateResourceDisplay();
    }

    private void InitializeTrains()
    {
        if (_trainPrefab == null) { Debug.LogError("Train prefab not assigned."); return; }

        List<GraphNode> allSpawnNodes = new List<GraphNode>();
        allSpawnNodes.AddRange(Bases);
        allSpawnNodes.AddRange(Mines);
        allSpawnNodes.AddRange(Waypoints);
        allSpawnNodes = allSpawnNodes.Distinct().ToList();
        
        if (allSpawnNodes.Count == 0) { Debug.LogError("No nodes available."); return; }

        foreach (var train in _trains)
        {
            if (train != null) Destroy(train.gameObject);
        }
        
        _trains.Clear();

        if (_numberOfTrainsToSpawn >= 1)
        {
            Color color = (_trainPathColors.Length > 0) ? _trainPathColors[0 % _trainPathColors.Length] : 
                Color.white;
            SpawnTrain("Train 1", _train1Speed, _train1BaseMiningTime, allSpawnNodes, color);
        }
        
        if (_numberOfTrainsToSpawn >= 2)
        {
            Color color = (_trainPathColors.Length > 0) ? _trainPathColors[1 % _trainPathColors.Length] : 
                Color.white;
            SpawnTrain("Train 2", _train2Speed, _train2BaseMiningTime, allSpawnNodes, color);
        }
        
        if (_numberOfTrainsToSpawn >= 3)
        {
            Color color = (_trainPathColors.Length > 0) ? _trainPathColors[2 % _trainPathColors.Length] : 
                Color.white;
            SpawnTrain("Train 3", _train3Speed, _train3BaseMiningTime, allSpawnNodes, color);
        }
        
        for (int i = 3; i < _numberOfTrainsToSpawn; i++)
        {
            Color color = (_trainPathColors.Length > 0) ? _trainPathColors[i % _trainPathColors.Length] : 
                Color.white;
            SpawnTrain($"Train {i + 1}", _train3Speed, _train3BaseMiningTime, allSpawnNodes, color);
        }
    }

    private void SpawnTrain(string trainName, float speed, float miningTime, List<GraphNode> spawnNodes, Color pathLineColor)
    {
        if (spawnNodes.Count == 0) return;
        
        GraphNode startNode = spawnNodes[Random.Range(0, spawnNodes.Count)];
        GameObject trainObj = Instantiate(_trainPrefab, startNode.transform.position, Quaternion.identity, _trainsParent);
        trainObj.name = trainName;
        Train trainComponent = trainObj.GetComponent<Train>();

        if (trainComponent != null)
        {
            trainComponent.Initialize(startNode, speed, miningTime, pathLineColor);
            _trains.Add(trainComponent);
        }
        else
        {
            Debug.LogError($"Train Prefab missing Train component.");
            Destroy(trainObj);
        }
    }

    public void UpdateTotalResources(float amountGained)
    {
        _totalResources += amountGained;
        UpdateResourceDisplay();
    }

    private void UpdateResourceDisplay()
    {
        if (_totalResourcesText != null)
        {
            _totalResourcesText.text = "Total Resources: " + _totalResources.ToString("F0");
        }
    }
}