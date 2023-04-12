using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.VisualScripting;
using UnityEngine;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine.Serialization;

[ExecuteInEditMode]
public class CompteShaderTest : MonoBehaviour
{
    [SerializeField] private ScriptableGrassBase grassData;
    [SerializeField] internal Transform obj;
    
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct SourceVertex {
        public Vector3 position;
        public Vector3 normal;
        public Vector2 uv;
    }
    
    private bool initialized;
    private ComputeBuffer sourceVertBuffer;
    private ComputeBuffer sourceTriBuffer;
    private ComputeBuffer drawBuffer;
    private ComputeBuffer argsBuffer;

    private ComputeShader instantiatedComputeShader;
    private Material instantiatedMaterial;

    private int idKarnel;
    private int dispatchSize;

    private Bounds localBounds;

    private static readonly int SOURCE_VERT_STRIDE = UnsafeUtility.SizeOf<SourceVertex>();
    private const int SOURCE_TRI_STRIDE = sizeof(int);
    private const int DRAW_STRIDE = sizeof(float) * (3 + (3 + 2) * 3);
    private const int ARGS_STRIDE = sizeof(int) * 4;

     private void OnEnable() {
         //Should be assert or not?
         if (grassData.grassComputeShader == null || grassData.sourceMesh == null)
         {
             return;
         }
         
         if(initialized) {
            OnDisable();
        }
        initialized = true;

        instantiatedComputeShader = Instantiate(grassData.grassComputeShader);
        instantiatedMaterial = Instantiate(grassData.material);
        
        Vector3[] positions = grassData.sourceMesh.vertices;
        Vector3[] normals = grassData.sourceMesh.normals;
        Vector2[] uvs = grassData.sourceMesh.uv;
        int[] tris = grassData.sourceMesh.triangles;
        SourceVertex[] vertices = new SourceVertex[positions.Length];

        Bounds bounds = new Bounds();
        
        for(int i = 0; i < vertices.Length; i++) {
            vertices[i] = new SourceVertex() {
                position = positions[i],
                normal = normals[i],
                uv = uvs[i]
            };
            bounds.Encapsulate(positions[i]);
        }
        int numTriangles = tris.Length / 3;
        int maxBladeTriangles = (grassData.bladeSegments - 1) * 2 + 1;

        sourceVertBuffer = new ComputeBuffer(vertices.Length, SOURCE_VERT_STRIDE, ComputeBufferType.Structured, ComputeBufferMode.Immutable);
        sourceVertBuffer.SetData(vertices);
        sourceTriBuffer = new ComputeBuffer(tris.Length, SOURCE_TRI_STRIDE, ComputeBufferType.Structured, ComputeBufferMode.Immutable);
        sourceTriBuffer.SetData(tris);
        drawBuffer = new ComputeBuffer(numTriangles * 3 * grassData.bladesPerVertex * maxBladeTriangles, DRAW_STRIDE, ComputeBufferType.Append);
        drawBuffer.SetCounterValue(0);
        argsBuffer = new ComputeBuffer(1, ARGS_STRIDE, ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(new int[] { 0, 1, 0, 0 });

        idKarnel = instantiatedComputeShader.FindKernel("CSMain");
       
        instantiatedComputeShader.SetBuffer(idKarnel, "_SourceVertices", sourceVertBuffer);
        instantiatedComputeShader.SetBuffer(idKarnel, "_SourceTriangles", sourceTriBuffer);
        instantiatedComputeShader.SetBuffer(idKarnel, "_DrawTriangles", drawBuffer);
        instantiatedComputeShader.SetBuffer(idKarnel, "_IndirectArgsBuffer", argsBuffer);

        //set vertex data
        instantiatedComputeShader.SetInt("_NumSourceTriangles", numTriangles);
        instantiatedComputeShader.SetInt("_SegmentsPerBlade", Mathf.Max(1,grassData.bladeSegments));
        instantiatedComputeShader.SetInt("_BladesPerVertex", Mathf.Max(1, grassData.bladesPerVertex));
        
        instantiatedComputeShader.SetFloat("_GrassHeight", grassData.grassHeight);
        instantiatedComputeShader.SetFloat("_GrassWidth", grassData.grassWidth);
        instantiatedComputeShader.SetFloat("_GrassHeightFactor", grassData.grassHeightFactor);
        instantiatedComputeShader.SetFloat("_GrassWidthFactor", grassData.grassWidthFactor);
        
        instantiatedComputeShader.SetFloat("_BladeForward", grassData.bladeForwardAmount);
        instantiatedComputeShader.SetFloat("_BladeCurve", Mathf.Max(0, grassData.bladeCurveAmount));
        instantiatedComputeShader.SetFloat("_OriginDisplacement", grassData.bladeOriginDisplacement);
        
        instantiatedComputeShader.SetVector("_CameraLOD", new Vector4(grassData.minLOD, grassData.maxLOD, Mathf.Max(0, grassData.factorLOD), 0));

        instantiatedMaterial.SetBuffer("_DrawTriangles", drawBuffer);

        // Calculate the number of threads to use. Get the thread size from the kernel
        // Then, divide the number of triangles by that size
        instantiatedComputeShader.GetKernelThreadGroupSizes(idKarnel, out uint threadGroupSize, out _, out _);
        dispatchSize = Mathf.CeilToInt((float)numTriangles / threadGroupSize);

        localBounds = grassData.sourceMesh.bounds;
        localBounds.Expand(1);
     }

    private void OnDisable() {
        if(initialized) {
            if (Application.isPlaying)
            {
                Destroy(instantiatedComputeShader);
                Destroy(instantiatedMaterial);
            }
            else
            {
                DestroyImmediate(instantiatedComputeShader);
                DestroyImmediate(instantiatedMaterial);
            }
            
            sourceVertBuffer.Release();
            sourceTriBuffer.Release();
            drawBuffer.Release();
            argsBuffer.Release();
        }
        initialized = false;
    }

    private void LateUpdate() {
        if (Application.isPlaying == false)
        {
            OnDisable();
            OnEnable();
        }
        drawBuffer.SetCounterValue(0);
        argsBuffer.SetData(new int[] { 0, 1, 0, 0 });

        Bounds bounds = TransformBounds(localBounds);

        instantiatedComputeShader.SetMatrix("_LocalToWorld", transform.localToWorldMatrix);
        instantiatedComputeShader.SetFloat("_Height", grassData.grassHeight);
        instantiatedComputeShader.SetVector("_CameraPos", obj.position);
        
        instantiatedComputeShader.Dispatch(idKarnel, dispatchSize, 1, 1);
  
        Graphics.DrawProceduralIndirect(instantiatedMaterial, bounds, MeshTopology.Triangles, argsBuffer);
    }
     
    // This applies the game object's transform to the local bounds
    // Code by benblo from https://answers.unity.com/questions/361275/cant-convert-bounds-from-world-coordinates-to-loca.html
    public Bounds TransformBounds(Bounds boundsOS) {
        var center = transform.TransformPoint(boundsOS.center);

        // transform the local extents' axes
        var extents = boundsOS.extents;
        var axisX = transform.TransformVector(extents.x, 0, 0);
        var axisY = transform.TransformVector(0, extents.y, 0);
        var axisZ = transform.TransformVector(0, 0, extents.z);

        // sum their absolute value to get the world extents
        extents.x = Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x);
        extents.y = Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y);
        extents.z = Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z);

        return new Bounds { center = center, extents = extents };
    }
}
