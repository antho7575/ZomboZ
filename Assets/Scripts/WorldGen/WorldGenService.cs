using Unity.Mathematics;
using Unity.Entities;

public static class WorldGenService
{
    public static void BuildChunkBase(int2 chunk, uint seed, DynamicBuffer<CellCost> costs, int2 size)
    {
        // Placeholder: simple cross-shaped road with low cost in the middle
        for (int y=0;y<size.y;y++)
        for (int x=0;x<size.x;x++)
        {
            int i = y*size.x + x;
            ushort c = 2;
            if (x == size.x/2 || y == size.y/2) c = 1;
            costs[i] = new CellCost{ Value = c };
        }
    }
}
