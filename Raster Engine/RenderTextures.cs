using System.IO;
using UnityEngine;

public class RenderTextures : MonoBehaviour
{
    public Camera renderCamera;
    public Shader diffuseShader;
    public Shader normalShader;

    private RenderTexture diffuseRT;
    private RenderTexture normalRT;
    public Texture2D diffuseTexture;
    public Texture2D normalTexture;

    public int textureWidth = 1024;
    public int textureHeight = 1024;

    void Awake()
    {
        if (renderCamera == null)
        {
            Debug.LogError("Render Camera is not assigned!");
            return;
        }

        // Create RenderTextures
        diffuseRT = new RenderTexture(textureWidth, textureHeight, 24);
        normalRT = new RenderTexture(textureWidth, textureHeight, 24);

        // Create Textures
        diffuseTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGB24, false);
        normalTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGB24, false);

        // Render and save textures
        CaptureTextures();
    }

    void CaptureTextures()
    {
        if (diffuseShader == null || normalShader == null)
        {
            Debug.LogError("Shaders are not assigned!");
            return;
        }

        // Render Diffuse (Albedo)
        renderCamera.targetTexture = diffuseRT;
        renderCamera.RenderWithShader(diffuseShader, "");
        SaveRenderTextureToTexture2D(diffuseRT, diffuseTexture, "DiffuseTexture.png");

        // Render Normal Map
        renderCamera.targetTexture = normalRT;
        renderCamera.RenderWithShader(normalShader, "");
        SaveRenderTextureToTexture2D(normalRT, normalTexture, "NormalTexture.png");

        // Reset Camera Target
        renderCamera.targetTexture = null;
    }

    void SaveRenderTextureToTexture2D(RenderTexture renderTexture, Texture2D texture, string filename)
    {
        RenderTexture.active = renderTexture;
        texture.ReadPixels(new Rect(0, 0, textureWidth, textureHeight), 0, 0);
        texture.Apply();

        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/" + filename, bytes);
        Debug.Log("Saved " + filename);

        RenderTexture.active = null;
    }
}

