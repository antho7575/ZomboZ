using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

[BurstCompile]
public partial struct ChunkStreamingSystem : ISystem
{
    public void OnUpdate(ref SystemState s)
    {
        // Placeholder: decide which chunks to keep loaded around player,
        // then call SaveBackend.Driver to load overrides/entities for those chunks.
    }
}
