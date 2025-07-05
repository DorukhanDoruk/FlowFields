using System;
namespace Scripts.Entity
{
    public readonly struct EntityIdentity : IEquatable<EntityIdentity>
    {
        public readonly InstanceId InstanceId;
        public readonly EntityType Type;
        
        public EntityIdentity(InstanceId instanceId, EntityType type)
        {
            InstanceId = instanceId;
            Type = type;
        }

        public bool Equals(EntityIdentity other)
        {
            return InstanceId.Value == other.InstanceId.Value;
        }
        public override bool Equals(object obj)
        {
            return obj is EntityIdentity other && Equals(other);
        }
        public override int GetHashCode()
        {
            return InstanceId.GetHashCode();
        }
    }
}