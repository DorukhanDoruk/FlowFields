namespace Scripts.Entity
{
    public sealed class Entity
    {
        public EntityIdentity Identity { get; }

        public Entity(EntityIdentity identity) 
        {
            Identity = identity;
        }
    }
}
