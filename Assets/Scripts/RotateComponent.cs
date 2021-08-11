using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

namespace SWE
{
    [GenerateAuthoringComponent]
    public struct RotateComponent : IComponentData
    {
        public float speed;
    }
}
