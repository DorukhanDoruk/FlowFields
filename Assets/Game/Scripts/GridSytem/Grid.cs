using UnityEngine;
namespace Scripts.GridSystem
{
    public class Grid
    {
        public readonly int GridSize;
        public readonly float CellSize;
        
        private readonly uint[] _obstacleBitmask;
        private readonly Vector3[] _worldPositions;

        public Grid(int gridSize, float cellSize)
        {
            GridSize = gridSize;
            CellSize = cellSize;
            _obstacleBitmask = new uint[(gridSize * gridSize + 31) / 32];
            _worldPositions = new Vector3[gridSize * gridSize];

            Vector3 worldOffset = new Vector3(-gridSize * cellSize * 0.5f, 0, -gridSize * cellSize * 0.5f);

            for (int x = 0; x < GridSize; x++)
            {
                for (int z = 0; z < GridSize; z++)
                {
                    int index = x * GridSize + z;
                    _worldPositions[index] = new Vector3(x * cellSize + cellSize * 0.5f, 0, z * cellSize + cellSize * 0.5f) + worldOffset;
                }
            }
        }

        public void SetObstacle(int x, int z, bool isObstacle)
        {
            int flatIndex = x * GridSize + z;
            int arrayIndex = flatIndex / 32;
            int bitIndex = flatIndex % 32;

            if (isObstacle)
                _obstacleBitmask[arrayIndex] |= (1u << bitIndex);
            else
                _obstacleBitmask[arrayIndex] &= ~(1u << bitIndex);
        }

        public bool IsObstacle(int x, int z)
        {
            int flatIndex = x * GridSize + z;
            int arrayIndex = flatIndex / 32;
            int bitIndex = flatIndex % 32;

            return (_obstacleBitmask[arrayIndex] & (1u << bitIndex)) != 0;
        }
        
        public Vector3 GetWorldPos(int x, int z) => _worldPositions[x * GridSize + z];

        public (int x, int z)? GetCoordsFromWorldPos(Vector3 worldPos)
        {
            int x = Mathf.FloorToInt((worldPos.x / CellSize) + GridSize * 0.5f);
            int z = Mathf.FloorToInt((worldPos.z / CellSize) + GridSize * 0.5f);

            if (x >= 0 && x < GridSize && z >= 0 && z < GridSize)
            {
                return (x, z);
            }
            return null;
        }
    }
}
