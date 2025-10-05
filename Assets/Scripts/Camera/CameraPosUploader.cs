using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;


[DisallowMultipleComponent]
public class CameraPosUploader : MonoBehaviour
{
    EntityManager _em;
    Entity _singleton;

    void Awake()
    {
        _em = World.DefaultGameObjectInjectionWorld.EntityManager;

        // Create (or find) the singleton
        var q = _em.CreateEntityQuery(ComponentType.ReadOnly<CameraPositionSingleton>());
        if (!q.TryGetSingletonEntity<CameraPositionSingleton>(out _singleton))
        {
            _singleton = _em.CreateEntity(typeof(CameraPositionSingleton));
            _em.SetName(_singleton, "CameraPositionSingleton");
            _em.SetComponentData(_singleton, new CameraPositionSingleton { Value = float3.zero });
        }
    }

    void LateUpdate()
    {
        if (_em.Exists(_singleton))
        {
            _em.SetComponentData(_singleton, new CameraPositionSingleton
            {
                Value = (float3)transform.position
            });
        }
    }
}
