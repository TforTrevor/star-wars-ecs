using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

namespace SWE
{
    [GenerateAuthoringComponent]
    public struct PrefabSpawnerComponent : IComponentData
    {
        public Entity prefab;
        public int spawnCount;
        public float spawnRadius;
    }
}