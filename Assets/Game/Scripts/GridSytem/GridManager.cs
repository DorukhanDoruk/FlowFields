using Scripts.Constants;
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
        private readonly ushort[,] _staticCostField;
        private readonly Queue<(int x, int z)> _cellQueue;

        private EntityManager _entityManager;
        private bool _isStaticCostFieldDirty = true;

        public GridManager(int gridSize, float cellSize)
        {
            _grid = new Grid(gridSize, cellSize);
            _staticCostField = new ushort[gridSize, gridSize];
            _cellQueue = new Queue<(int x, int z)>(gridSize);
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
                float thresholdSq = ConstantVariables.Grid.RecalculateDistanceThreshold * _grid.CellSize * (ConstantVariables.Grid.RecalculateDistanceThreshold * _grid.CellSize);

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
                int currentFlatIndex = currentCoords.x * _grid.GridSize + currentCoords.z;

                int directionFlatIndex = flowField.BestDirectionField[currentFlatIndex];

                if (directionFlatIndex == currentFlatIndex)
                    return Vector3.zero;

                Vector3 startWorldPos = _grid.GetWorldPos(currentCoords.x, currentCoords.z);

                int dirX = directionFlatIndex / _grid.GridSize;
                int dirZ = directionFlatIndex % _grid.GridSize;
                Vector3 directionWorldPos = _grid.GetWorldPos(dirX, dirZ);

                return Vector3.Normalize(directionWorldPos - startWorldPos);
            }
            return Vector3.zero;
        }

        public void CreateFlowFieldForNewTarget(InstanceId targetId, Vector3 targetPosition)
        {
            if (!_flowFields.ContainsKey(targetId))
            {
                var newField = new FlowField(_grid.GridSize);
                if (_isStaticCostFieldDirty)
                {
                    CreateStaticCostField();
                    _isStaticCostFieldDirty = false;
                }
                CreateFlowField(targetPosition, newField);
                _flowFields[targetId] = new FlowFieldData(newField, targetPosition);
            }
        }

        private void CreateFlowField(Vector3 targetPosition, FlowField flowField)
        {
            foreach (int flatIndex in flowField.DirtyCells)
            {
                flowField.CostField[flatIndex] = ushort.MaxValue;
                flowField.BestDirectionField[flatIndex] = flatIndex;
            }
            flowField.DirtyCells.Clear();

            var targetCoordsNullable = _grid.GetCoordsFromWorldPos(targetPosition);
            if (!targetCoordsNullable.HasValue) return;
            var targetCoords = targetCoordsNullable.Value;

            _cellQueue.Clear();
            _cellQueue.Enqueue(targetCoords);

            int targetFlatIndex = targetCoords.x * _grid.GridSize + targetCoords.z;
            flowField.CostField[targetFlatIndex] = 0;
            flowField.BestDirectionField[targetFlatIndex] = targetFlatIndex;
            flowField.DirtyCells.Add(targetFlatIndex);

            const ushort straightCost = 10;
            const ushort diagonalCost = 14;

            while (_cellQueue.Count > 0)
            {
                var currentCoords = _cellQueue.Dequeue();
                int currentFlatIndex = currentCoords.x * _grid.GridSize + currentCoords.z;
                ushort currentCost = flowField.CostField[currentFlatIndex];

                for (int x = -1; x <= 1; x++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        if (x == 0 && z == 0) continue;

                        int nx = currentCoords.x + x;
                        int nz = currentCoords.z + z;

                        if (nx < 0 || nx >= _grid.GridSize || nz < 0 || nz >= _grid.GridSize) continue;

                        int neighbourFlatIndex = nx * _grid.GridSize + nz;

                        if (flowField.CostField[neighbourFlatIndex] == ushort.MaxValue)
                        {
                            ushort terrainCost = _staticCostField[nx, nz];
                            if (terrainCost == ushort.MaxValue) continue;

                            ushort moveCost = (x != 0 && z != 0) ? diagonalCost : straightCost;
                            ushort newCost = (ushort)(currentCost + moveCost + terrainCost);

                            flowField.CostField[neighbourFlatIndex] = newCost;
                            flowField.BestDirectionField[neighbourFlatIndex] = currentFlatIndex;
                            _cellQueue.Enqueue((nx, nz));
                            flowField.DirtyCells.Add(neighbourFlatIndex);
                        }
                    }
                }
            }
        }
    }
}
