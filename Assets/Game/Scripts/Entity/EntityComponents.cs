using UnityEngine;
namespace Scripts.Entity
{
    public abstract record BaseComponent
    {
        public abstract void Reset();
    }

    public record CTransformComponent : BaseComponent
    {
        private Matrix4x4 _trs;
        public Vector3 Position
        {
            get => _trs.GetColumn(3);
            set => _trs.SetColumn(3, new Vector4(value.x, value.y, value.z, 1f));
        }

        public Quaternion Rotation
        {
            get => _trs.rotation;
            set
            {
                var scale = Scale;
                _trs = Matrix4x4.TRS(Position, value, scale);
            }
        }

        public Vector3 Scale
        {
            get => _trs.lossyScale;
            set
            {
                var pos = Position;
                var rot = Rotation;
                _trs = Matrix4x4.TRS(pos, rot, value);
            }
        }

        public void SetPosition(float x, float y, float z)
        {
            Position = new Vector3(x, y, z);
        }

        public void SetRotation(float x, float y, float z, float w)
        {
            Rotation = new Quaternion(x, y, z, w);
        }

        public void SetScale(float x, float y, float z)
        {
            Scale = new Vector3(x, y, z);
        }

        public override void Reset()
        {
            _trs = Matrix4x4.identity;
        }
    }

    public record CTargetComponent : BaseComponent
    {
        public InstanceId TargetEntityId;

        public override void Reset()
        {
            TargetEntityId = InstanceId.Invalid;
        }
    }
    
    public record CRenderableComponent : BaseComponent
    {
        public override void Reset() { }
    }
}
