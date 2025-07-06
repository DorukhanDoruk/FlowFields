using System.Collections.Generic;
namespace Scripts.GridSystem
{
    public class FlowField
    {
        public readonly int GridSize;
        public readonly ushort[] CostField;
        public readonly int[] BestDirectionField;
        public readonly List<int> DirtyCells;

        public FlowField(int gridSize)
        {
            GridSize = gridSize;
            int cellCount = gridSize * gridSize;
            
            CostField = new ushort[cellCount];
            BestDirectionField = new int[cellCount];
            DirtyCells = new List<int>(cellCount / 4); 

            for (int i = 0; i < cellCount; i++)
            {
                CostField[i] = ushort.MaxValue;
                BestDirectionField[i] = i;
            }
        }
    }
}
