using Scripts.Entity;
using UnityEngine;
namespace Scripts.GameLoop
{
    namespace Scripts.GameLoop
    {
        public class PlayerMovementController : MonoBehaviour
        {
            private EntityManager _entityManager;
            private InstanceId _playerEntityId;

            [SerializeField] private float _speed = 10f;

            public void Initialize(EntityManager entityManager, InstanceId playerEntityId)
            {
                _entityManager = entityManager;
                _playerEntityId = playerEntityId;
            }

            private void Update()
            {
                if (_entityManager == null || _playerEntityId.Value == InstanceId.Invalid.Value)
                {
                    return;
                }

                float horizontalInput = Input.GetAxisRaw("Horizontal");
                float verticalInput = Input.GetAxisRaw("Vertical");

                Vector3 moveDirection = new Vector3(horizontalInput, 0, verticalInput);

                if (Vector3.SqrMagnitude(moveDirection) > 0)
                {
                    moveDirection.Normalize();
                    ref var playerTransform = ref _entityManager.GetComponent<CTransformComponent>(_playerEntityId);
                    playerTransform.Position += moveDirection * (_speed * Time.deltaTime);
                }
            }
        }
    }
}
