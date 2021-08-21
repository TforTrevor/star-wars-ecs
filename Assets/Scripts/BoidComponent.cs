using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace SWE
{
    [GenerateAuthoringComponent]
    public struct BoidComponent : IComponentData
    {
        [Header("Runtime")]
        public float3 velocity;
        public float3 target;
        public bool hasTarget;
        //public float3 avgFlockHeading;
        //public float3 avgAvoidanceHeading;
        //public float3 centreOfFlockmates;
        //public int numPerceivedFlockmates;
    }
}
