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
    const string PadlockPath = Folder + "/padlock.png";

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

    /// <summary>
    /// Cadeado branco com fundo transparente, para a linha de fase trancada.
    /// Branco de propósito: a cor sai do <c>Image</c> que o desenha, então o
    /// mesmo PNG serve a qualquer paleta que a arte de verdade traga depois.
    /// </summary>
    public static Sprite Padlock()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(PadlockPath);
        if (existing != null)
            return existing;

        const int size = 64;

        // Corpo, arco e buraco da fechadura em coordenadas do próprio desenho: o
        // arco nasce exatamente no topo do corpo, então as duas peças se encostam.
        const float bodyLeft = 12f, bodyRight = 52f, bodyBottom = 6f, bodyTop = 36f;
        const float shackleX = 32f, shackleY = bodyTop;
        const float shackleInner = 11f, shackleOuter = 16f;
        const float keyholeX = 32f, keyholeY = 21f, keyholeRadius = 4.5f;

        var opaque = new Color32(255, 255, 255, 255);
        var clear = new Color32(255, 255, 255, 0);
        var pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;

                bool body = px >= bodyLeft && px <= bodyRight && py >= bodyBottom && py <= bodyTop;

                float dx = px - shackleX;
                float dy = py - shackleY;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                bool shackle = dy >= 0f && distance >= shackleInner && distance <= shackleOuter;

                float kx = px - keyholeX;
                float ky = py - keyholeY;
                bool keyhole = kx * kx + ky * ky <= keyholeRadius * keyholeRadius;

                pixels[y * size + x] = (body || shackle) && !keyhole ? opaque : clear;
            }
        }

        return Write(PadlockPath, size, size, pixels, pixelsPerUnit: size);
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
