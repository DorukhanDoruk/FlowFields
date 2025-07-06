using Scripts.Entity;
using Scripts.GridSystem.Model;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using Object = System.Object;

namespace Scripts.GridSystem
{
    public class GridManager
    {
        public Grid Grid => _grid;
        public ClusterGraph ClusterGraph => _clusterGraph;

        private readonly int _clusterSize;
        private readonly Grid _grid;
        private readonly ClusterGraph _clusterGraph;
        private readonly PathFinder _pathfinder;
        private readonly Dictionary<InstanceId, Queue<GridCluster>> _entityPaths = new Dictionary<InstanceId, Queue<GridCluster>>();
        private readonly Dictionary<InstanceId, GridCluster> _lastTargetClusters = new Dictionary<InstanceId, GridCluster>();
        private readonly Queue<(int x, int z)> _localBfsQueue = new Queue<(int x, int z)>();
        private readonly HashSet<GridCluster> _activeClustersForGizmos = new HashSet<GridCluster>();

        private EntityManager _entityManager;

        public GridManager(int gridSize, float cellSize, int clusterSize)
        {
            _clusterSize = clusterSize;
            _grid = new Grid(gridSize, cellSize, _clusterSize);
            _clusterGraph = new ClusterGraph(gridSize, _clusterSize);

            BuildClusterGraph();
            foreach (var cluster in _clusterGraph.Clusters)
                UpdateLocalFlowFields(cluster);
            
            _pathfinder = new PathFinder(_clusterGraph);
        }

        public void Tick(List<InstanceId> entitiesWithTarget)
        {
            _activeClustersForGizmos.Clear();
            
            var playerTransform = _entityManager.GetComponent<CTransformComponent>(new InstanceId(1));
            var playerCluster = _grid.GetClusterFromWorldPos(new Vector3(playerTransform.Position.x, 0, playerTransform.Position.z), _clusterGraph);
            if (playerCluster == null) return;
            
            _activeClustersForGizmos.Add(playerCluster);
            UpdateEntityPathsAndPopulateActiveClusters(entitiesWithTarget, playerCluster, _activeClustersForGizmos);

        }

        private void UpdateEntityPathsAndPopulateActiveClusters(IEnumerable<InstanceId> entities, GridCluster playerCluster, HashSet<GridCluster> activeClusters)
        {
            using (new ProfilerMarker(nameof(UpdateEntityPathsAndPopulateActiveClusters)).Auto())
            {
                foreach (var entityId in entities)
                {
                    var entityTransform = _entityManager.GetComponent<CTransformComponent>(entityId);
                    var startCluster = _grid.GetClusterFromWorldPos(new Vector3(entityTransform.Position.x, 0, entityTransform.Position.z), _clusterGraph);

                    if (startCluster == null) 
                        continue;

                    activeClusters.Add(startCluster);

                    bool needsNewPath = !_lastTargetClusters.ContainsKey(entityId) || !_lastTargetClusters[entityId].Equals(playerCluster);

                    if (needsNewPath)
                    {
                        if (!_entityPaths.TryGetValue(entityId, out var entityPathQueue))
                        {
                            entityPathQueue = new Queue<GridCluster>();
                            _entityPaths[entityId] = entityPathQueue;
                        }
            
                        if (startCluster.Equals(playerCluster))
                        {
                            entityPathQueue.Clear();
                        }
                        else
                        {
                            _pathfinder.FindClusterPath(startCluster, playerCluster, entityPathQueue);
                        }
                        _lastTargetClusters[entityId] = playerCluster;
                    }

                    if (_entityPaths.TryGetValue(entityId, out var path) && path.Count > 0)
                    {
                        var nextClusterInPath = path.Peek();
                        if (startCluster.Equals(nextClusterInPath))
                        {
                            path.Dequeue();
                        }
                    }
                }
            }
        }

        private void UpdateLocalFlowFields(GridCluster cluster)
        {
            using (new ProfilerMarker(nameof(UpdateLocalFlowFields)).Auto())
            {
                foreach (var portal in cluster.Portals)
                {
                    if (!cluster.LocalFlowFields.ContainsKey(portal.GetHashCode()))
                    {
                        var flowField = new FlowField(_clusterSize);
                        _localBfsQueue.Clear();

                        foreach (var node in portal.Nodes)
                        {
                            if (node.x >= cluster.StartX && node.x < cluster.StartX + _clusterSize &&
                                node.z >= cluster.StartZ && node.z < cluster.StartZ + _clusterSize)
                            {
                                int localX = node.x - cluster.StartX;
                                int localZ = node.z - cluster.StartZ;
                                int flatIndex = localZ * _clusterSize + localX;

                                if (flowField.CostField[flatIndex] == ushort.MaxValue)
                                {
                                    flowField.CostField[flatIndex] = 0;
                                    flowField.BestDirectionField[flatIndex] = flatIndex;
                                    _localBfsQueue.Enqueue((localX, localZ));
                                }
                            }
                        }

                        if (_localBfsQueue.Count > 0)
                        {
                            RunLocalBfs(cluster, flowField);
                        }
                        cluster.LocalFlowFields[portal.GetHashCode()] = flowField;
                    }
                }
            }
        }

