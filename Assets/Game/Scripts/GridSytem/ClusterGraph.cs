using Scripts.GridSystem.Model;
namespace Scripts.GridSystem
{
    public class ClusterGraph
    {
        public GridCluster[,] Clusters { get; }
        public readonly int ClustersX;
        public readonly int ClustersZ;

        public ClusterGraph(int gridSize, int clusterSize)
        {
            ClustersX = gridSize / clusterSize;
            ClustersZ = gridSize / clusterSize;

            Clusters = new GridCluster[ClustersX, ClustersZ];
        }
    }
}
