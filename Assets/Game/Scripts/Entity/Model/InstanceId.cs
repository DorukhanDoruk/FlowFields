using System;
namespace Scripts.Entity
{
    public readonly struct InstanceId : IEquatable<InstanceId>
    {
        public readonly int Value;
        public InstanceId(int value)
        {
            Value = value;
        }
        
        public bool Equals(InstanceId other)
        {
            return Value == other.Value;
        }
        public override bool Equals(object obj)
        {
            return obj is InstanceId other && Equals(other);
        }
        public override int GetHashCode()
        {
            return Value;
        }
        
        public static readonly InstanceId Invalid = new InstanceId(0);
    }
}
