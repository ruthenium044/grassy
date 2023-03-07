using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CompteShaderTest : MonoBehaviour
{
    public ComputeShader computeShader;
    public RenderTexture renderTexture;

     Material mat;

     void Start()
    {
        mat = GetComponent<Renderer>().material;

        renderTexture = new RenderTexture(256, 256, 24);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();
        
        computeShader.SetTexture(0, "Result", renderTexture);
        computeShader.SetFloat("Resolution", 128);
        computeShader.Dispatch(0, renderTexture.width / 8, renderTexture.height / 8, 1);
        
        mat.mainTexture = renderTexture;
    }

    
}
