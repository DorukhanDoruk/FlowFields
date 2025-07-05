using Scripts.Entity;
using Scripts.GridSystem.Model;
using System.Collections.Generic;
using UnityEngine;

namespace Scripts.GridSystem
{
    public class GridManager
    {
        public Grid Grid => _grid;
        private readonly Grid _grid;
        private readonly Dictionary<InstanceId, FlowFieldData> _flowFields = new();
        private const float _recalculateDistanceThreshold = 2.0f;
        private readonly Queue<(int x, int z)> _cellQueue;
        private readonly ushort[,] _staticCostField;

        private bool _isStaticCostFieldDirty = true;
        private EntityManager _entityManager;

        public GridManager(int gridSize, float cellSize)
        {
            _grid = new Grid(gridSize, cellSize);
            _cellQueue = new Queue<(int x, int z)>(gridSize);
            _staticCostField = new ushort[gridSize, gridSize];
        }

        public void Tick(float deltaTime)
        {
            if (_isStaticCostFieldDirty)
            {
                CreateStaticCostField();
                _isStaticCostFieldDirty = false;
            }

            foreach (var (targetId, fieldData) in _flowFields)
            {
                if (!_entityManager.HasComponent<CTransformComponent>(targetId)) continue;
                ref var targetTransform = ref _entityManager.GetComponent<CTransformComponent>(targetId);
                Vector3 targetPosition = targetTransform.Position;

                float distanceSq = (targetPosition - fieldData.TargetPosition).sqrMagnitude;
                float thresholdSq = _recalculateDistanceThreshold * _grid.CellSize * (_recalculateDistanceThreshold * _grid.CellSize);

                if (distanceSq > thresholdSq)
                {
                    CreateFlowField(targetPosition, fieldData.Field);
                    fieldData.SetTargetPosition(targetPosition);
                }
            }
        }

        public void SetEssentialComponents(EntityManager entityManager)
        {
            _entityManager = entityManager;
        }

        public void SetObstacle(int x, int z, bool isObstacle)
        {
            if (_grid.IsObstacle(x, z) != isObstacle)
            {
                _grid.SetObstacle(x, z, isObstacle);
                _isStaticCostFieldDirty = true;
            }
        }

        private void CreateStaticCostField()
        {
            for (int x = 0; x < _grid.GridSize; x++)
            {
                for (int z = 0; z < _grid.GridSize; z++)
                {
                    if (_grid.IsObstacle(x, z))
                    {
                        _staticCostField[x, z] = ushort.MaxValue;
                    }
                    else
                    {
                        _staticCostField[x, z] = 1;
                    }
                }
            }
        }

        public FlowFieldData GetFlowFieldForTarget(InstanceId targetId)
        {
            _flowFields.TryGetValue(targetId, out var fieldData);
            return fieldData;
        }

        public Vector3 GetFlowDirection(Vector3 followerPosition, InstanceId targetId)
        {
            if (_flowFields.TryGetValue(targetId, out var fieldData))
            {
                FlowField flowField = fieldData.Field;
                var currentCoordsNullable = _grid.GetCoordsFromWorldPos(followerPosition);
                if (!currentCoordsNullable.HasValue) return Vector3.zero;

                var currentCoords = currentCoordsNullable.Value;
                var directionCoords = flowField.BestDirectionField[currentCoords.x, currentCoords.z];

                if (directionCoords.x == currentCoords.x && directionCoords.z == currentCoords.z)
                {
                    return Vector3.zero;
                }

                Vector3 startWorldPos = _grid.GetWorldPos(currentCoords.x, currentCoords.z);
                Vector3 directionWorldPos = _grid.GetWorldPos(directionCoords.x, directionCoords.z);

                return Vector3.Normalize(directionWorldPos - startWorldPos);
            }

            return Vector3.zero;
        }

        public void CreateFlowFieldForNewTarget(InstanceId targetId, Vector3 targetPosition)
        {
            if (!_flowFields.ContainsKey(targetId))
            {
                var newField = new FlowField(_grid.GridSize);
                CreateFlowField(targetPosition, newField);
                _flowFields[targetId] = new FlowFieldData(newField, targetPosition);
            }
        }

        private void CreateFlowField(Vector3 targetPosition, FlowField flowField)
        {
            flowField.Reset();

            var targetCoordsNullable = _grid.GetCoordsFromWorldPos(targetPosition);
            if (!targetCoordsNullable.HasValue) return;
            var targetCoords = targetCoordsNullable.Value;

            _cellQueue.Clear();
            _cellQueue.Enqueue(targetCoords);

            flowField.CostField[targetCoords.x, targetCoords.z] = 0;
    
            const ushort straightCost = 10;
            const ushort diagonalCost = 14;

            while (_cellQueue.Count > 0)
            {
                var currentCoords = _cellQueue.Dequeue();
                ushort currentCost = flowField.CostField[currentCoords.x, currentCoords.z];

                for (int x = -1; x <= 1; x++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        if (x == 0 && z == 0) continue;

                        int nx = currentCoords.x + x;
                        int nz = currentCoords.z + z;

                        if (nx < 0 || nx >= _grid.GridSize || nz < 0 || nz >= _grid.GridSize) continue;

                        ushort terrainCost = _staticCostField[nx, nz];
                        if (terrainCost == ushort.MaxValue)
                        {
                            continue;
                        }
                
                        bool isDiagonal = (x != 0 && z != 0);
                        ushort moveCost = isDiagonal ? diagonalCost : straightCost;

                        ushort newCost = (ushort)(currentCost + moveCost + terrainCost);

                        if (newCost < flowField.CostField[nx, nz])
                        {
                            flowField.CostField[nx, nz] = newCost;
                            flowField.BestDirectionField[nx, nz] = currentCoords;
                            _cellQueue.Enqueue((nx, nz));
                        }
                    }
                }
            }
        }
    }
}
