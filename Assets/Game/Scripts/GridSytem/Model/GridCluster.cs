using System;
using System.Collections.Generic;
using UnityEngine;
namespace Scripts.GridSystem.Model
{
    public class GridCluster : IEquatable<GridCluster>
    {
        public readonly int ClusterX;
        public readonly int ClusterZ;
        public readonly Vector3 Center;
        
        public int StartX { get; }
        public int StartZ { get; }
        public List<ClusterPortal> Portals { get; }
        public Dictionary<int, FlowField> LocalFlowFields { get; }

        public GridCluster(int clusterX, int clusterZ, int startX, int startZ, int clusterSize)
        {
            ClusterX = clusterX;
            ClusterZ = clusterZ;
            StartX = startX;
            StartZ = startZ;
            Portals = new List<ClusterPortal>();
            LocalFlowFields = new Dictionary<int, FlowField>();
            Center = new Vector3(startX + clusterSize / 2f, 0, startZ + clusterSize / 2f);
        }
        
        public bool Equals(GridCluster other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return ClusterX == other.ClusterX && ClusterZ == other.ClusterZ;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((GridCluster) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (ClusterX * 397) ^ ClusterZ;
            }
        }
    }
}
