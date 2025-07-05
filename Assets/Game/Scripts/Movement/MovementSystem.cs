using Scripts.Entity;
using Scripts.GridSystem;
namespace Scripts.Movement
{
    public class MovementSystem
    {
        private readonly EntityManager _entityManager;
        private readonly GridManager _gridManager;
        private Archetype _movableArchetype;

        public MovementSystem(EntityManager entityManager, GridManager gridManager)
        {
            _entityManager = entityManager;
            _gridManager = gridManager;
        }

        public void Tick(float deltaTime)
        {
            if(_movableArchetype == null)
                _movableArchetype = _entityManager.GetArchetype(typeof(CTransformComponent), typeof(CTargetComponent), typeof(CRenderableComponent));
            
            if (_movableArchetype == null || _movableArchetype.Entities.Count == 0) return;

            foreach (var entityId in _movableArchetype.Entities)
            {
                ref var transform = ref _entityManager.GetComponent<CTransformComponent>(entityId);
                ref var target = ref _entityManager.GetComponent<CTargetComponent>(entityId);
                
                if (target.TargetEntityId.Value == InstanceId.Invalid.Value) continue;
                if (!_entityManager.HasComponent<CTransformComponent>(target.TargetEntityId)) continue;
                
                var flowDirection = _gridManager.GetFlowDirection(
                    transform.Position, 
                    target.TargetEntityId
                );

                float moveSpeed = 5.0f; 
                transform.Position += flowDirection * (moveSpeed * deltaTime);
            }
        }
    }
}
