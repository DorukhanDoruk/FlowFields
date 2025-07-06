using System;
namespace Scripts.GridSystem.Model
{
    public class PathNode : MinHeap.IHeapItem<PathNode>, IEquatable<PathNode>
    {
        public readonly GridCluster Cluster;
        public PathNode Parent;

        public float GCost;
        public float HCost;
        public float FCost;

        public int HeapIndex { get; set; }

        public PathNode(GridCluster cluster)
        {
            Cluster = cluster;
        }

        public bool Equals(PathNode other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Cluster, other.Cluster);
        }
        
        public int CompareTo(PathNode other)
        {
            int compare = FCost.CompareTo(other.FCost);
            if (compare != 0) return compare;

            compare = HCost.CompareTo(other.HCost);
            if (compare != 0) return compare;

            compare = Cluster.ClusterX.CompareTo(other.Cluster.ClusterX);
            if (compare != 0) return compare;

            return Cluster.ClusterZ.CompareTo(other.Cluster.ClusterZ);
        }

        public override bool Equals(object obj) => Equals(obj as PathNode);
        public override int GetHashCode() => Cluster.GetHashCode();
    }
}
