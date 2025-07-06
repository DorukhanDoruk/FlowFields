using Scripts.Entity;
using System.Collections.Generic;
namespace Scripts.Rendering
{
    public class RenderSystem
    {
        private readonly EntityManager _entityManager;
        private readonly EntityPresenter _presenter;
        private List<Archetype> _renderableArchetypes;

        public RenderSystem(EntityManager entityManager, EntityPresenter presenter)
        {
            _entityManager = entityManager;
            _presenter = presenter;
        }

        public void Tick(float deltaTime)
        {
            using (new Unity.Profiling.ProfilerMarker("RenderSystem.Tick").Auto())
            {
                if(_renderableArchetypes == null)
                    _renderableArchetypes = _entityManager.GetArchetypesWith(typeof(CTransformComponent), typeof(CRenderableComponent));
            
                if (_renderableArchetypes.Count == 0) 
                    return;

                foreach (var archetype in _renderableArchetypes)
                {
                    foreach (var entityId in archetype.Entities)
                    {
                        ref var transform = ref _entityManager.GetComponent<CTransformComponent>(entityId);
                        _presenter.CreateOrUpdateCube(entityId, transform.Position);
                    }
                }
            }
        }
    }
}
