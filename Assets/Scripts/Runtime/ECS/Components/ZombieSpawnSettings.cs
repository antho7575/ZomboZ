using Unity.Entities;

namespace ZomboZ.Runtime
{
    public struct ZombieSpawnSettings : IComponentData
    {
        public Entity Prefab { get; set; }
        public float SpawnInterval { get; set; }
        public float SpawnRadius { get; set; }
        public float DespawnDistance { get; set; }
    }
}
