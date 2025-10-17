using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(PresentationSystemGroup), OrderFirst = true)]
public partial struct ZombieAnimateSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        //// 1️⃣ Only create GameObjects when missing
        //var ecb = new EntityCommandBuffer(Allocator.Temp);
        //foreach (var (prefab, entity) in
        //         SystemAPI.Query<ZombieGameObjectPrefab>().WithNone<ZombieAnimatorReference>().WithEntityAccess())
        //{
        //    var go = Object.Instantiate(prefab.Value);
        //    var anim = go.GetComponent<Animator>();
        //    ecb.AddComponent(entity, new ZombieAnimatorReference { Value = anim });
        //}
        //ecb.Playback(state.EntityManager);
        //ecb.Dispose();

        //// 2️⃣ Only update changed anim states
        //foreach (var (transform, animatorRef, animState) in
        //         SystemAPI.Query<LocalTransform, ZombieAnimatorReference, AnimState>()
        //         .WithChangeFilter<AnimState>())
        //{
        //    var anim = animatorRef.Value;
        //    if (anim == null) continue;

        //    anim.SetFloat("Speed", animState.Speed);
        //    var t = anim.transform;
        //    t.SetPositionAndRotation(transform.Position, transform.Rotation);
        //}
    }
}
