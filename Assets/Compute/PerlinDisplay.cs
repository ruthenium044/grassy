using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[ExecuteInEditMode]
public class PerlinDisplay : MonoBehaviour
{
    public ComputeShader computeShader;
    public RenderTexture renderTexture;
    
    public Mesh mesh;
    public Material material;
    Renderer m_Renderer;
    
    private int idKarnel;
    private int dispatchSize;
    
    private void OnEnable()
    {
        m_Renderer = GetComponent<Renderer> ();
        material = m_Renderer.material;
        mesh = GetComponent<MeshFilter>().mesh;
        
        renderTexture = new RenderTexture(256, 256, 24);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();
        
        idKarnel = computeShader.FindKernel("CSMain");
        computeShader.SetTexture(idKarnel, "Result", renderTexture);
        
        int[] tris = mesh.triangles;
        int numTriangles = tris.Length / 3;
        
        computeShader.SetInt("_NumSourceTriangles", numTriangles);
        
        computeShader.GetKernelThreadGroupSizes(idKarnel, out uint threadGroupSize, out _, out _);
        dispatchSize = Mathf.CeilToInt((float)numTriangles / threadGroupSize);
        
        //computeShader.Dispatch(idKarnel, renderTexture.width / 8, renderTexture.height / 8, 1);
        
        material.SetTexture("_MainTex", renderTexture);
        Graphics.DrawMesh(mesh, transform.position, transform.rotation, material, 0);
    }

    private void LateUpdate() {
        if (Application.isPlaying == false)
        {
            OnEnable();
        }
        computeShader.Dispatch(idKarnel, dispatchSize, 1, 1);
    }
}
