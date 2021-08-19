using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;

namespace SWE
{
    public class BoidSystem : SystemBase
    {
        //protected override void OnStartRunning()
        //{
        //    BoidSettingsComponent settings = GetSingleton<BoidSettingsComponent>();

        //    EntityQuery targetQuery = GetEntityQuery(ComponentType.ReadOnly<TargetTag>(), ComponentType.ReadOnly<Translation>());
        //    NativeArray<Translation> targetTranslations = targetQuery.ToComponentDataArray<Translation>(Allocator.TempJob);

        //    Random random = new Random((uint)UnityEngine.Random.Range(0, int.MaxValue));

        //    Entities.ForEach((ref BoidComponent boid, in Rotation rotation) =>
        //    {
        //        float startSpeed = (settings.minSpeed + settings.maxSpeed) / 2;
        //        boid.velocity = math.forward(rotation.Value) * startSpeed;
        //        boid.target = targetTranslations[random.NextInt(0, targetTranslations.Length)].Value;
        //    })
        //    .WithReadOnly(targetTranslations)
        //    .WithoutBurst()
        //    .ScheduleParallel();

        //    Dependency.Complete();

        //    targetTranslations.Dispose();
        //}

        protected override void OnUpdate()
        {
            BoidSettingsComponent settings = GetSingleton<BoidSettingsComponent>();
            EntityQuery boidQuery = GetEntityQuery(ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<Rotation>(), ComponentType.ReadOnly<BoidComponent>());

            NativeMultiHashMap<uint, int> cellHashMap = new NativeMultiHashMap<uint, int>(boidQuery.CalculateEntityCount(), Allocator.TempJob);

            var parallelHashMap = cellHashMap.AsParallelWriter();
            Entities.ForEach((int entityInQueryIndex, ref BoidComponent boid, in Translation translation) =>
            {
                uint hash = math.hash(new int3(math.floor(translation.Value / settings.cellRadius)));
                parallelHashMap.Add(hash, entityInQueryIndex);
                boid.cellHash = hash;
            })
            .ScheduleParallel();

            Dependency.Complete();

            NativeArray<Translation> translations = boidQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
            NativeArray<Rotation> rotations = boidQuery.ToComponentDataArray<Rotation>(Allocator.TempJob);

            EntityQuery targetQuery = GetEntityQuery(ComponentType.ReadOnly<TargetTag>(), ComponentType.ReadOnly<Translation>());
            NativeArray<Translation> targetTranslations = targetQuery.ToComponentDataArray<Translation>(Allocator.TempJob);

            float deltaTime = UnityEngine.Time.deltaTime;
            Random random = new Random((uint)UnityEngine.Random.Range(0, int.MaxValue));

            Entities.ForEach((int entityInQueryIndex, ref Translation translation, ref Rotation rotation, ref BoidComponent boid) =>
            {
                float3 acceleration = float3.zero;

                if (!boid.hasTarget || math.distance(translation.Value, boid.target) < 5)
                {
                    boid.target = targetTranslations[random.NextInt(0, targetTranslations.Length)].Value;
                    boid.hasTarget = true;
                }
                float3 offsetToTarget = boid.target - translation.Value;
                acceleration = SteerTowards(offsetToTarget, settings.maxSpeed, boid.velocity, settings.maxSteerForce) * settings.targetWeight;

                //if (targetTranslations.Length > 0)
                //{
                //    Translation closest = targetTranslations[0];
                //    for (int i = 0; i < targetTranslations.Length; i++)
                //    {
                //        if (math.distance(translation.Value, targetTranslations[i].Value) < math.distance(translation.Value, closest.Value))
                //        {
                //            closest = targetTranslations[i];
                //        }
                //    }
                //    float3 offsetToTarget = closest.Value - translation.Value;
                //    acceleration = SteerTowards(offsetToTarget, boid.maxSpeed, boid.velocity, boid.maxSteerForce) * boid.targetWeight;
                //}

                float3 flockHeading = float3.zero;
                float3 flockCenter = float3.zero;
                float3 separationHeading = float3.zero;
                int numFlockmates = 0;

                NativeArray<uint> cellHashes = new NativeArray<uint>(27, Allocator.Temp);
                int3 cellIndex = new int3(math.floor(translation.Value / settings.cellRadius));
                for (int x = 0; x < 3; x++)
                {
                    for (int y = 0; y < 3; y++)
                    {
                        for (int z = 0; z < 3; z++)
                        {
                            int index = x + 3 * (y + 3 * z);
                            int3 neighborCell = new int3(cellIndex.x + (x - 1), cellIndex.y + (y - 1), cellIndex.z + (z - 1));
                            cellHashes[index] = math.hash(neighborCell);
                            //float3 translationOffset = new float3((x - 1) * settings.cellRadius, (y - 1) * settings.cellRadius, (z - 1) * settings.cellRadius);
                            //cellHashes[index] = CellHash(translationOffset + translation.Value, settings.cellRadius);
                        }
                    }
                }

                for (int i = 0; i < cellHashes.Length; i++)
                {
                    int queryIndex;
                    NativeMultiHashMapIterator<uint> iterator;
                    if (cellHashMap.TryGetFirstValue(cellHashes[i], out queryIndex, out iterator))
                    {
                        do
                        {
                            if (entityInQueryIndex != queryIndex)
                            {
                                float3 offset = translations[queryIndex].Value - translation.Value;
                                float distance = math.distance(translations[queryIndex].Value, translation.Value);

                                if (distance < settings.perceptionRadius)
                                {
                                    numFlockmates += 1;
                                    flockHeading += math.forward(rotations[queryIndex].Value);
                                    flockCenter += translations[queryIndex].Value;

                                    if (distance < settings.avoidanceRadius)
                                    {
                                        separationHeading -= math.normalizesafe(offset) / distance;
                                    }
                                }
                            }                            
                        }
                        while (cellHashMap.TryGetNextValue(out queryIndex, ref iterator));
                    }
                }

                cellHashes.Dispose();

                if (numFlockmates > 0)
                {
                    float3 averageFlockHeading = flockHeading / numFlockmates;
                    flockCenter /= numFlockmates;
                    float3 averageSeparationHeading = separationHeading / numFlockmates;

                    float3 offsetToFlockCenter = flockCenter - translation.Value;

                    float3 alignmentForce = SteerTowards(averageFlockHeading, settings.maxSpeed, boid.velocity, settings.maxSteerForce) * settings.alignWeight;
                    float3 cohesionForce = SteerTowards(offsetToFlockCenter, settings.maxSpeed, boid.velocity, settings.maxSteerForce) * settings.cohesionWeight;
                    float3 separationForce = SteerTowards(averageSeparationHeading, settings.maxSpeed, boid.velocity, settings.maxSteerForce) * settings.seperateWeight;

                    acceleration += alignmentForce;
                    acceleration += cohesionForce;
                    acceleration += separationForce;
                }

                boid.velocity += acceleration * deltaTime;
                float speed = math.length(boid.velocity);
                float3 direction = math.normalizesafe(boid.velocity);
                speed = math.clamp(speed, settings.minSpeed, settings.maxSpeed);
                boid.velocity = direction * speed;

                rotation.Value = quaternion.LookRotation(direction, math.up());

                translation.Value += boid.velocity * deltaTime;
            })
            .WithReadOnly(targetTranslations)
            .WithReadOnly(cellHashMap)
            .WithReadOnly(translations)
            .WithReadOnly(rotations)
            .ScheduleParallel();

            Dependency.Complete();

            targetTranslations.Dispose();
            translations.Dispose();
            rotations.Dispose();
            cellHashMap.Dispose();
        }

        static float3 SteerTowards(float3 vector, float maxSpeed, float3 velocity, float maxSteerForce)
        {
            float3 v = math.normalizesafe(vector) * maxSpeed - velocity;
            return math.normalizesafe(v) * math.clamp(math.length(v), -maxSteerForce, maxSteerForce);
        }
    }
}
