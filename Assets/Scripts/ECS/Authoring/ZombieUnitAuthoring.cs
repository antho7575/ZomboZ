using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[DisallowMultipleComponent]
public class ZombiePrefabAuthoring : MonoBehaviour
{
    public GameObject ZombieVisualPrefab; // e.g., holds Animator/SkinnedMesh/etc.

    class Baker : Baker<ZombiePrefabAuthoring>
    {
        public override void Bake(ZombiePrefabAuthoring a)
        {
            var e = GetEntity(TransformUsageFlags.Dynamic | TransformUsageFlags.Renderable);

            // Default ECS data a zombie should have
            AddComponent<ZombieTag>(e);
            AddComponent<WanderTag>(e);
            AddComponent<WanderState>(e);
            AddComponent(e, new DesiredVelocity { Value = float3.zero });
            AddComponent(e, new AnimState { Speed = 0f });
            AddComponent(e, new MoveSpeed { Value = 1.6f });
            AddComponent(e, new Velocity { Value = float3.zero });
            AddComponent(e, LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));

            // Managed ref copied to all instances
            AddComponentObject(e, new ZombieGameObjectPrefab { Value = a.ZombieVisualPrefab });
        }
    }
}
