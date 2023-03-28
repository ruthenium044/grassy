using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.VisualScripting;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Serialization;

[ExecuteInEditMode]
public class CompteShaderTest : MonoBehaviour
{
    [SerializeField] private Mesh sourceMesh = default;
    [SerializeField] private ComputeShader grassComputeShader = default;
    [SerializeField] private Material material = default;
    [SerializeField] [Range(1, 10)] private int bladeSegments;
    [SerializeField] [Range(1, 500)] private int bladesPerVertex;
    
    [Header("Blade Size")]
    [SerializeField] private float grassHeight = 1;
    [SerializeField] private float grassWidth = 0.2f;
    [SerializeField] private float grassHeightFactor = 1;
    [SerializeField] private float grassWidthFactor = 0.2f;

    [Header("Blade Shape")]
    [SerializeField] [Range(0, 1)] private float bladeForwardAmount = 0.38f;
    [SerializeField] [Range(1, 5)] private float bladeCurveAmount = 2;
    [SerializeField] [Range(0, 0.5f)] private float bladeOriginiDisplacement = 0.38f;
    
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

    private int idKarnel;
    private int dispatchSize;

    private Bounds localBounds;

    private static readonly int SOURCE_VERT_STRIDE = UnsafeUtility.SizeOf<SourceVertex>();
    private const int SOURCE_TRI_STRIDE = sizeof(int);
    private const int DRAW_STRIDE = sizeof(float) * (3 + (3 + 2) * 3);
    private const int ARGS_STRIDE = sizeof(int) * 4;

     private void OnEnable() {
         //Should be assert or not?
         if (grassComputeShader == null || sourceMesh == null)
         {
             return;
         }
         
         if(initialized) {
            OnDisable();
        }
        initialized = true;
        
        Vector3[] positions = sourceMesh.vertices;
        Vector3[] normals = sourceMesh.normals;
        Vector2[] uvs = sourceMesh.uv;
        int[] tris = sourceMesh.triangles;
        
        SourceVertex[] vertices = new SourceVertex[positions.Length];
        for(int i = 0; i < vertices.Length; i++) {
            vertices[i] = new SourceVertex() {
                position = positions[i],
                normal = normals[i],
                uv = uvs[i]
            };
        }
        
        int numTriangles = tris.Length / 3;
        int maxBladeTriangles = (bladeSegments - 1) * 2 + 1;

        sourceVertBuffer = new ComputeBuffer(vertices.Length, SOURCE_VERT_STRIDE, ComputeBufferType.Structured, ComputeBufferMode.Immutable);
        sourceVertBuffer.SetData(vertices);
        sourceTriBuffer = new ComputeBuffer(tris.Length, SOURCE_TRI_STRIDE, ComputeBufferType.Structured, ComputeBufferMode.Immutable);
        sourceTriBuffer.SetData(tris);
        drawBuffer = new ComputeBuffer(numTriangles * 3 * bladesPerVertex * maxBladeTriangles, DRAW_STRIDE, ComputeBufferType.Append);
        drawBuffer.SetCounterValue(0);
        argsBuffer = new ComputeBuffer(1, ARGS_STRIDE, ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(new int[] { 0, 1, 0, 0 });

        idKarnel = grassComputeShader.FindKernel("CSMain");
       
        grassComputeShader.SetBuffer(idKarnel, "_SourceVertices", sourceVertBuffer);
        grassComputeShader.SetBuffer(idKarnel, "_SourceTriangles", sourceTriBuffer);
        grassComputeShader.SetBuffer(idKarnel, "_DrawTriangles", drawBuffer);
        grassComputeShader.SetBuffer(idKarnel, "_IndirectArgsBuffer", argsBuffer);
        
        //set vertex data
        grassComputeShader.SetInt("_NumSourceTriangles", numTriangles);
        grassComputeShader.SetInt("_SegmentsPerBlade", Mathf.Max(1,bladeSegments));
        grassComputeShader.SetInt("_BladesPerVertex", Mathf.Max(1, bladesPerVertex));
        
        grassComputeShader.SetFloat("_GrassHeight", grassHeight);
        grassComputeShader.SetFloat("_GrassWidth", grassWidth);
        grassComputeShader.SetFloat("_GrassHeightFactor", grassHeightFactor);
        grassComputeShader.SetFloat("_GrassWidthFactor", grassWidthFactor);
        
        grassComputeShader.SetFloat("_BladeForward", bladeForwardAmount);
        grassComputeShader.SetFloat("_BladeCurve", Mathf.Max(0, bladeCurveAmount));
        grassComputeShader.SetFloat("_OriginDisplacement", bladeOriginiDisplacement);

        material.SetBuffer("_DrawTriangles", drawBuffer);

        // Calculate the number of threads to use. Get the thread size from the kernel
        // Then, divide the number of triangles by that size
        grassComputeShader.GetKernelThreadGroupSizes(idKarnel, out uint threadGroupSize, out _, out _);
        dispatchSize = Mathf.CeilToInt((float)numTriangles / threadGroupSize);

        localBounds = sourceMesh.bounds;
        localBounds.Expand(1);
     }

    private void OnDisable() {
        if(initialized) {
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

        grassComputeShader.SetMatrix("_LocalToWorld", transform.localToWorldMatrix);
        grassComputeShader.SetFloat("_Height", grassHeight);
        
        grassComputeShader.Dispatch(idKarnel, dispatchSize, 1, 1);
        Graphics.DrawProceduralIndirect(material, bounds, MeshTopology.Triangles, argsBuffer);
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
