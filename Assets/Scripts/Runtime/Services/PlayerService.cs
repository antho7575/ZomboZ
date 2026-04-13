using Unity.Mathematics;
using UnityEngine;

namespace ZomboZ.Runtime
{
    /// <summary>
    /// Simple helper to get player/camera position.
    /// </summary>
    public static class PlayerService
    {
        /// <summary>
        /// Gets the player/camera position. Just uses Camera.main - simple!
        /// </summary>
        public static float3 GetPlayerPosition()
        {
            var cam = Camera.main;
            if (cam != null)
            {
                var p = cam.transform.position;
                return new float3(p.x, p.y, p.z);
            }

            // Fallback: try to find by tag
            var playerGo = GameObject.FindWithTag("Player");
            if (playerGo != null)
            {
                var p = playerGo.transform.position;
                return new float3(p.x, p.y, p.z);
            }

            return float3.zero;
        }
    }
}
