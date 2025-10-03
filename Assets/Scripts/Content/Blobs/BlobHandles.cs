using Unity.Entities;

public struct BlobHandles
{
    public BlobAssetReference<ItemDBBlob> ItemDB;
    public BlobAssetReference<LootTableDBBlob> LootDB;
    public BlobAssetReference<ZombieDBBlob> ZombieDB;
    public BlobAssetReference<BiomeDBBlob> BiomeDB;
}

// Example root blob structs (minimal placeholders)
public struct ItemDBBlob { public BlobArray<int> Ids; }
public struct LootTableDBBlob { public BlobArray<int> Dummy; }
public struct ZombieDBBlob { public BlobArray<int> Ids; }
public struct BiomeDBBlob { public BlobArray<int> Dummy; }
