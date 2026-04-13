using System;
using System.Collections.Generic;
using Unity.Entities;

namespace ZomboZ.Runtime
{
    /// <summary>
    /// Maintains a GUID -> Entity mapping for fast zombie lookups.
    /// O(1) lookup instead of iterating through all entities!
    /// </summary>
    public static class ZombieEntityRegistry
    {
        static readonly Dictionary<Guid, Entity> _guidToEntity = new Dictionary<Guid, Entity>();

        /// <summary>
        /// Register a zombie entity with its GUID.
        /// Call this when spawning a zombie.
        /// </summary>
        public static void Register(Guid guid, Entity entity)
        {
            _guidToEntity[guid] = entity;
        }

        /// <summary>
        /// Unregister a zombie entity.
        /// Call this when despawning/destroying a zombie.
        /// </summary>
        public static void Unregister(Guid guid)
        {
            _guidToEntity.Remove(guid);
        }

        /// <summary>
        /// Try to find a zombie entity by its GUID.
        /// Returns true if found, false otherwise.
        /// O(1) lookup!
        /// </summary>
        public static bool TryGetEntity(Guid guid, out Entity entity)
        {
            return _guidToEntity.TryGetValue(guid, out entity);
        }

        /// <summary>
        /// Clear all registrations.
        /// </summary>
        public static void Clear()
        {
            _guidToEntity.Clear();
        }

        /// <summary>
        /// Get count of registered zombies.
        /// </summary>
        public static int Count => _guidToEntity.Count;
    }
}