        private void RunLocalBfs(GridCluster cluster, FlowField flowField)
        {
            using (new ProfilerMarker(nameof(RunLocalBfs)).Auto())
            {
                while (_localBfsQueue.Count > 0)
                {
                    var currentLocal = _localBfsQueue.Dequeue();
                    int currentFlatIndex = currentLocal.z * _clusterSize + currentLocal.x;
                    ushort currentCost = flowField.CostField[currentFlatIndex];

                    for (int x = -1; x <= 1; x++)
                    {
                        for (int z = -1; z <= 1; z++)
                        {
                            if (x == 0 && z == 0) 
                                continue;
                            int localNx = currentLocal.x + x;
                            int localNz = currentLocal.z + z;

                            if (localNx < 0 || localNx >= _clusterSize || localNz < 0 || localNz >= _clusterSize)
                                continue;

                            int globalNx = cluster.StartX + localNx;
                            int globalNz = cluster.StartZ + localNz;

                            if (_grid.IsObstacle(globalNx, globalNz)) 
                                continue;

                            int neighbourFlatIndex = localNz * _clusterSize + localNx;
                            if (flowField.CostField[neighbourFlatIndex] == ushort.MaxValue)
                            {
                                flowField.CostField[neighbourFlatIndex] = (ushort)(currentCost + 1);
                                flowField.BestDirectionField[neighbourFlatIndex] = currentFlatIndex;
                                _localBfsQueue.Enqueue((localNx, localNz));
                            }
                        }
                    }
                }
            }
        }

        public Vector3 GetFlowDirection(InstanceId entityId, Vector3 currentPosition, Vector3 targetPosition)
        {
            using (new ProfilerMarker(nameof(GetFlowDirection)).Auto())
            {
                var currentCluster = _grid.GetClusterFromWorldPos(currentPosition, _clusterGraph);
                if (currentCluster == null || !_entityPaths.TryGetValue(entityId, out var path) || path == null || path.Count == 0)
                {
                    return Vector3.Normalize(targetPosition - currentPosition);
                }

                var nextClusterInPath = path.Peek();
                ClusterPortal targetPortal = FindPortal(currentCluster, nextClusterInPath);

                if (targetPortal == null)
                {
                    if (_entityPaths.TryGetValue(entityId, out var path2))
                        path2.Clear(); 
                    return Vector3.Normalize(targetPosition - currentPosition);
                }

                Vector3 bestPortalNodePosition = Vector3.zero;
                float minPathLength = float.MaxValue;

                if (targetPortal.Nodes.Count > 0)
                {
                    foreach (var nodeCoords in targetPortal.Nodes)
                    {
                        Vector3 nodePos = _grid.GetWorldPos(nodeCoords.x, nodeCoords.z);

                        float pathLength = (nodePos - currentPosition).sqrMagnitude + (targetPosition - nodePos).sqrMagnitude;

                        if (pathLength < minPathLength)
                        {
                            minPathLength = pathLength;
                            bestPortalNodePosition = nodePos;
                        }
                    }
                    return Vector3.Normalize(bestPortalNodePosition - currentPosition);
                }

                return Vector3.Normalize(GetPortalCenter(targetPortal) - currentPosition);
            }
        }

        public void SetEssentialComponents(EntityManager entityManager)
        {
            _entityManager = entityManager;
        }

        private enum Direction
        {
            Invalid = 0,
            Up = 1, Right = 4,
            UpLeft = 5, DownLeft = 7,
        }

