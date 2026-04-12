using ZomboZ.Infrastructure.Cache;
using ZomboZ.Infrastructure.Persistence;

namespace ZomboZ.Infrastructure.Mappers
{
    public static class ZombieMapper
    {
        public static ZombiePersistenceModel ToPersistence(ZombieCacheModel cache)
        {
            return new ZombiePersistenceModel
            {
                Id = cache.Id,
                PosX = cache.PosX,
                PosY = cache.PosY,
                PosZ = cache.PosZ,
                RotationY = cache.RotationY,
                Health = cache.Health,
                LastSeenTicks = cache.LastSeenTicks
            };
        }

        public static ZombieCacheModel ToCache(ZombiePersistenceModel persistence)
        {
            return new ZombieCacheModel
            {
                Id = persistence.Id,
                PosX = persistence.PosX,
                PosY = persistence.PosY,
                PosZ = persistence.PosZ,
                RotationY = persistence.RotationY,
                Health = persistence.Health,
                LastSeenTicks = persistence.LastSeenTicks,
                IsSpawned = false
            };
        }
    }
}
