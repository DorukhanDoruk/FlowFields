using Scripts.GridSystem.Model;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace Scripts.GridSystem
{
    public class PathFinder
    {
        private readonly PathNode[] _allNodes;
        private readonly int _clustersZ;
        private readonly MinHeap _openSet;
        private readonly HashSet<GridCluster> _closedSet = new HashSet<GridCluster>();
        private readonly Stack<GridCluster> _reusablePathStack = new Stack<GridCluster>();

        public PathFinder(ClusterGraph clusterGraph)
        {
            _clustersZ = clusterGraph.ClustersZ;
            int clusterCount = clusterGraph.ClustersX * clusterGraph.ClustersZ;
            _allNodes = new PathNode[clusterCount];
            _openSet = new MinHeap(clusterCount);

            for (int x = 0; x < clusterGraph.ClustersX; x++)
            {
                for (int z = 0; z < clusterGraph.ClustersZ; z++)
                {
                    var cluster = clusterGraph.Clusters[x, z];
                    if (cluster != null)
                    {
                        int flatIndex = x * _clustersZ + z;
                        _allNodes[flatIndex] = new PathNode(cluster);
                    }
                }
            }
        }

        private PathNode GetNode(GridCluster cluster)
        {
            return _allNodes[cluster.ClusterX * _clustersZ + cluster.ClusterZ];
        }

        public bool FindClusterPath(GridCluster startCluster, GridCluster targetCluster, Queue<GridCluster> outputPathQueue)
        {
            using (new ProfilerMarker(nameof(FindClusterPath)).Auto())
            {
                for (int i = 0; i < _allNodes.Length; i++)
                {
                    var node = _allNodes[i];
                    if (node != null)
                    {
                        node.Parent = null;
                        node.GCost = float.MaxValue;
                        node.HCost = 0;
                        node.FCost = node.GCost + node.HCost;
                    }
                }

                _openSet.Clear();
                _closedSet.Clear();

                var startNode = GetNode(startCluster);
                startNode.GCost = 0;
                startNode.HCost = GetOctileDistance(startCluster.Center, targetCluster.Center);
                startNode.FCost = startNode.GCost + startNode.HCost;
                _openSet.Add(startNode);

                while (_openSet.Count > 0)
                {
                    var currentNode = _openSet.RemoveFirst();
                    _closedSet.Add(currentNode.Cluster);

                    if (currentNode.Cluster.Equals(targetCluster))
                    {
                        ReconstructPath(currentNode, outputPathQueue);
                        return true;
                    }

                    var portals = currentNode.Cluster.Portals;
                    for (int i = 0; i < portals.Count; i++)
                    {
                        var portal = portals[i];
                        var neighbourCluster = (portal.Cluster1.Equals(currentNode.Cluster)) ? portal.Cluster2 : portal.Cluster1;
                        if (_closedSet.Contains(neighbourCluster)) continue;

                        var neighbourNode = GetNode(neighbourCluster);
                        float newGCost = currentNode.GCost + GetOctileDistance(currentNode.Cluster.Center, neighbourCluster.Center);

                        bool isInOpenSet = _openSet.Contains(neighbourNode);
                        if (newGCost < neighbourNode.GCost || !isInOpenSet)
                        {
                            neighbourNode.GCost = newGCost;
                            neighbourNode.HCost = GetOctileDistance(neighbourCluster.Center, targetCluster.Center);
                            neighbourNode.Parent = currentNode;
                            neighbourNode.FCost = neighbourNode.GCost + neighbourNode.HCost;

                            if (!isInOpenSet)
                                _openSet.Add(neighbourNode);
                            else
                                _openSet.UpdateItem(neighbourNode);
                        }
                    }
                }

                outputPathQueue.Clear();
                return false;
            }
        }
        
        private float GetOctileDistance(Vector3 a, Vector3 b)
        {
            var dx = Mathf.Abs(a.x - b.x);
            var dz = Mathf.Abs(a.z - b.z);

            const float d = 1f;
            const float d2 = 1.414f;

            return d * (dx + dz) + (d2 - 2 * d) * Mathf.Min(dx, dz);
        }


        private void ReconstructPath(PathNode endNode, Queue<GridCluster> outputPathQueue)
        {
            using (new ProfilerMarker(nameof(ReconstructPath)).Auto())
            {
                _reusablePathStack.Clear();
                outputPathQueue.Clear();

                using (new ProfilerMarker("ReconstructPath.ClearOutputQueue").Auto())
                {
                    var currentNode = endNode;
                    while (currentNode != null && currentNode.Parent != null)
                    {
                        _reusablePathStack.Push(currentNode.Cluster);
                        currentNode = currentNode.Parent;
                    }
                }

                using (new ProfilerMarker("ReconstructPath.PushToQueue").Auto())
                {
                    while (_reusablePathStack.Count > 0)
                        outputPathQueue.Enqueue(_reusablePathStack.Pop());
                }
            }
        }
    }
}
