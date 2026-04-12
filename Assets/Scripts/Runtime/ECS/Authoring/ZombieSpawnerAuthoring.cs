using Unity.Entities;
using UnityEngine;


namespace ZomboZ.Runtime
{
    [DisallowMultipleComponent]
    public class ZombieSpawnerAuthoring : MonoBehaviour
    {
        public GameObject ZombiePrefab;
        public float SpawnInterval = 2f;
        public float SpawnRadius = 50f;
        public float DespawnDistance = 80f;

        class Baker : Baker<ZombieSpawnerAuthoring>
        {
            public override void Bake(ZombieSpawnerAuthoring a)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new ZombieSpawnSettings
                {
                    Prefab = GetEntity(a.ZombiePrefab, TransformUsageFlags.Dynamic | TransformUsageFlags.Renderable),
                    SpawnInterval = a.SpawnInterval,
                    SpawnRadius = a.SpawnRadius,
                    DespawnDistance = a.DespawnDistance,
                });
            }
        }
    }
}
