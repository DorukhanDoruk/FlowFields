using System;
using System.Collections.Generic;
namespace Scripts.Entity
{
    public class Archetype
    {
        public readonly int Hash;
        public readonly List<InstanceId> Entities = new();
        public readonly HashSet<Type> ComponentTypes;

        public Archetype(HashSet<Type> componentTypes)
        {
            ComponentTypes = componentTypes;
            Hash = CalculateHash(componentTypes);
        }

        public void AddEntity(InstanceId entityId) => Entities.Add(entityId);
        public void RemoveEntity(InstanceId entityId) => Entities.Remove(entityId);

        public static int CalculateHash(HashSet<Type> types)
        {
            int hash = 17;
            foreach (var type in new SortedSet<Type>(types, Comparer<Type>.Create((a, b) => a.FullName.CompareTo(b.FullName))))
            {
                hash = hash * 31 + type.GetHashCode();
            }
            return hash;
        }
    }
}
