using System.Collections.Generic;

public static class ModRegistry
{
    public static Dictionary<int, ItemDef> Items = new();
    public static Dictionary<int, RecipeDef> Recipes = new();
    public static Dictionary<int, ZombieArchetypeDef> Zombies = new();
    public static Dictionary<string, BiomeDef> Biomes = new();

    public static BlobHandles Blobs;
}
