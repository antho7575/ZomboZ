using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;

[DisallowMultipleComponent]
public class ZombieStreamAuthoring : MonoBehaviour
{
    [Header("Procedural world")]
    public Vector2 HalfExtents = new(1000, 1000); // world spans [-X..X] x [-Y..Y] meters
    public int TotalCount = 1_000_000;
    public float CellSize = 32f;
    public uint RandomSeed = 1234567u;

    [Header("Streaming")]
    public GameObject ZombiePrefab;
    public int LoadRadiusCells = 6;

    class Baker : Baker<ZombieStreamAuthoring>
    {
        public override void Bake(ZombieStreamAuthoring a)
        {
            // 1) Compute grid
            int2 minCell = new int2(
                (int)math.floor(-a.HalfExtents.x / a.CellSize),
                (int)math.floor(-a.HalfExtents.y / a.CellSize)
            );
            int2 maxCell = new int2(
                (int)math.floor(+a.HalfExtents.x / a.CellSize),
                (int)math.floor(+a.HalfExtents.y / a.CellSize)
            );
            int2 size = maxCell - minCell + new int2(1, 1);
            int cells = size.x * size.y;

            // 2) Generate positions (deterministic)
            var rnd = new Unity.Mathematics.Random(a.RandomSeed == 0 ? 1u : a.RandomSeed);
            var tmpPositions = new NativeArray<float3>(a.TotalCount, Allocator.Temp);

            for (int i = 0; i < a.TotalCount; i++)
            {
                float x = rnd.NextFloat(-a.HalfExtents.x, +a.HalfExtents.x);
                float z = rnd.NextFloat(-a.HalfExtents.y, +a.HalfExtents.y);
                tmpPositions[i] = new float3(x, 0f, z);
            }

            // 3) Count per-cell (first pass)
            var counts = new NativeArray<int>(cells, Allocator.Temp);
            for (int i = 0; i < tmpPositions.Length; i++)
            {
                int2 c = WorldToCell(tmpPositions[i].xz, a.CellSize);
                int idx = CellIndex(c, minCell, size);
                if ((uint)idx < (uint)cells) counts[idx]++; // clamp to grid
            }

            // 4) Prefix sum → starts
            var starts = new NativeArray<int>(cells, Allocator.Temp);
            int running = 0;
            for (int i = 0; i < cells; i++)
            {
                starts[i] = running;
                running += counts[i];
            }
            int totalInGrid = running;

            // 5) Blob build
            var bb = new BlobBuilder(Allocator.Temp);
            ref var root = ref bb.ConstructRoot<ZombieIndexBlob>();
            root.minCoord = minCell;
            root.size = size;
            root.cellSize = a.CellSize;

            var bStarts = bb.Allocate(ref root.cellStart, cells);
            var bLens = bb.Allocate(ref root.cellLen, cells);
            var bPos = bb.Allocate(ref root.positions, totalInGrid);

            // Copy starts & lengths (we’ll reuse counts as write cursors)
            for (int i = 0; i < cells; i++)
            {
                bStarts[i] = starts[i];
                bLens[i] = counts[i];
                counts[i] = 0; // reuse as cursor
            }

            // 6) Fill positions by cell (second pass)
            for (int i = 0; i < tmpPositions.Length; i++)
            {
                int2 c = WorldToCell(tmpPositions[i].xz, a.CellSize);
                int cell = CellIndex(c, minCell, size);
                if ((uint)cell >= (uint)cells) continue; // out of grid
                int write = bStarts[cell] + counts[cell];
                bPos[write] = tmpPositions[i];
                counts[cell]++;
            }

            var blob = bb.CreateBlobAssetReference<ZombieIndexBlob>(Allocator.Persistent);
            bb.Dispose();
            tmpPositions.Dispose();
            counts.Dispose();
            starts.Dispose();

            // 7) Create the streaming singleton with the Blob + prefab
            var e = GetEntity(TransformUsageFlags.None);
            AddComponent(e, new ZombieStreamConfig
            {
                Index = blob,
                Prefab = GetEntity(a.ZombiePrefab, TransformUsageFlags.Renderable | TransformUsageFlags.Dynamic),
                LoadRadiusCells = Mathf.Max(1, a.LoadRadiusCells)
            });
            AddComponent(e, new ZombieStreamState { LastCenterCell = new int2(int.MinValue, int.MinValue) });
            AddBuffer<LoadedCell>(e); // will hold the current loaded set
        }

        static int2 WorldToCell(float2 xz, float cell)
            => new int2((int)math.floor(xz.x / cell), (int)math.floor(xz.y / cell));

        static int CellIndex(int2 c, int2 min, int2 size)
            => (c.x - min.x) + (c.y - min.y) * size.x;
    }
}
