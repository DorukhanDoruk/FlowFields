using System.Collections.Generic;
using UnityEngine;
namespace Scripts.Entity
{
    public class EntityPresenter
    {
        private readonly Dictionary<InstanceId, GameObject> _activeCubes = new();
        private readonly Queue<GameObject> _cubePool = new();

        private readonly Transform _cubeParent;
        private readonly GameObject _cubePrefab;

        public EntityPresenter()
        {
            _cubeParent = new GameObject("--- Entity Cubes ---").transform;

            _cubePrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _cubePrefab.SetActive(false);
            Object.Destroy(_cubePrefab.GetComponent<BoxCollider>());
        }

        public void CreateOrUpdateCube(InstanceId instanceId, Vector3 position)
        {
            if (_activeCubes.TryGetValue(instanceId, out var cube))
            {
                cube.transform.position = new Vector3(position.x, position.y, position.z);
            }
            else
            {
                GameObject newCube;
                if (_cubePool.Count > 0)
                {
                    newCube = _cubePool.Dequeue();
                    newCube.SetActive(true);
                }
                else
                {
                    newCube = Object.Instantiate(_cubePrefab, _cubeParent);
                    newCube.SetActive(true);
                }
                newCube.name = $"Entity_{instanceId.Value}";
                newCube.transform.position = new Vector3(position.x, position.y, position.z);
                _activeCubes[instanceId] = newCube;
            }
        }

        public void RemoveCube(InstanceId instanceId)
        {
            if (_activeCubes.TryGetValue(instanceId, out var cube))
            {
                cube.SetActive(false);
                _activeCubes.Remove(instanceId);
                _cubePool.Enqueue(cube);
            }
        }

        public void Tick(float deltaTime) { }
    }
}
