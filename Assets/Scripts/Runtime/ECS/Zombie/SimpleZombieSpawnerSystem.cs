using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using ZomboZ.Infrastructure.Cache;

namespace ZomboZ.Runtime
{
    public partial class SimpleZombieSpawnerSystem : SystemBase
    {
        double _lastSpawnTime = 0.0;

        protected override void OnCreate()
        {
            RequireForUpdate<ZombieStreamConfig>();

            // Ensure a default spawn settings singleton exists
            if (!HasSingleton<ZombieSpawnSettings>())
            {
                var e = EntityManager.CreateEntity();
                EntityManager.AddComponentData(e, new ZombieSpawnSettings
                {
                    SpawnInterval = 2f,
                    SpawnRadius = 50f,
                    DesiredCount = 20,
                    DespawnDistance = 80f
                });
            }
        }

        protected override void OnUpdate()
        {
            var em = EntityManager;

            // timing
            var now = SystemAPI.Time.ElapsedTime;
            var settings = SystemAPI.GetSingleton<ZombieSpawnSettings>();
            if (now - _lastSpawnTime < settings.SpawnInterval)
                return;
            _lastSpawnTime = now;

            // Get player position safely via scene object (falls back to world origin)
            float3 center = float3.zero;
            var playerGo = UnityEngine.GameObject.FindWithTag("Player");
            if (playerGo == null)
                playerGo = UnityEngine.GameObject.Find("Player");
            if (playerGo != null)
            {
                var p = playerGo.transform.position;
                center = new float3(p.x, p.y, p.z);
            }

            // Use cache + persistence to restore nearby zombies first
            var nearby = ZombieCacheService.QueryNear(center, settings.SpawnRadius);
            int toSpawn = nearby.Count;

            var rng = new Unity.Mathematics.Random((uint)UnityEngine.Random.Range(1, int.MaxValue));

            // Spawn restored zombies
            for (int i = 0; i < nearby.Count; i++)
            {
                var r = nearby[i];
                var request = new ZombieCreateRequest
                {
                    Prefab = SystemAPI.GetSingleton<ZombieStreamConfig>().Prefab,
                    Id = r.Id,
                    Position = new float3(r.PosX, r.PosY, r.PosZ),
                    Rotation = quaternion.EulerXYZ(0, r.RotationY, 0),
                    Scale = 1f,
                    MoveSpeed = 1f,
                    Hunger = r.Health,
                    Velocity = float3.zero,
                    DesiredVelocity = float3.zero,
                    TimeSinceSeenPlayer = 999f,
                    WithWander = true,
                    WithAnimation = true
                };

                ZombieEntityFactory.CreateZombie(em, request);

                // Mark record as spawned so it won't be chosen again
                r.IsSpawned = true;
                ZombieCacheService.AddOrUpdate(r);
                ZombiePersistenceService.AddOrUpdate(r);

                toSpawn--;
            }

            // Create new zombies if needed
            for (int i = 0; i < toSpawn; i++)
            {
                float ang = rng.NextFloat(0f, math.PI * 2f);
                float dist = rng.NextFloat(0f, settings.SpawnRadius);
                float3 pos = new float3(
                    center.x + math.cos(ang) * dist,
                    center.y,
                    center.z + math.sin(ang) * dist);

                var record = new ZombieCacheModel
                {
                    Id = System.Guid.NewGuid(),
                    PosX = pos.x,
                    PosY = pos.y,
                    PosZ = pos.z,
                    RotationY = 0f,
                    Health = 100,
                    IsSpawned = true
                };

                ZombiePersistenceService.AddOrUpdate(record);
                ZombieCacheService.AddOrUpdate(record);

                var request = new ZombieCreateRequest
                {
                    Prefab = SystemAPI.GetSingleton<ZombieStreamConfig>().Prefab,
                    Id = record.Id,
                    Position = pos,
                    Rotation = quaternion.identity,
                    Scale = 1f,
                    MoveSpeed = 1f,
                    Hunger = 0f,
                    Velocity = float3.zero,
                    DesiredVelocity = float3.zero,
                    TimeSinceSeenPlayer = 999f,
                    WithWander = true,
                    WithAnimation = true
                };

                ZombieEntityFactory.CreateZombie(em, request);
            }
        }
    }
}
