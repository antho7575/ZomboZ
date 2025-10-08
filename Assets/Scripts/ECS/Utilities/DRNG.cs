using System.Runtime.CompilerServices;
using Unity.Mathematics;

public static class DRNG
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Hash(uint v)
    {
        v ^= 2747636419u; v *= 2654435769u;
        v ^= v >> 16; v *= 2654435769u;
        v ^= v >> 16; v *= 2654435769u;
        return v;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Hash2(uint a, uint b) => Hash(a ^ (b + 0x9E3779B9u + (a << 6) + (a >> 2)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Rand01(uint s) => (Hash(s) & 0x00FFFFFFu) * (1f / 16777216f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float2 Rand02(uint s)
    {
        uint h = Hash(s);
        return new float2((h & 0xFFFFu) * (1f / 65536f), (h >> 16) * (1f / 65536f));
    }
}
