using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Jobs;

namespace SWE
{
    public class PrefabSpawnerSystem : SystemBase
    {
        BeginInitializationEntityCommandBufferSystem commandBufferSystem;

        protected override void OnCreate()
        {
            commandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var commandBuffer = commandBufferSystem.CreateCommandBuffer().AsParallelWriter();

            Entities.ForEach((Entity entity, int entityInQueryIndex, ref PrefabSpawnerComponent spawner, in Translation translation) =>
            {
                if (!spawner.disabled)
                {
                    int rowLength = (int)math.floor(math.sqrt(spawner.spawnCount));
                    for (int i = 0; i < spawner.spawnCount; i++)
                    {
                        Entity spawnedEntity = commandBuffer.Instantiate(entityInQueryIndex, spawner.prefab);
                        commandBuffer.SetComponent(entityInQueryIndex, spawnedEntity, new Translation
                        {
                            Value = new float3(i % rowLength, 0, i / rowLength) + translation.Value
                        });
                    }
                    spawner.disabled = true;
                }
            }).ScheduleParallel();

            commandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}
