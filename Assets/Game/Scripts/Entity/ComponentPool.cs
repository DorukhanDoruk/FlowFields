using System;
namespace Scripts.Entity
{
    public interface IComponentPool
    {
        bool Has(int entityId);
        void Remove(int entityId);
    }

    public class ComponentPool<T> : IComponentPool where T : BaseComponent
    {
        private T[] _dense = new T[16];
        private int[] _sparse = new int[1024];

        private int[] _entityIds = new int[16];

        public int Count { get; private set; }

        public void Add(int entityId, T component)
        {
            if (entityId >= _sparse.Length) Array.Resize(ref _sparse, entityId * 2);
            if (Count >= _dense.Length)
            {
                Array.Resize(ref _dense, Count * 2);
                Array.Resize(ref _entityIds, Count * 2);
            }

            _sparse[entityId] = Count;
            _dense[Count] = component;
            _entityIds[Count] = entityId;
            Count++;
        }

        public void Remove(int entityId)
        {
            if (!Has(entityId)) return;

            int indexOfRemoved = _sparse[entityId];
            int lastEntityId = _entityIds[Count - 1];

            _dense[indexOfRemoved] = _dense[Count - 1];
            _entityIds[indexOfRemoved] = lastEntityId;
            _sparse[lastEntityId] = indexOfRemoved;

            Count--;
        }


        public bool Has(int entityId)
        {
            return entityId < _sparse.Length && _sparse[entityId] < Count && _entityIds[_sparse[entityId]] == entityId;
        }

        public ref T Get(int entityId)
        {
            if (!Has(entityId))
            {
                throw new Exception($"Entity {entityId} does not have a component of type {typeof(T).Name}");
            }
            return ref _dense[_sparse[entityId]];
        }
    }
}
