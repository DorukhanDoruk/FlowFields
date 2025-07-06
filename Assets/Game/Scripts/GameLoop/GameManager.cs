using Scripts.Entity;
using Scripts.GameLoop.Scripts.GameLoop;
using Scripts.GridSystem;
using Scripts.GridSystem.Model;
using Scripts.Movement;
using Scripts.Rendering;
using UnityEngine;
namespace Scripts.GameLoop
{
    public class GameManager : MonoBehaviour
    {
        private GridManager _gridManager;
        private EntityManager _entityManager;
        private EntityPresenter _entityPresenter;
        private MovementSystem _movementSystem;
        private RenderSystem _renderSystem;
        private Archetype _targetableArchetype;

        private PlayerMovementController _playerController;
        
        private const int _gridSize = 1000;
        private const int _clusterSize = 20;

        private void Start()
        {
            Application.targetFrameRate = 60;
            _gridManager = new GridManager(_gridSize, 0.25f, _clusterSize);
            _entityPresenter = new EntityPresenter();
            _entityManager = new EntityManager();
            _movementSystem = new MovementSystem(_entityManager, _gridManager);
            _renderSystem = new RenderSystem(_entityManager, _entityPresenter);

            _gridManager.SetEssentialComponents(_entityManager);

            _playerController = gameObject.AddComponent<PlayerMovementController>();

            var player = _entityManager.CreateEntity();
            var playerStartPos = Vector3.zero;
            _entityManager.AddComponent(player, new CTransformComponent
            {
                Position = playerStartPos
            });
            _entityManager.AddComponent(player, new CRenderableComponent());

            _playerController.Initialize(_entityManager, player);

            for (int i = 0; i < 500; i++)
            {
                var enemy = _entityManager.CreateEntity();
                _entityManager.AddComponent(enemy, new CTransformComponent
                {
                    Position = new Vector3(Random.Range(-100, 100), 0, Random.Range(-100, 100))
                });
                _entityManager.AddComponent(enemy, new CTargetComponent
                {
                    TargetEntityId = player
                });
                _entityManager.AddComponent(enemy, new CRenderableComponent());
            }
        }

        private void Update()
        {
            var deltaTime = Time.deltaTime;

            if (_targetableArchetype == null)
                _targetableArchetype = _entityManager.GetArchetype(typeof(CTransformComponent), typeof(CTargetComponent), typeof(CRenderableComponent));

            if (_targetableArchetype != null && _targetableArchetype.Entities.Count > 0)
                _gridManager.Tick(_targetableArchetype.Entities);

            _movementSystem.Tick(deltaTime);
            _renderSystem.Tick(deltaTime);
        }

        private void OnDrawGizmos()
        {
            if (_gridManager == null || !Application.isPlaying || _entityManager == null)
            {
                return;
            }

            var grid = _gridManager.Grid;
            var clusterGraph = _gridManager.ClusterGraph;
            if (grid == null || clusterGraph == null) return;
            
            Gizmos.color = new Color(1f, 1f, 0f, 1f);
            for (int x = 0; x < clusterGraph.ClustersX; x++)
            {
                for (int z = 0; z < clusterGraph.ClustersZ; z++)
                {
                    var cluster = clusterGraph.Clusters[x, z];
                    Vector3 center = grid.GetWorldPos(cluster.StartX + _clusterSize / 2, cluster.StartZ + _clusterSize / 2);
                    var size = new Vector3(_clusterSize * grid.CellSize, 0.5f, _clusterSize * grid.CellSize);
                    Gizmos.DrawWireCube(center, size);
                    
                    continue;
                    for (int cellX = 0; cellX < _clusterSize; cellX++)
                    {
                        for (int cellZ = 0; cellZ < _clusterSize; cellZ++)
                        {
                            int cellIndex = cellZ * _clusterSize + cellX;
                            var index = 0;
                            foreach (var clusterLocalFlowField in cluster.LocalFlowFields)
                            {
                                if (clusterLocalFlowField.Value.CostField.Length > cellIndex)
                                {
                                    var cost = clusterLocalFlowField.Value.CostField[cellIndex];

                                    int flatIndex = cellZ * _clusterSize + cellX;
                                    int directionFlatIndex = clusterLocalFlowField.Value.BestDirectionField[flatIndex];
                                    
                                    int dirLocalZ = directionFlatIndex / _clusterSize;
                                    int dirLocalX = directionFlatIndex % _clusterSize;

                                    Vector3 startWorldPos = grid.GetWorldPos(cluster.StartX + cellX, cluster.StartZ + cellZ);
                                    Vector3 dirWorldPos = grid.GetWorldPos(cluster.StartX + dirLocalX, cluster.StartZ + dirLocalZ);
                                    
                                    Gizmos.color = Color.Lerp(Color.green, Color.red, Mathf.InverseLerp(0, _clusterSize, cost));
                                    Gizmos.DrawLine(startWorldPos + Vector3.up * index++, dirWorldPos + Vector3.up * index++);
                                }
                            }
                        }
                    }
                }
            }
            
            if (_targetableArchetype == null) return;

            Color[] pathColors =
            {
                new Color(0.8f, 0.7f, 0.9f, 0.1f), // pastel purple
                new Color(0.7f, 0.9f, 0.8f, 0.1f), // pastel teal
                new Color(0.9f, 0.8f, 0.7f, 0.1f), // pastel peach
                new Color(0.8f, 0.9f, 0.7f, 0.1f)  // pastel green
            };
            int colorIndex = 0;

            foreach (var entityId in _targetableArchetype.Entities)
            {
                var path = _gridManager.GetEntityPathForGizmos(entityId);
                if (path != null && path.Count > 0)
                {
                    Gizmos.color = pathColors[colorIndex % pathColors.Length];

                    var entityTransform = _entityManager.GetComponent<CTransformComponent>(entityId);
                    var startCluster = grid.GetClusterFromWorldPos(new Vector3(entityTransform.Position.x, 0, entityTransform.Position.z), _gridManager.ClusterGraph);

                    if (startCluster != null)
                    {
                        Vector3 previousClusterCenter = GetClusterCenter(startCluster);

                        foreach (var cluster in path)
                        {
                            Vector3 currentClusterCenter = GetClusterCenter(cluster);
                            Gizmos.DrawLine(previousClusterCenter, currentClusterCenter);
                            previousClusterCenter = currentClusterCenter;
                        }
                    }
                }

                var entityCurrentTransform = _entityManager.GetComponent<CTransformComponent>(entityId);
                var playerTransform = _entityManager.GetComponent<CTransformComponent>(new InstanceId(1));

                Vector3 entityPos = new Vector3(entityCurrentTransform.Position.x, 1f, entityCurrentTransform.Position.z);
                Vector3 playerPos = new Vector3(playerTransform.Position.x, 1f, playerTransform.Position.z);

                Vector3 flowDirection = _gridManager.GetFlowDirection(entityId, entityPos, playerPos);

                if (flowDirection != Vector3.zero)
                {
                    Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
                    Gizmos.DrawRay(entityPos, flowDirection * 5f);
                }

                colorIndex++;
            }
        }

        private Vector3 GetClusterCenter(GridCluster cluster)
        {
            return _gridManager.Grid.GetWorldPos(cluster.StartX + _clusterSize / 2, cluster.StartZ + _clusterSize / 2);
        }
    }
}
