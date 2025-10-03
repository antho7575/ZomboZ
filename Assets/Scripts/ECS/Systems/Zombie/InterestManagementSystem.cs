using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[DisableAutoCreation]
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct InterestManagementSystem : ISystem
{
    // ---- system-owned stores (as before) ----
    private NativeParallelMultiHashMap<int2, ZombieRecord> _bySector;
    private NativeParallelHashMap<int, ZombieRecord> _byId;
    private NativeParallelHashSet<int> _liveIds;

    // ---- cached queries (NEW) ----
    private EntityQuery _liveZombiesQ;
    private EntityQuery _prefabRefQ;
    private EntityQuery _spawnerQ;

    public void OnCreate(ref SystemState s)
    {
        _bySector = new NativeParallelMultiHashMap<int2, ZombieRecord>(8192, Allocator.Persistent);
        _byId = new NativeParallelHashMap<int, ZombieRecord>(16384, Allocator.Persistent);
        _liveIds = new NativeParallelHashSet<int>(16384, Allocator.Persistent);

        _liveZombiesQ = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<ZombieTag, ZombieId, LocalTransform, Sector>()
            .Build(ref s);

        _prefabRefQ = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<ZombiePrefabRef>()
            .Build(ref s);

        _spawnerQ = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<ZombieSpawner>()
            .Build(ref s);

        // (optional) prefill _bySector/_byId here… (same as before)
    }

    public void OnDestroy(ref SystemState s)
    {
        if (_bySector.IsCreated) _bySector.Dispose();
        if (_byId.IsCreated) _byId.Dispose();
        if (_liveIds.IsCreated) _liveIds.Dispose();
    }

    public void OnUpdate(ref SystemState s)
    {
        if (!SystemAPI.TryGetSingleton<ObserverSettings>(out var obs)) return;

        var cam = Camera.main; if (!cam) return;
        float3 playerPos = cam.transform.position;
        int2 playerSector = new int2(
            (int)math.floor(playerPos.x / obs.SectorSize),
            (int)math.floor(playerPos.z / obs.SectorSize));

        // build desired sectors…
        var desired = new NativeList<int2>(Allocator.Temp);
        for (int dx = -obs.LoadRadius; dx <= obs.LoadRadius; dx++)
            for (int dz = -obs.LoadRadius; dz <= obs.LoadRadius; dz++)
                desired.Add(new int2(playerSector.x + dx, playerSector.y + dz));

        // refresh live id set
        _liveIds.Clear();
        using (var ids = _liveZombiesQ.ToComponentDataArray<ZombieId>(Allocator.Temp))
            for (int i = 0; i < ids.Length; i++) _liveIds.Add(ids[i].Value);

        // resolve prefab once per frame (instance method, no SystemAPI in static)
        var prefab = ResolveZombiePrefab(ref s);

        // spawn missing
        var ecbSpawn = new EntityCommandBuffer(Allocator.Temp);
        // Only spawn entities for zombies in desired sectors (near player)
        for (int i = 0; i < desired.Length; i++)
        {
            var sec = desired[i];
            if (!_bySector.TryGetFirstValue(sec, out var rec, out var it)) continue;
            do
            {
                if (!_liveIds.Contains(rec.Id))
                {
                    // Spawn entity for this zombie
                    var z = ecbSpawn.Instantiate(prefab);
                    ecbSpawn.AddComponent(z, new ZombieTag());
                    ecbSpawn.AddComponent(z, new ZombieId { Value = rec.Id });
                    ecbSpawn.AddComponent(z, new Sector { XY = sec });
                    ecbSpawn.AddComponent(z, new MoveSpeed { Value = 3f });
                    ecbSpawn.AddComponent(z, new Velocity { Value = float3.zero });
                    ecbSpawn.AddComponent(z, new DesiredVelocity { Value = float3.zero });
                    ecbSpawn.AddComponent<WanderTag>(z);
                    ecbSpawn.SetComponent(z, LocalTransform.FromPositionRotationScale(
                        rec.Pos, quaternion.AxisAngle(math.up(), rec.Heading), 1f));
                    _liveIds.Add(rec.Id);
                }
            }
            while (_bySector.TryGetNextValue(out rec, ref it));
        }
        ecbSpawn.Playback(s.EntityManager);
        ecbSpawn.Dispose();

        // despawn outside hard radius…
        int hard = math.max(obs.HardUnloadRadius, obs.LoadRadius + 1);
        using var live = _liveZombiesQ.ToEntityArray(Allocator.Temp);
        using var ids2 = _liveZombiesQ.ToComponentDataArray<ZombieId>(Allocator.Temp);
        using var xfs = _liveZombiesQ.ToComponentDataArray<LocalTransform>(Allocator.Temp);
        using var secs2 = _liveZombiesQ.ToComponentDataArray<Sector>(Allocator.Temp);

        var ecbDespawn = new EntityCommandBuffer(Allocator.Temp);
        // Despawn entities outside hard radius
        for (int i = 0; i < live.Length; i++)
        {
            int2 sec = secs2[i].XY;
            if (math.abs(sec.x - playerSector.x) > hard || math.abs(sec.y - playerSector.y) > hard)
            {
                // Save state and destroy entity
                var rec = new ZombieRecord
                {
                    Id = ids2[i].Value,
                    Sector = sec,
                    Pos = xfs[i].Position,
                    Heading = 0f,
                    WanderSeed = 0u,
                    TimeSinceSeen = 999f
                };
                _byId[rec.Id] = rec;
                _bySector.Add(rec.Sector, rec);

                ecbDespawn.DestroyEntity(live[i]);
                _liveIds.Remove(rec.Id);
            }
        }
        ecbDespawn.Playback(s.EntityManager);
        ecbDespawn.Dispose();

        desired.Dispose();
    }

    // -------- instance helper (NO static, NO SystemAPI in static) --------
    private Entity ResolveZombiePrefab(ref SystemState s)
    {
        var em = s.EntityManager;

        // Prefer ZombiePrefabRef singleton (if baked via ZombiePrefabAuthoring)
        if (_prefabRefQ.CalculateEntityCount() > 0 &&
            _prefabRefQ.TryGetSingleton(out ZombiePrefabRef pr))
            return pr.Prefab;

        // Fallback: first ZombieSpawner in the world
        using var spawners = _spawnerQ.ToEntityArray(Allocator.Temp);
        if (spawners.Length > 0)
            return em.GetComponentData<ZombieSpawner>(spawners[0]).Prefab;

        throw new System.InvalidOperationException(
            "No prefab source found. Add ZombiePrefabAuthoring (bakes ZombiePrefabRef) or place a ZombieSpawner with a valid Prefab.");
    }
}