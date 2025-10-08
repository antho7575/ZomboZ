using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct WorldGenSystem : ISystem
{
    public void OnCreate(ref SystemState s)
    {
        s.RequireForUpdate<WorldGenConfig>();
        s.RequireForUpdate<BiomeWeight>();
        s.RequireForUpdate<WorldGenRequest>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState s)
    {
        var cfg = SystemAPI.GetSingleton<WorldGenConfig>();

        // Get the entity that holds the singleton config + buffer
        var cfgEntity = SystemAPI.GetSingletonEntity<WorldGenConfig>();

        // FIX: use TempJob so this NativeArray can be captured by scheduled jobs
        var weights = SystemAPI.GetBuffer<BiomeWeight>(cfgEntity)
                       .ToNativeArray(Allocator.TempJob);


        int2 gridSize = cfg.Size switch
        {
            MapSizePreset.Small => new int2(256, 256),
            MapSizePreset.Medium => new int2(512, 512),
            MapSizePreset.Large => new int2(1024, 1024),
            _ => new int2(512, 512)
        };

        // Build working arrays
        var heights = new NativeArray<float>(gridSize.x * gridSize.y, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        var moisture = new NativeArray<float>(gridSize.x * gridSize.y, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        var biome = new NativeArray<byte>(gridSize.x * gridSize.y, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        var flags = new NativeArray<byte>(gridSize.x * gridSize.y, Allocator.TempJob, NativeArrayOptions.ClearMemory);

        // Pass 1: noise fields
        var noiseJob = new NoiseJob
        {
            size = gridSize,
            cellSz = cfg.CellSize,
            seed = cfg.Seed,
            hFreq = cfg.HeightFreq,
            mFreq = cfg.MoistureFreq,
            heights = heights,
            moist = moisture
        }.Schedule(gridSize.x * gridSize.y, 128, s.Dependency);

        // Pass 2: biome classify (simple weighted thresholds)
        var classifyJob = new ClassifyJob
        {
            size = gridSize,
            heights = heights,
            moist = moisture,
            biomeOut = biome,
            weights = weights
        }.Schedule(heights.Length, 128, noiseJob);

        classifyJob.Complete(); // we need the data to build the Blob

        // Build blob
        var bb = new BlobBuilder(Allocator.Temp);
        ref var root = ref bb.ConstructRoot<MapGridBlob>();
        root.size = gridSize;
        root.cellSize = cfg.CellSize;

        var bh = bb.Allocate(ref root.height, heights.Length);
        var bm = bb.Allocate(ref root.moisture, moisture.Length);
        var bbio = bb.Allocate(ref root.biome, biome.Length);
        var bfl = bb.Allocate(ref root.flags, flags.Length);

        // ❌ Don’t do FixedStringMethods.CopyFrom(ref bh, heights) etc.
        // ✅ Simple loops (Burst-friendly)
        for (int i = 0; i < heights.Length; i++) bh[i] = heights[i];
        for (int i = 0; i < moisture.Length; i++) bm[i] = moisture[i];
        for (int i = 0; i < biome.Length; i++) bbio[i] = biome[i];
        for (int i = 0; i < flags.Length; i++) bfl[i] = flags[i];

        var blob = bb.CreateBlobAssetReference<MapGridBlob>(Allocator.Persistent);
        bb.Dispose();


        heights.Dispose();
        moisture.Dispose();
        biome.Dispose();
        flags.Dispose();
        weights.Dispose();

        // Publish singleton (create or update)
        var em = s.EntityManager;
        Entity gridE;
        var q = em.CreateEntityQuery(ComponentType.ReadWrite<MapGridSingleton>());
        if (!q.TryGetSingletonEntity<MapGridSingleton>(out gridE))
        {
            gridE = em.CreateEntity(typeof(MapGridSingleton));
        }
        else
        {
            var prev = em.GetComponentData<MapGridSingleton>(gridE);
            if (prev.Grid.IsCreated) prev.Grid.Dispose();
        }
        em.SetComponentData(gridE, new MapGridSingleton { Grid = blob });

        // Done: remove request so we don't regenerate every frame
        var reqQ = em.CreateEntityQuery(ComponentType.ReadOnly<WorldGenRequest>());
        em.RemoveComponent<WorldGenRequest>(reqQ);
        reqQ.Dispose();
    }

    // ----------------- Jobs -----------------

    [BurstCompile]
    struct NoiseJob : IJobParallelFor
    {
        public int2 size;
        public float cellSz;
        public uint seed;
        public float hFreq, mFreq;
        public NativeArray<float> heights;
        public NativeArray<float> moist;

        public void Execute(int i)
        {
            int x = i % size.x;
            int y = i / size.x;

            float2 p = new float2(x * cellSz, y * cellSz);
            float h = FractalValueNoise(p * hFreq, seed ^ 0xA53u, 4);
            float m = FractalValueNoise(p * mFreq, seed ^ 0x19Eu, 3);

            heights[i] = math.saturate(h);
            moist[i] = math.saturate(m);
        }

        // Fast, Burst-friendly fractal value noise
        static float FractalValueNoise(float2 p, uint seed, int oct)
        {
            float a = 0f, amp = 0.5f, freq = 1f;
            for (int o = 0; o < oct; o++)
            {
                a += amp * ValueNoise(p * freq, seed + (uint)o * 1315423911u);
                amp *= 0.5f; freq *= 2f;
            }
            return a;
        }
        static float ValueNoise(float2 p, uint seed)
        {
            int2 ip = (int2)math.floor(p);
            float2 f = math.frac(p);

            float v00 = Hash01(ip + new int2(0, 0), seed);
            float v10 = Hash01(ip + new int2(1, 0), seed);
            float v01 = Hash01(ip + new int2(0, 1), seed);
            float v11 = Hash01(ip + new int2(1, 1), seed);

            float2 u = f * f * (3f - 2f * f); // smoothstep
            return math.lerp(math.lerp(v00, v10, u.x), math.lerp(v01, v11, u.x), u.y);
        }
        static float Hash01(int2 p, uint seed)
        {
            uint h = (uint)p.x * 374761393u + (uint)p.y * 668265263u + seed * 2246822519u;
            h = (h ^ (h >> 13)) * 1274126177u;
            return (h ^ (h >> 16)) * (1.0f / 4294967295.0f);
        }
    }

    [BurstCompile]
    struct ClassifyJob : IJobParallelFor
    {
        public int2 size;
        [ReadOnly] public NativeArray<float> heights;
        [ReadOnly] public NativeArray<float> moist;
        public NativeArray<byte> biomeOut;

        [ReadOnly]
        public NativeArray<BiomeWeight> weights;

        public void Execute(int i)
        {
            float h = heights[i];
            float m = moist[i];

            // Simple 2D partition + water clamp
            // You can tune rules later or switch to Voronoi regions per-biome.
            BiomeId b;
            if (h < 0.28f) b = BiomeId.Water;
            else
            {
                // map to 4 land biomes by moisture & height
                if (m > 0.6f && h > 0.6f) b = BiomeId.Snow;    // cold & wet at high height
                else if (m > 0.5f) b = BiomeId.Forest;  // wetter
                else if (m < 0.25f) b = BiomeId.Desert;  // dryer
                else b = BiomeId.Plains;  // default
            }

            // Optional: bias toward authoring weights (soft, not hard quotas)
            // shift by small additive prior based on weights
            float2 key = new float2(h, m);
            b = BiasByWeights(b, key, weights);

            biomeOut[i] = (byte)b;
        }

        static BiomeId BiasByWeights(BiomeId current, float2 key, NativeArray<BiomeWeight> w)
        {
            // Very light bias: if chosen biome has very low weight compared to others nearby,
            // flip to closest-alternate once in a while.
            float curW = 0.2f;
            for (int i = 0; i < w.Length; i++) if (w[i].Id == current) { curW = w[i].Weight; break; }
            if (curW >= 0.05f) return current;

            // pick an alternate based on weights (simple roulette)
            float r = (Hash01((int)(key.x * 10000f), (int)(key.y * 10000f)) * 0.9999f);
            float acc = 0f;
            for (int i = 0; i < w.Length; i++)
            {
                acc += w[i].Weight;
                if (r <= acc) return w[i].Id;
            }
            return current;
        }
        static float Hash01(int x, int y)
        {
            uint h = (uint)x * 374761393u + (uint)y * 668265263u + 2246822519u;
            h = (h ^ (h >> 13)) * 1274126177u;
            return (h ^ (h >> 16)) * (1.0f / 4294967295.0f);
        }
    }
}
