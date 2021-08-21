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

            Random random = new Random((uint)UnityEngine.Random.Range(0, int.MaxValue));

            Entities.ForEach((Entity entity, int entityInQueryIndex, in PrefabSpawnerComponent spawner, in Translation translation) =>
            {
                int rowLength = (int)math.floor(math.sqrt(spawner.spawnCount));
                for (int i = 0; i < spawner.spawnCount; i++)
                {
                    Entity spawnedEntity = commandBuffer.Instantiate(entityInQueryIndex, spawner.prefab);

                    commandBuffer.SetComponent(entityInQueryIndex, spawnedEntity, new Rotation
                    {
                        Value = quaternion.EulerXYZ(random.NextFloat3())
                    });

                    commandBuffer.SetComponent(entityInQueryIndex, spawnedEntity, new Translation
                    {
                        Value = new float3(random.NextFloat3(-spawner.spawnRadius, spawner.spawnRadius))
                    });
                }

                commandBuffer.DestroyEntity(entityInQueryIndex, entity);
            }).ScheduleParallel();

            commandBufferSystem.AddJobHandleForProducer(Dependency);

            Dependency.Complete();
        }
    }
}
