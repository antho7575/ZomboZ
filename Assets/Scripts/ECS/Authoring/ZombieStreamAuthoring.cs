using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
public class ZombieStreamAuthoring : MonoBehaviour
{
    [Header("Prefab to spawn (Entity prefab wrapper)")]
    public GameObject ZombiePrefab;   // assign your ZombieEntity wrapper

    [Header("Spawn Parameters")]
    [Range(1, 1_000_000)]
    public int TotalZombies = 100_000;
    public Vector2 HalfExtents = new(1000, 1000);  // world bounds: [-X..X] x [-Y..Y]
    public uint RandomSeed = 12345;

    [Header("Chunking")]
    public float CellSize = 32f;
    public int VisibleRadiusCells = 6;

    class Baker : Baker<ZombieStreamAuthoring>
    {
        public override void Bake(ZombieStreamAuthoring a)
        {
            var holder = GetEntity(TransformUsageFlags.None);
            var prefab = GetEntity(a.ZombiePrefab, TransformUsageFlags.Dynamic | TransformUsageFlags.Renderable);

            //// Config singleton
            //AddComponent(holder, new ZombieStreamConfig
            //{
            //    Prefab = prefab,
            //    CellSize = a.CellSize,
            //    VisibleRadiusCells = a.VisibleRadiusCells
            //});

            // ---- Generate random positions (managed) and bucket by cell ----
            var rng = new Unity.Mathematics.Random(a.RandomSeed);
            var buckets = new Dictionary<int2, List<float3>>(new Int2Equality());

            for (int i = 0; i < a.TotalZombies; i++)
            {
                float x = rng.NextFloat(-a.HalfExtents.x, a.HalfExtents.x);
                float z = rng.NextFloat(-a.HalfExtents.y, a.HalfExtents.y);
                var p = new float3(x, 0f, z);

                var c = new int2(
                    (int)math.floor(p.x / a.CellSize),
                    (int)math.floor(p.z / a.CellSize));

                if (!buckets.TryGetValue(c, out var list))
                {
                    list = new List<float3>(8);
                    buckets[c] = list;
                }
                list.Add(p);
            }

            // Stable ordering of cells using managed sort
            var cells = new List<int2>(buckets.Keys);
            cells.Sort(Int2Comparer.Instance);

            // Count total positions
            int total = 0;
            for (int i = 0; i < cells.Count; i++) total += buckets[cells[i]].Count;

            // ---- Build blob ----
            using var bb = new BlobBuilder(Allocator.Temp);
            ref var root = ref bb.ConstructRoot<ZombieIndexBlob>();

            var posArr = bb.Allocate(ref root.positions, total);
            var cellArr = bb.Allocate(ref root.uniqueCells, cells.Count);
            var startArr = bb.Allocate(ref root.cellStart, cells.Count);
            var lenArr = bb.Allocate(ref root.cellLen, cells.Count);

            int write = 0;
            for (int i = 0; i < cells.Count; i++)
            {
                var c = cells[i];
                var list = buckets[c];

                cellArr[i] = c;
                startArr[i] = write;
                lenArr[i] = list.Count;

                for (int k = 0; k < list.Count; k++)
                    posArr[write++] = list[k];
            }

            var blobRef = bb.CreateBlobAssetReference<ZombieIndexBlob>(Allocator.Persistent);
            AddBlobAsset(ref blobRef, out _); // ensures proper lifetime in subscene/world
            AddComponent(holder, new ZombieIndexRef { Blob = blobRef });
            AddBuffer<LoadedCell>(holder);     // runtime tracker
        }

        // Dictionary equality for int2
        sealed class Int2Equality : IEqualityComparer<int2>
        {
            public bool Equals(int2 a, int2 b) => a.x == b.x && a.y == b.y;
            public int GetHashCode(int2 v) => (v.x * 73856093) ^ (v.y * 19349663);
        }

        // Sort order for cells (x then y)
        sealed class Int2Comparer : IComparer<int2>
        {
            public static readonly Int2Comparer Instance = new();
            public int Compare(int2 a, int2 b)
                => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y);
        }
    }
}
