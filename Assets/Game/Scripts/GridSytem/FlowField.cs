namespace Scripts.GridSystem
{
    public class FlowField
    {
        public readonly ushort[] CostField;
        public readonly int[] BestDirectionField;

        public FlowField(int gridSize)
        {
            int cellCount = gridSize * gridSize;
            
            CostField = new ushort[cellCount];
            BestDirectionField = new int[cellCount];

            for (int i = 0; i < cellCount; i++)
            {
                CostField[i] = ushort.MaxValue;
                BestDirectionField[i] = i;
            }
        }
    }
}
