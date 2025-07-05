using Scripts.Entity;
using Scripts.GameLoop.Scripts.GameLoop;
using Scripts.GridSystem;
using Scripts.GridSystem.Model;
using Scripts.Movement;
using Scripts.Rendering;
using UnityEngine;
namespace Scripts.GameLoop
{
    public class GameManager : MonoBehaviour
    {
        private GridManager _gridManager;
        private EntityManager _entityManager;
        private EntityPresenter _entityPresenter;
        private MovementSystem _movementSystem;
        private RenderSystem _renderSystem;
        
        private PlayerMovementController _playerController;

        private void Start()
        {
            var gridSize = 1000;

            _gridManager = new GridManager(gridSize, 0.25f);
            _entityPresenter = new EntityPresenter();
            _entityManager = new EntityManager();
            _movementSystem = new MovementSystem(_entityManager, _gridManager);
            _renderSystem = new RenderSystem(_entityManager, _entityPresenter);
            
            _gridManager.SetEssentialComponents(_entityManager);

            _playerController = gameObject.AddComponent<PlayerMovementController>();

            var player = _entityManager.CreateEntity();
            var playerStartPos = Vector3.zero;
            _entityManager.AddComponent(player, new CTransformComponent { Position = playerStartPos });
            _entityManager.AddComponent(player, new CRenderableComponent());
            _gridManager.CreateFlowFieldForNewTarget(player, playerStartPos);

            _playerController.Initialize(_entityManager, player);

            for (int i = 0; i < 500; i++)
            {
                var enemy = _entityManager.CreateEntity();
                _entityManager.AddComponent(enemy, new CTransformComponent { Position = new Vector3(Random.Range(-100, 100), 0, Random.Range(-100, 100)) });
                _entityManager.AddComponent(enemy, new CTargetComponent { TargetEntityId = player });
                _entityManager.AddComponent(enemy, new CRenderableComponent());
            }
        }

        private void Update()
        {
            var deltaTime = Time.deltaTime;
            _gridManager.Tick(deltaTime);
            _movementSystem.Tick(deltaTime);
            _renderSystem.Tick(deltaTime);
        }

        private void OnDrawGizmos()
        {
            if (_gridManager == null) return;

            var grid = _gridManager.Grid;
            if (grid == null) return;

            var targetId = new InstanceId(1);
            FlowFieldData fieldData = _gridManager.GetFlowFieldForTarget(targetId);

            if (fieldData?.Field == null) return;

            var flowField = fieldData.Field;

            int gizmoDrawStep = 5;

            for (int x = 0; x < grid.GridSize; x += gizmoDrawStep)
            {
                for (int z = 0; z < grid.GridSize; z += gizmoDrawStep)
                {

                    if (grid.IsObstacle(x, z)) continue;

                    var directionCoords = flowField.BestDirectionField[x, z];
                    var currentCoords = (x, z);

                    if (directionCoords.x == currentCoords.x && directionCoords.z == currentCoords.z) continue;

                    Vector3 startPos = grid.GetWorldPos(currentCoords.x, currentCoords.z);
                    Vector3 endPos = grid.GetWorldPos(directionCoords.x, directionCoords.z);

                    float cost = flowField.CostField[x, z];
                    if (cost == ushort.MaxValue) continue;

                    float maxCost = 5000f;
                    Gizmos.color = Color.Lerp(Color.green, Color.red, cost / maxCost);
                    Gizmos.DrawLine(startPos, endPos);
                }
            }
        }
    }
}
