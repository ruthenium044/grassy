using System.Collections;
using System.Collections.Generic;
using UnityEngine;
    
[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/GrassData", order = 1)]

public class ScriptableGrassBase : ScriptableObject
{
    [Header("General Settings")]
    [SerializeField] internal Mesh sourceMesh = default;
    [SerializeField] internal ComputeShader grassComputeShader = default;
    [SerializeField] internal Material material = default;
    [SerializeField] [Range(1, 10)] internal int bladeSegments = 5;
    [SerializeField] [Range(1, 100)] internal int bladesPerVertex = 20;
    
    [Header("Blade Size")]
    [SerializeField] internal float grassHeight = 1;
    [SerializeField] internal float grassWidth = 0.2f;
    [SerializeField] internal float grassHeightFactor = 1;
    [SerializeField] internal float grassWidthFactor = 0.2f;

    [Header("Blade Shape")]
    [SerializeField] [Range(0, 1)] internal float bladeForwardAmount = 0.38f;
    [SerializeField] [Range(1, 5)] internal float bladeCurveAmount = 2;
    [SerializeField] [Range(0, 0.5f)] internal float bladeOriginDisplacement = 0.38f;
    
    [Header("Distance LOD")]
    [SerializeField] internal float minLOD = 3;
    [SerializeField] internal float maxLOD = 9;
    [SerializeField] internal float factorLOD = 1;
}
