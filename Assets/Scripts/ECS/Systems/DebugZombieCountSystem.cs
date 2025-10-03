using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;

public partial struct DebugZombieCountSystem : ISystem
{
    bool _logged;
    public void OnUpdate(ref SystemState s)
    {
        if (_logged) return;

        var em = s.EntityManager;
        var q = SystemAPI.QueryBuilder().WithAll<ZombieTag, LocalTransform>().Build();
        int count = q.CalculateEntityCount();
        if (count == 0) return;

        using var ents = q.ToEntityArray(Unity.Collections.Allocator.Temp);
        int sample = math.min(3, ents.Length);
        string msg = $"[Zombies] Spawned: {count}. First {sample} positions: ";
        for (int i = 0; i < sample; i++)
        {
            var p = em.GetComponentData<LocalTransform>(ents[i]).Position;
            msg += $" #{i}:{p}";
        }
        Debug.Log(msg);
        _logged = true;
    }
}
