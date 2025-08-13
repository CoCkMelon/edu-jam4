using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

// NoiseGenerator.cs
[RequireComponent(typeof(Camera))]
public class NoiseGenerator : MonoBehaviour
{
    public int textureWidth = 2000;
    public int textureHeight = 10000;
    public float scale = 0.05f;
    public float seed = 12345;

    private RenderTexture noiseTexture;
    private Material noiseMaterial;
    private MeshRenderer meshRenderer;

    void Start()
    {
        GenerateNoiseTexture();
        ApplyNoiseToMesh();
    }

    void GenerateNoiseTexture()
    {
        // Create render texture
        noiseTexture = new RenderTexture(textureWidth, textureHeight, 24);
        noiseTexture.enableRandomWrite = false;
        noiseTexture.Create();

        // Create material with noise shader
        noiseMaterial = new Material(Shader.Find("Custom/NoiseShader"));
        noiseMaterial.SetFloat("_Scale", scale);
        noiseMaterial.SetFloat("_Seed", seed);

        // Render noise to texture
        Graphics.Blit(null, noiseTexture, noiseMaterial);
    }

    void ApplyNoiseToMesh()
    {
        // For visualization (optional)
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.material.mainTexture = noiseTexture;
        }
    }

    public Texture2D ReadNoiseTexture()
    {
        // Create temporary texture
        Texture2D readableTex = new Texture2D(textureWidth, textureHeight, TextureFormat.RFloat, false);

        // Read pixels from GPU
        RenderTexture.active = noiseTexture;
        readableTex.ReadPixels(new Rect(0, 0, textureWidth, textureHeight), 0, 0);
        readableTex.Apply();
        RenderTexture.active = null;

        return readableTex;
    }
}
