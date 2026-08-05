using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Arte marcador de lugar gerada em código. A regra do projeto é que trocar por
/// arte de verdade seja só substituir o PNG, sem encostar em script nenhum.
/// </summary>
static class PlaceholderArt
{
    public const string Folder = "Assets/Art/Placeholder";
    const string PixelPath = Folder + "/pixel.png";

    /// <summary>
    /// Sprite branco de 1x1 unidade de mundo. Escalar no transform dá qualquer
    /// retângulo, e a cor vem do SpriteRenderer — serve de tiro, de obstáculo e
    /// de divisa de faixa sem gerar um PNG para cada.
    /// </summary>
    public static Sprite Pixel()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(PixelPath);
        if (existing != null)
            return existing;

        var pixels = new Color32[4];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(255, 255, 255, 255);

        return Write(PixelPath, 2, 2, pixels, pixelsPerUnit: 2f);
    }

    public static Sprite Write(string assetPath, int width, int height, Color32[] pixels, float pixelsPerUnit)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(assetPath));

        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.SetPixels32(pixels);
        texture.Apply();
        File.WriteAllBytes(assetPath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }
}
