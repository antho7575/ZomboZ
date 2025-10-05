//using Unity.Burst;
//using Unity.Entities;
//using Unity.Mathematics;
//using Unity.Transforms;

//[UpdateInGroup(typeof(SimulationSystemGroup))]
//public partial struct UpdateFocusFromPlayerSystem : ISystem
//{
//    EntityQuery _playerQ;

//    public void OnCreate(ref SystemState s)
//    {
//        // Create the focus singleton once
//        var em = s.EntityManager;
//        if (!em.CreateEntityQuery(ComponentType.ReadOnly<FocusPoint>())
//               .TryGetSingletonEntity<FocusPoint>(out _))
//        {
//            var e = em.CreateEntity(typeof(FocusPoint));
//            em.SetComponentData(e, new FocusPoint { Value = float3.zero });
//        }

//        _playerQ = new EntityQueryBuilder(Unity.Collections.Allocator.Temp)
//            .WithAll<PlayerTag, LocalTransform>()
//            .Build(ref s);
//    }

//    [BurstCompile]
//    public void OnUpdate(ref SystemState s)
//    {
//        float3 focus;

//        // Prefer the player position if present
//        if (_playerQ.TryGetSingletonEntity(out var player))
//        {
//            var xf = s.EntityManager.GetComponentData<LocalTransform>(player);
//            focus = xf.Position;
//        }
//        else
//        {
//            // Fallback: camera
//            // (Make sure your CameraPosUploader is running somewhere)
//            focus = SystemAPI.HasSingleton<CameraPositionSingleton>()
//                  ? SystemAPI.GetSingleton<CameraPositionSingleton>().Value
//                  : default;
//        }

//        SystemAPI.SetSingleton(new FocusPoint { Value = focus });
//    }
//}
