using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.VisualScripting;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Serialization;

public class CompteShaderTest : MonoBehaviour
{
    [SerializeField] private ComputeShader grassComputeShader = default;
    [SerializeField] private Material material = default;
    
    [SerializeField] private float grassHeight = 1;
    private float grassWidth = 0.2f;
    [Range(0, 1)] public float bladeRadius = 0.6f;
    [Range(0, 1)] public float bladeForwardAmount = 0.38f;
    [Range(1, 5)] public float bladeCurveAmount = 2;

    private int bladeSegments = 4;
    private int bladesPerVertex = 1;

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct SourceVertex {
        public Vector3 position;
        public Vector3 normal;
        public Vector2 uv;
    }
    
    private Mesh sourceMesh;
    private bool initialized;
    private ComputeBuffer sourceVertBuffer;

    private ComputeBuffer drawBuffer;
    private ComputeBuffer argsBuffer;

    private int idKarnel;
    private int dispatchSize;

    private Bounds localBounds;

    private static readonly int SOURCE_VERT_STRIDE = UnsafeUtility.SizeOf<SourceVertex>();
    private const int DRAW_STRIDE = sizeof(float) * (3 + (3 + 2) * 3);
    private const int ARGS_STRIDE = sizeof(int) * 4;

     private void OnEnable() {
         if(initialized) {
            OnDisable();
        }
        initialized = true;

        sourceMesh = GetComponent<MeshFilter>().sharedMesh;
        
        Vector3[] positions = sourceMesh.vertices;
        Vector3[] normals = sourceMesh.normals;
        Vector2[] uvs = sourceMesh.uv;


        SourceVertex[] vertices = new SourceVertex[positions.Length];
        for(int i = 0; i < vertices.Length; i++) {
            vertices[i] = new SourceVertex() {
                position = positions[i],
                normal = normals[i],
                uv = uvs[i],
            };
        }
        int numTriangles = vertices.Length;
        int maxBladeTriangles = 4 * ((5 - 1) * 2 + 1);

        sourceVertBuffer = new ComputeBuffer(vertices.Length, SOURCE_VERT_STRIDE, ComputeBufferType.Structured, ComputeBufferMode.Immutable);
        sourceVertBuffer.SetData(vertices);

        // We split each triangle into three new ones
        drawBuffer = new ComputeBuffer(numTriangles * maxBladeTriangles, DRAW_STRIDE, ComputeBufferType.Append);
        drawBuffer.SetCounterValue(0);
        argsBuffer = new ComputeBuffer(1, ARGS_STRIDE, ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(new int[] { 0, 1, 0, 0 });

        idKarnel = grassComputeShader.FindKernel("CSMain");
       
        grassComputeShader.SetBuffer(idKarnel, "_SourceVertices", sourceVertBuffer);
        grassComputeShader.SetBuffer(idKarnel, "_DrawTriangles", drawBuffer);
        grassComputeShader.SetBuffer(idKarnel, "_IndirectArgsBuffer", argsBuffer);
        
        //set vertex data
        grassComputeShader.SetInt("_NumSourceTriangles", numTriangles);

        material.SetBuffer("_DrawTriangles", drawBuffer);

        // Calculate the number of threads to use. Get the thread size from the kernel
        // Then, divide the number of triangles by that size
        grassComputeShader.GetKernelThreadGroupSizes(idKarnel, out uint threadGroupSize, out _, out _);
        dispatchSize = Mathf.CeilToInt((float)numTriangles / threadGroupSize);
        
        localBounds = sourceMesh.bounds;
        localBounds.Expand(1);
        
        grassComputeShader.SetFloat("_GrassHeight", grassHeight);
        grassComputeShader.SetFloat("_GrassWidth", grassWidth);

        grassComputeShader.SetFloat("_BladeRadius", bladeRadius);
        grassComputeShader.SetFloat("_BladeForward", bladeForwardAmount);
        grassComputeShader.SetFloat("_BladeCurve", Mathf.Max(0, bladeCurveAmount));
     }

    private void OnDisable() {
        if(initialized) {
            sourceVertBuffer.Release();
            //sourceTriBuffer.Release();
            drawBuffer.Release();
            argsBuffer.Release();
        }
        initialized = false;
    }

    private void LateUpdate() {
        drawBuffer.SetCounterValue(0);
        argsBuffer.SetData(new int[] { 0, 1, 0, 0 });

        Bounds bounds = TransformBounds(localBounds);

        grassComputeShader.SetMatrix("_LocalToWorld", transform.localToWorldMatrix);
        grassComputeShader.SetFloat("_Height", grassHeight);
        
        // Copy the count (stack size) of the draw buffer to the args buffer, at byte position zero
        // This sets the vertex count for our draw procediral indirect call
        ComputeBuffer.CopyCount(drawBuffer, argsBuffer, 0);
        
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
