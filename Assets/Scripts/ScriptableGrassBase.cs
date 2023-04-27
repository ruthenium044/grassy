using System.Collections;
using System.Collections.Generic;
using UnityEngine;
    
[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/GrassData", order = 1)]

public class ScriptableGrassBase : ScriptableObject
{
    [Header("General Settings")]
    [SerializeField] internal ComputeShader grassComputeShader = default;
    [SerializeField] internal Material material = default;
    [SerializeField] [Range(1, 10)] internal int bladeSegments = 5;
    [SerializeField] [Range(1, 100)] internal int bladesPerVertex = 20;
    
    [Header("Material")]
    [SerializeField] internal bool useTexture = false;
    [SerializeField] internal Texture mainTexture = default;
    [SerializeField] internal bool blendWithFloor = false;
    [SerializeField] internal Color topColor = new Color(1, 1, 1);
    [SerializeField] internal Color bottomColor = new Color(0, 1, 0);
    
    [SerializeField] internal Color longGrassTint = new Color(1, 1, 1);
    [SerializeField] internal Color shortGrassTint = new Color(0, 0, 0);
    
    [Header("Blade Size")]
    [SerializeField] internal float grassHeight = 1;
    [SerializeField] internal float grassHeightFactor = 1;
    [SerializeField] internal float grassWidth = 0.2f;
    [SerializeField] internal float grassWidthFactor = 0.2f;

    [Header("Blade Shape")]
    [SerializeField] [Range(0, 1)] internal float bladeForwardAmount = 0.38f;
    [SerializeField] [Range(1, 15)] internal float bladeCurveAmount = 2;
    [SerializeField] [Range(0, 0.5f)] internal float bladeOriginDisplacement = 0.38f;

    [Header("Wind")] 
    [SerializeField] internal float windSpeed = 5.0f;
    [SerializeField] internal float windScale = 2.0f;
    [SerializeField] [Range(0, 1.0f)] internal float windBendStrength = 0.2f;

    [Header("Interaction")] 
    [SerializeField] internal float displacementRadius;

    [Header("Distance LOD")] 
    [SerializeField] internal bool fadeInEditor = false;
    [SerializeField] internal float minLOD = 3;
    [SerializeField] internal float maxLOD = 9;
    [SerializeField] internal float factorLOD = 1;
}
