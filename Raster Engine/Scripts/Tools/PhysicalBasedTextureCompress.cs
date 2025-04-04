using UnityEngine;
using System.IO;

public class PhysicalBasedTextureCompress : MonoBehaviour
{
    [Header("Texture Assignments")]
    [Tooltip("Assign your diffuse texture here")]
    public Texture2D diffuseTexture;

    [Tooltip("Assign your ambient occlusion (AO) texture here (grayscale), resolution must match diffuse")]
    public Texture2D aoTexture;
    [Tooltip("Metalic specular compressed into alpha, resolution must match diffuse")]
    public Texture2D metalicSpecular;

    [Header("Output Settings")]
    [Tooltip("Folder path relative to the project folder (e.g., Assets/Output)")]
    public string outputFolder = "Assets/Output";

    public string fileName = "CompressedAoDiffSpec";
    public Color DiffuseTint = new Color(1,1,1,1);
    public float MetalicAdd;
    public float SpecAdd;

    public float MetalicMult = 1;
    public float SpecMult = 1;

    /// <summary>
    /// This method applies the AO texture to modulate the brightness of the diffuse texture.
    /// It then saves the resulting texture as a PNG in the specified output folder.
    /// To run, click the gear (context menu) on this component in the Inspector and select
    /// "Apply AO to Diffuse and Save PNG".
    /// </summary>
    [ContextMenu("Apply AO to Diffuse and Save PNG")]
    public void ApplyAndSaveAOTexture()
    {
        // Ensure both textures are assigned.
        if (diffuseTexture == null)
        {
            Debug.LogError("Please assign both the diffuse and AO textures in the inspector.");
            return;
        }

        // Ensure both textures have the same dimensions.
        //if (diffuseTexture.width != aoTexture.width || diffuseTexture.height != aoTexture.height)
        //{
        //    Debug.LogError("Diffuse and AO textures must have the same dimensions.");
        //    return;
        //}

        int width = diffuseTexture.width;
        int height = diffuseTexture.height;

        // Create a new texture to store the output.
        Texture2D outputTexture = new Texture2D(width, height);

        // Get pixel data from both textures.
        Color[] diffusePixels = diffuseTexture.GetPixels();
       
        Color[] outputPixels = diffuseTexture.GetPixels();

        if (aoTexture != null)
        {
            Color[] aoPixels = aoTexture.GetPixels();
            // Process each pixel: multiply the diffuse color by the AO intensity (using red channel).
            for (int i = 0; i < diffusePixels.Length; i++)
            {
                float aoIntensity = aoPixels[i].r;  // AO value assumed to be grayscale stored in the red channel.
                Color diffColor = diffusePixels[i];

                outputPixels[i] = new Color(
                    diffColor.r * aoIntensity,
                    diffColor.g * aoIntensity,
                    diffColor.b * aoIntensity,
                    diffColor.a);  // Preserve the original alpha.
            }
        }


        for (int i = 0; i < outputPixels.Length; i++)
        {
            outputPixels[i] *= DiffuseTint;
        }

        if(metalicSpecular != null)
        {
            Color[] metspec = metalicSpecular.GetPixels();
            Debug.Log("adding spec metalic");
            for (int i = 0; i < diffusePixels.Length; i++)
            {

                Color diffColor = outputPixels[i];

              float met =  (float)(((int)(Mathf.Min(0.99f, (metspec[i].r + MetalicAdd) * MetalicMult) * 4)) * 0.25f);

                //float met = (float)(((int)(MetalicMult * 4)) * 0.25f);

                // diffColor.a = (metspec[i].r *0.8f) + (metspec[i].a * 0.2f);
                diffColor.a = met + ((Mathf.Min(0.99f, metspec[i].a * SpecMult) + SpecAdd) * 0.2f);
               // diffColor.a = (metspec[i].a * 0.2f);
                outputPixels[i] = diffColor;


            }

        }

        // Apply the modified pixel data to the output texture.
        outputTexture.SetPixels(outputPixels);
        outputTexture.Apply();

        // Encode the texture to PNG.
        byte[] pngData = outputTexture.EncodeToPNG();
        if (pngData == null)
        {
            Debug.LogError("Failed to encode texture to PNG.");
            return;
        }

        // Ensure the output folder exists.
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        // Create a unique file name.
       // string fileName = "AOTexture_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
        string filePath = Path.Combine(outputFolder, fileName+".png");

        // Write the PNG file.
        File.WriteAllBytes(filePath, pngData);

        Debug.Log("Saved texture to: " + filePath);
    }
}

