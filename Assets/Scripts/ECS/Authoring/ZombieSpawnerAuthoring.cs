using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;

public class ZombieSpawnerAuthoring : MonoBehaviour
{
    public GameObject ZombiePrefab;
    public int Count = 500;
    public Vector2 Area = new(100, 100);
    public float Speed = 3.5f;

    class Baker : Baker<ZombieSpawnerAuthoring>
    {
        public override void Bake(ZombieSpawnerAuthoring a)
        {
            var e = GetEntity(TransformUsageFlags.None);
            var test = GetEntity(a.ZombiePrefab, TransformUsageFlags.Renderable | TransformUsageFlags.Dynamic);
            AddComponent(e, new ZombieSpawner
            {
                Prefab = GetEntity(a.ZombiePrefab, TransformUsageFlags.Renderable | TransformUsageFlags.Dynamic),
                Count = a.Count,
                Area = new float2(a.Area.x, a.Area.y),
                Speed = a.Speed
            });
        }
    }
}

public struct ZombieSpawner : IComponentData
{
    public Entity Prefab;
    public int Count;
    public float2 Area;
    public float Speed;
}
