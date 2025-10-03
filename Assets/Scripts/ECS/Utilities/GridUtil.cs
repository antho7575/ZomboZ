using Unity.Mathematics;

public static class GridUtil
{
    public static int Index(int2 p, int2 size) => p.y * size.x + p.x;
    public static bool InBounds(int2 p, int2 size) => (uint)p.x < (uint)size.x && (uint)p.y < (uint)size.y;

    public static int2 WorldToCell(float3 posWS, float2 origin, float cellSize)
    {
        float2 rel = new float2(posWS.x - origin.x, posWS.z - origin.y);
        return (int2)math.floor(rel / cellSize);
    }
}
