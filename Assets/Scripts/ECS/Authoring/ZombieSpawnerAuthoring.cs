using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
public class ZombieSpawnerAuthoring : MonoBehaviour
{
    public GameObject ZombiePrefab;
    public int TotalZombies = 100000;
    public Vector2 HalfExtents = new(1000, 1000);
    public uint RandomSeed = 1234;

    class Baker : Baker<ZombieSpawnerAuthoring>
    {
        public override void Bake(ZombieSpawnerAuthoring a)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new ZombieStreamConfig
            {
                Prefab = GetEntity(a.ZombiePrefab, TransformUsageFlags.Dynamic | TransformUsageFlags.Renderable),
                TotalZombies = a.TotalZombies,
                HalfExtents = a.HalfExtents,
                RandomSeed = a.RandomSeed
            });
        }
    }
}
