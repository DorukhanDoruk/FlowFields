namespace Scripts.GridSystem
{
    public class FlowField
    {
        public (int x, int z)[,] BestDirectionField { get; private set; }
        public readonly ushort[,] CostField;

        private readonly int _gridSize;

        public FlowField(int gridSize)
        {
            _gridSize = gridSize;
            BestDirectionField = new (int x, int z)[_gridSize, _gridSize];
            CostField = new ushort[_gridSize, _gridSize];
        }

        public void Reset()
        {
            for (int x = 0; x < _gridSize; x++)
            {
                for (int z = 0; z < _gridSize; z++)
                {
                    CostField[x, z] = ushort.MaxValue;
                    BestDirectionField[x, z] = (x, z);
                }
            }
        }
    }
}
