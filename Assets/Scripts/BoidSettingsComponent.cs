using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

namespace SWE
{
    [GenerateAuthoringComponent]
    public struct BoidSettingsComponent : IComponentData
    {
        public float minSpeed;
        public float maxSpeed;
        public float perceptionRadius;
        public float avoidanceRadius;
        public float maxSteerForce;

        public float alignWeight;
        public float cohesionWeight;
        public float seperateWeight;
        public float targetWeight;

        public float cellRadius;

        //[Header("Collisions")]
        //public LayerMask obstacleMask;
        //public float boundsRadius;
        //public float avoidCollisionWeight;
        //public float collisionAvoidDst;
    }
}
