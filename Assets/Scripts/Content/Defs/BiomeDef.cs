[System.Serializable]
public class BiomeDef { public string id; public string[] buildingSets; public LootTableDef[] lootTables; }
[System.Serializable]
public class LootTableDef { public int[] itemIds; public float[] weights; }
