using System;
using System.Collections.Generic;
using System.Linq;
namespace Scripts.Entity
{
    public class EntityManager
    {
        private readonly Dictionary<Type, IComponentPool> _componentPools = new();

        private readonly Dictionary<int, Archetype> _archetypes = new();
        private readonly Dictionary<InstanceId, Archetype> _entityArchetypeMap = new();

        private readonly Queue<InstanceId> _recycledIds = new();
        private int _nextEntityId = 1;

        #region Entity Lifecycle
        public InstanceId CreateEntity()
        {
            var id = _recycledIds.Count > 0 ? _recycledIds.Dequeue() : new InstanceId(_nextEntityId++);
            UpdateEntityArchetype(id, new HashSet<Type>());
            return id;
        }

        public void DestroyEntity(InstanceId instanceId)
        {
            if (!_entityArchetypeMap.TryGetValue(instanceId, out var archetype)) return;

            foreach (var componentType in archetype.ComponentTypes.ToList())
            {
                if(_componentPools.TryGetValue(componentType, out var pool))
                {
                    pool.Remove(instanceId.Value);
                }
            }

            archetype.RemoveEntity(instanceId);
            _entityArchetypeMap.Remove(instanceId);
            _recycledIds.Enqueue(instanceId);
        }
        #endregion

        #region Component Management
        public void AddComponent<T>(InstanceId instanceId, T component) where T : BaseComponent
        {
            var pool = GetOrCreatePool<T>();
            pool.Add(instanceId.Value, component);

            var currentArchetype = _entityArchetypeMap[instanceId];
            var newComponentTypes = new HashSet<Type>(currentArchetype.ComponentTypes) { typeof(T) };
            UpdateEntityArchetype(instanceId, newComponentTypes);
        }

        public void RemoveComponent<T>(InstanceId instanceId) where T : BaseComponent
        {
            if (!_entityArchetypeMap.TryGetValue(instanceId, out var archetype) || !archetype.ComponentTypes.Contains(typeof(T)))
                return;
                
            if (_componentPools.TryGetValue(typeof(T), out var pool))
            {
                pool.Remove(instanceId.Value);
            }

            var newComponentTypes = new HashSet<Type>(archetype.ComponentTypes);
            newComponentTypes.Remove(typeof(T));
            UpdateEntityArchetype(instanceId, newComponentTypes);
        }
        
        public ref T GetComponent<T>(InstanceId instanceId) where T : BaseComponent
        {
            return ref ((ComponentPool<T>)_componentPools[typeof(T)]).Get(instanceId.Value);
        }
        
        public bool HasComponent<T>(InstanceId instanceId) where T : BaseComponent
        {
            return _entityArchetypeMap.TryGetValue(instanceId, out var archetype) && archetype.ComponentTypes.Contains(typeof(T));
        }

        #endregion

        #region Archetype Management
        private void UpdateEntityArchetype(InstanceId entityId, HashSet<Type> newComponentTypes)
        {
            if (_entityArchetypeMap.TryGetValue(entityId, out var oldArchetype))
            {
                oldArchetype.RemoveEntity(entityId);
            }

            int newHash = Archetype.CalculateHash(newComponentTypes);
            if (!_archetypes.TryGetValue(newHash, out var newArchetype))
            {
                newArchetype = new Archetype(newComponentTypes);
                _archetypes[newHash] = newArchetype;
            }

            newArchetype.AddEntity(entityId);
            _entityArchetypeMap[entityId] = newArchetype;
        }

        public Archetype GetArchetype(params Type[] componentTypes)
        {
            int hash = Archetype.CalculateHash(new HashSet<Type>(componentTypes));
            _archetypes.TryGetValue(hash, out var archetype);
            return archetype;
        }
        
        public List<Archetype> GetArchetypesWith(params Type[] componentTypes)
        {
            var requiredTypes = new HashSet<Type>(componentTypes);
            var matchingArchetypes = new List<Archetype>();

            if (requiredTypes.Count == 0) return matchingArchetypes;

            foreach (var archetype in _archetypes.Values)
            {
                if (requiredTypes.IsSubsetOf(archetype.ComponentTypes))
                {
                    matchingArchetypes.Add(archetype);
                }
            }
            return matchingArchetypes;
        }
        #endregion

        private ComponentPool<T> GetOrCreatePool<T>() where T : BaseComponent
        {
            if (!_componentPools.TryGetValue(typeof(T), out var pool))
            {
                pool = new ComponentPool<T>();
                _componentPools[typeof(T)] = pool;
            }
            return (ComponentPool<T>)pool;
        }
    }

}
