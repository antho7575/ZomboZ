using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

public static class BlobBuilderUtil
{
    public static BlobHandles BuildAll(
        Dictionary<int, ItemDef> items,
        Dictionary<int, RecipeDef> recipes,
        Dictionary<int, ZombieArchetypeDef> zombies,
        Dictionary<string, BiomeDef> biomes)
    {
        var handles = new BlobHandles();

        // Items
        {
            using var bb = new BlobBuilder(Allocator.Temp);
            ref var root = ref bb.ConstructRoot<ItemDBBlob>();
            var ids = bb.Allocate(ref root.Ids, items.Count);
            int i = 0; foreach (var kv in items) ids[i++] = kv.Key;
            handles.ItemDB = bb.CreateBlobAssetReference<ItemDBBlob>(Allocator.Persistent);
        }

        // Zombies
        {
            using var bb = new BlobBuilder(Allocator.Temp);
            ref var root = ref bb.ConstructRoot<ZombieDBBlob>();
            var ids = bb.Allocate(ref root.Ids, zombies.Count);
            int i = 0; foreach (var kv in zombies) ids[i++] = kv.Key;
            handles.ZombieDB = bb.CreateBlobAssetReference<ZombieDBBlob>(Allocator.Persistent);
        }

        // Loot / Biomes (stubs)
        {
            using var bb = new BlobBuilder(Allocator.Temp);
            ref var root = ref bb.ConstructRoot<LootTableDBBlob>();
            var arr = bb.Allocate(ref root.Dummy, 1); arr[0] = 0;
            handles.LootDB = bb.CreateBlobAssetReference<LootTableDBBlob>(Allocator.Persistent);
        }
        {
            using var bb = new BlobBuilder(Allocator.Temp);
            ref var root = ref bb.ConstructRoot<BiomeDBBlob>();
            var arr = bb.Allocate(ref root.Dummy, 1); arr[0] = 0;
            handles.BiomeDB = bb.CreateBlobAssetReference<BiomeDBBlob>(Allocator.Persistent);
        }

        return handles;
    }
}
