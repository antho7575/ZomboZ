using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System;
using UnityEngine;

public static class ModRegistryLoader
{
    public static async Task LoadAllAsync(string modsRoot)
    {
        // Clear registries
        ModRegistry.Items.Clear();
        ModRegistry.Recipes.Clear();
        ModRegistry.Zombies.Clear();
        ModRegistry.Biomes.Clear();

        await Task.Run(() =>
        {
            if (!Directory.Exists(modsRoot)) return;

            foreach (var file in Directory.EnumerateFiles(modsRoot, "*.json", SearchOption.AllDirectories))
            {
                try
                {
                    var name = Path.GetFileName(file).ToLowerInvariant();
                    var text = File.ReadAllText(file);

                    if (name.Contains("items"))
                    {
                        var arr = JsonUtility.FromJson<ItemDefArray>(WrapArray("items", text));
                        if (arr?.items != null)
                            foreach (var d in arr.items) ModRegistry.Items[d.id] = d;
                    }
                    else if (name.Contains("recipes"))
                    {
                        var arr = JsonUtility.FromJson<RecipeDefArray>(WrapArray("recipes", text));
                        if (arr?.recipes != null)
                            foreach (var d in arr.recipes) ModRegistry.Recipes[d.resultId] = d;
                    }
                    else if (name.Contains("zombies"))
                    {
                        var arr = JsonUtility.FromJson<ZombieArchetypeArray>(WrapArray("zombies", text));
                        if (arr?.zombies != null)
                            foreach (var d in arr.zombies) ModRegistry.Zombies[d.id] = d;
                    }
                    else if (file.ToLowerInvariant().Contains("/biomes/"))
                    {
                        var def = JsonUtility.FromJson<BiomeDef>(text);
                        if (def != null && !string.IsNullOrEmpty(def.id))
                            ModRegistry.Biomes[def.id] = def;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Mod parse failed: {file}\n{ex}");
                }
            }
        });

        // Build blobs for ECS hot paths (stub implementation here)
        ModRegistry.Blobs = BlobBuilderUtil.BuildAll(ModRegistry.Items, ModRegistry.Recipes, ModRegistry.Zombies, ModRegistry.Biomes);
    }

    // Helper to wrap top-level arrays for JsonUtility
    private static string WrapArray(string keyName, string rawText)
    {
        string trimmed = rawText.TrimStart();
        if (trimmed.StartsWith("["))
        {
            // If the JSON is a top-level array, wrap it into an object
            return "{ \"" + keyName + "\": " + rawText + " }";
        }
        return rawText; // Already an object
    }


    // JsonUtility helpers for arrays
    [System.Serializable] public class ItemDefArray { public ItemDef[] items; }
    [System.Serializable] public class RecipeDefArray { public RecipeDef[] recipes; }
    [System.Serializable] public class ZombieArchetypeArray { public ZombieArchetypeDef[] zombies; }

}