        private void BuildClusterGraph()
        {
            using (new ProfilerMarker(nameof(BuildClusterGraph)).Auto())
            {
                for (int x = 0; x < _clusterGraph.ClustersX; x++)
                {
                    for (int z = 0; z < _clusterGraph.ClustersZ; z++)
                    {
                        _clusterGraph.Clusters[x, z] = new GridCluster(x, z, x * _clusterSize, z * _clusterSize, _clusterSize);
                    }
                }

                for (int x = 0; x < _clusterGraph.ClustersX; x++)
                {
                    for (int z = 0; z < _clusterGraph.ClustersZ; z++)
                    {
                        var cluster1 = _clusterGraph.Clusters[x, z];

                        if (x < _clusterGraph.ClustersX - 1)
                        {
                            var cluster2 = _clusterGraph.Clusters[x + 1, z];
                            CreatePortalBetween(cluster1, cluster2, Direction.Right);
                        }
                        if (z > 0)
                        {
                            var cluster3 = _clusterGraph.Clusters[x, z - 1];
                            CreatePortalBetween(cluster1, cluster3, Direction.Up);
                        }
                        if (x > 0 && z < _clusterGraph.ClustersZ - 1)
                        {
                            var clusterDiag2 = _clusterGraph.Clusters[x - 1, z + 1];
                            CreatePortalBetween(cluster1, clusterDiag2, Direction.DownLeft);
                        }
                        if (x > 0 && z > 0)
                        {
                            var clusterDiag2 = _clusterGraph.Clusters[x - 1, z - 1];
                            CreatePortalBetween(cluster1, clusterDiag2, Direction.UpLeft);
                        }
                    }
                }
            }
        }

        private void CreatePortalBetween(GridCluster c1, GridCluster c2, Direction direction)
        {
            using (new ProfilerMarker(nameof(CreatePortalBetween)).Auto())
            {
                var portal = new ClusterPortal(c1, c2);

                switch (direction)
                {
                    case Direction.Right:
                        {
                            int portalEdgeX = c2.StartX;
                            for (int pz = c1.StartZ; pz < c1.StartZ + _clusterSize; pz++)
                            {
                                if (!_grid.IsObstacle(portalEdgeX - 1, pz) && !_grid.IsObstacle(portalEdgeX, pz))
                                {
                                    portal.Nodes.Add((portalEdgeX - 1, pz));
                                    portal.Nodes.Add((portalEdgeX, pz));
                                }
                            }
                            break;
                        }
                    case Direction.Up:
                        {
                            int portalEdgeZ = c2.StartZ + _clusterSize - 1;
                            for (int px = c1.StartX; px < c1.StartX + _clusterSize; px++)
                            {
                                if (!_grid.IsObstacle(px, portalEdgeZ) && !_grid.IsObstacle(px, portalEdgeZ + 1))
                                {
                                    portal.Nodes.Add((px, portalEdgeZ));
                                    portal.Nodes.Add((px, portalEdgeZ + 1));
                                }
                            }
                            break;
                        }
                    case Direction.UpLeft:
                        {
                            int portalEdgeX = c2.StartX + _clusterSize - 1;
                            int portalEdgeZ = c2.StartZ + _clusterSize - 1;
                            if (!_grid.IsObstacle(portalEdgeX, portalEdgeZ) && !_grid.IsObstacle(portalEdgeX + 1, portalEdgeZ + 1))
                            {
                                portal.Nodes.Add((portalEdgeX, portalEdgeZ));
                                portal.Nodes.Add((portalEdgeX + 1, portalEdgeZ + 1));
                            }
                            break;
                        }
                    case Direction.DownLeft:
                        {
                            int portalEdgeX = c2.StartX + _clusterSize - 1;
                            int portalEdgeZ = c2.StartZ;
                            if (!_grid.IsObstacle(portalEdgeX, portalEdgeZ) && !_grid.IsObstacle(portalEdgeX + 1, portalEdgeZ - 1))
                            {
                                portal.Nodes.Add((portalEdgeX, portalEdgeZ));
                                portal.Nodes.Add((portalEdgeX + 1, portalEdgeZ - 1));
                            }
                            break;
                        }
                }

                if (portal.Nodes.Count > 0)
                {
                    c1.Portals.Add(portal);
                    c2.Portals.Add(portal);
                }
            }
        }

        private ClusterPortal FindPortal(GridCluster from, GridCluster to)
        {
            using (new ProfilerMarker(nameof(FindPortal)).Auto())
            {
                foreach (var portal in from.Portals)
                {
                    if ((portal.Cluster1.Equals(from) && portal.Cluster2.Equals(to)) || (portal.Cluster2.Equals(from) && portal.Cluster1.Equals(to)))
                    {
                        return portal;
                    }
                }
                return null;
            }
        }

        private Vector3 GetPortalCenter(ClusterPortal portal)
        {
            using (new ProfilerMarker(nameof(GetPortalCenter)).Auto())
            {
                if (portal == null || portal.Nodes.Count == 0) return Vector3.zero;

                Vector3 center = Vector3.zero;
                foreach (var nodeCoords in portal.Nodes)
                {
                    center += _grid.GetWorldPos(nodeCoords.x, nodeCoords.z);
                }

                return center / portal.Nodes.Count;
            }
        }

        public Queue<GridCluster> GetEntityPathForGizmos(InstanceId entityId)
        {
            _entityPaths.TryGetValue(entityId, out var path);
            return path;
        }
    }
}
