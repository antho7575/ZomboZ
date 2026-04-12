using Unity.Entities;

namespace ZomboZ.Runtime
{
    public struct ZombieSpawnSettings : IComponentData
    {
        public float SpawnInterval;
        public float SpawnRadius;
        public int DesiredCount;
        public float DespawnDistance;
    }
}
