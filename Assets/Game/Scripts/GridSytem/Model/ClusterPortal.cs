using System.Collections.Generic;
namespace Scripts.GridSystem.Model
{
    public class ClusterPortal
    {
        public GridCluster Cluster1 { get; }
        public GridCluster Cluster2 { get; }
        public List<(int x, int z)> Nodes { get; }

        public ClusterPortal(GridCluster c1, GridCluster c2)
        {
            Cluster1 = c1;
            Cluster2 = c2;
            Nodes = new List<(int x, int z)>();
        }
    }
}
