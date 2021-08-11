using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace SWE
{
    public class RotateSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            float deltaTime = Time.DeltaTime;

            Entities.ForEach((ref Rotation rotation, in RotateComponent rotate) =>
            {
                rotation.Value = math.mul(rotation.Value, quaternion.RotateY(math.radians(rotate.speed) * deltaTime));
            }).ScheduleParallel();
        }
    }
}
