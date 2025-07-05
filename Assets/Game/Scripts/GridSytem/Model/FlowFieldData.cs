using UnityEngine;
namespace Scripts.GridSystem.Model
{
    public class FlowFieldData
    {
        public readonly FlowField Field;
        public Vector3 TargetPosition;

        public FlowFieldData(FlowField field, Vector3 targetPosition)
        {
            Field = field;
            TargetPosition = targetPosition;
        }
        
        public void SetTargetPosition(Vector3 targetPosition)
        {
            TargetPosition = targetPosition;
        }
    }
}
