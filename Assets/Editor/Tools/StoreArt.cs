using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Gera a arte da página do app na Play: ícone 512×512 e gráfico de destaque
/// 1024×500. Marcador de lugar, no mesmo espírito do <see cref="PlaceholderArt"/>
/// — existe para destravar o teste interno, e sai fora no dia da arte de verdade.
///
/// **Por que não fica em Assets/:** isto não é arte do jogo, é arte de vitrine.
/// Dentro de Assets/ a Unity importaria as duas como textura e elas entrariam no
/// build, engordando o `.aab` com imagem que o jogo nunca desenha. Vão para
/// `docs/play-console/arte/`, ao lado do texto da página.
///
/// **Requisitos que o código respeita**, porque a Play recusa fora deles: os dois
/// PNGs são **totalmente opacos** (nenhuma transparência), o ícone é exatamente
/// 512×512 e o destaque exatamente 1024×500.
///
/// Desenha com semente fixa: rodar de novo dá a mesma imagem, então trocar a arte
/// nunca é surpresa.
/// </summary>
static class StoreArt
{
    const string OutputFolder = "docs/play-console/arte";
    const string IconName = "icone-512.png";
    const string FeatureName = "destaque-1024x500.png";

    /// <summary>
    /// Fator de superamostragem. Desenha grande e reduz por média no fim — é o
    /// que dá borda lisa na nave sem escrever antisserrilhado à mão.
    /// </summary>
    const int Supersample = 3;

    // A paleta é a mesma nas duas imagens, para ícone e destaque parecerem do
    // mesmo jogo quando aparecem lado a lado na loja.
    static readonly Color TopSky = new Color(0.031f, 0.043f, 0.110f);
    static readonly Color BottomSky = new Color(0.075f, 0.086f, 0.196f);
    static readonly Color Glow = new Color(0.180f, 0.290f, 0.620f);
    static readonly Color Hull = new Color(0.918f, 0.945f, 1f);
    static readonly Color HullShade = new Color(0.596f, 0.667f, 0.851f);
    static readonly Color Flame = new Color(1f, 0.569f, 0.286f);

    [MenuItem(ProjectTools.StoreArtItem, false, 32)]
    internal static void Run()
    {
        string folder = Path.Combine(Directory.GetCurrentDirectory(), OutputFolder);
        Directory.CreateDirectory(folder);

        string iconPath = Path.Combine(folder, IconName);
        string featurePath = Path.Combine(folder, FeatureName);

        // No ícone a nave ocupa quase tudo: ele é visto a 48px na gaveta do
        // celular, e forma pequena vira borrão.
        WritePng(iconPath, 512, 512, shipScale: 0.34f, shipCenterY: 0.52f, starCount: 90, seed: 20260808);

        // No destaque a nave é menor e mais baixa: a Play corta as bordas em
        // alguns lugares e escreve o nome do app por cima do meio.
        WritePng(featurePath, 1024, 500, shipScale: 0.20f, shipCenterY: 0.46f, starCount: 220, seed: 8082026);

        Debug.Log(
            "[Arte da loja] Gerados em " + OutputFolder + "/:\n" +
            "  " + IconName + " — ícone do app (512×512, opaco)\n" +
            "  " + FeatureName + " — gráfico de destaque (1024×500, opaco)\n\n" +
            "  São marcador de lugar para destravar o teste interno. As screenshots continuam " +
            "sendo print do aparelho — e as de tablet são opcionais, dá para pular.");

        EditorUtility.RevealInFinder(iconPath);
    }

    static void WritePng(string path, int width, int height, float shipScale, float shipCenterY,
                         int starCount, int seed)
    {
        int bigWidth = width * Supersample;
        int bigHeight = height * Supersample;

        var big = new Color[bigWidth * bigHeight];

        PaintSky(big, bigWidth, bigHeight);
        PaintStars(big, bigWidth, bigHeight, starCount * Supersample * Supersample / 4, seed);

        float shipCenterX = bigWidth * 0.5f;
        float shipY = bigHeight * shipCenterY;
        // A escala sai do menor lado, senão a nave estica junto com o banner.
        float shipSize = Mathf.Min(bigWidth, bigHeight) * shipScale;

        PaintTrail(big, bigWidth, bigHeight, shipCenterX, shipY, shipSize);
        PaintShip(big, bigWidth, bigHeight, shipCenterX, shipY, shipSize);

        var pixels = Downsample(big, bigWidth, bigHeight, width, height);

        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.SetPixels32(pixels);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
    }

    static void PaintSky(Color[] buffer, int width, int height)
    {
        float glowX = width * 0.5f;
        float glowY = height * 0.5f;
        float glowRadius = Mathf.Min(width, height) * 0.62f;

        for (int y = 0; y < height; y++)
        {
            // y cresce para CIMA, como na Texture2D: índice 0 é a linha de baixo.
            float t = height > 1 ? (float)y / (height - 1) : 0f;
            Color sky = Color.Lerp(BottomSky, TopSky, t);

            for (int x = 0; x < width; x++)
            {
                float dx = (x - glowX) / glowRadius;
                float dy = (y - glowY) / glowRadius;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);

                // Brilho suave atrás da nave, para ela não flutuar num vazio chapado.
                float halo = Mathf.Clamp01(1f - distance);
                halo *= halo;

                buffer[y * width + x] = Color.Lerp(sky, Glow, halo * 0.45f);
            }
        }
    }

    static void PaintStars(Color[] buffer, int width, int height, int count, int seed)
    {
        var random = new System.Random(seed);

        for (int i = 0; i < count; i++)
        {
            float x = (float)random.NextDouble() * width;
            float y = (float)random.NextDouble() * height;

            // Poucas grandes e muitas pequenas: campo de estrelas chapado não
            // tem profundidade nenhuma.
            double roll = random.NextDouble();
            float radius = roll > 0.97 ? 2.6f * Supersample
                         : roll > 0.85 ? 1.6f * Supersample
                         : 0.9f * Supersample;

            float brightness = 0.35f + (float)random.NextDouble() * 0.65f;

            PaintDot(buffer, width, height, x, y, radius, Color.white, brightness);
        }
    }

    static void PaintDot(Color[] buffer, int width, int height, float cx, float cy,
                         float radius, Color color, float strength)
    {
        int minX = Mathf.Max(0, Mathf.FloorToInt(cx - radius));
        int maxX = Mathf.Min(width - 1, Mathf.CeilToInt(cx + radius));
        int minY = Mathf.Max(0, Mathf.FloorToInt(cy - radius));
        int maxY = Mathf.Min(height - 1, Mathf.CeilToInt(cy + radius));

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float dx = x + 0.5f - cx;
                float dy = y + 0.5f - cy;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                if (distance > radius)
                    continue;

                float falloff = 1f - distance / radius;
                int index = y * width + x;
                buffer[index] = Color.Lerp(buffer[index], color, falloff * strength);
            }
        }
    }

    /// <summary>
    /// O rastro da nave. É o que faz a imagem parada dizer "corrida" — sem ele o
    /// desenho vira uma nave pousada no espaço.
    /// </summary>
    static void PaintTrail(Color[] buffer, int width, int height, float shipX, float shipY, float shipSize)
    {
        float halfWidth = shipSize * 0.22f;
        float length = shipSize * 2.6f;
        float top = shipY - shipSize * 0.85f;

        int minX = Mathf.Max(0, Mathf.FloorToInt(shipX - halfWidth));
        int maxX = Mathf.Min(width - 1, Mathf.CeilToInt(shipX + halfWidth));
        int minY = Mathf.Max(0, Mathf.FloorToInt(top - length));
        int maxY = Mathf.Min(height - 1, Mathf.CeilToInt(top));

        for (int y = minY; y <= maxY; y++)
        {
            // Some conforme desce, e afina junto.
            float along = Mathf.Clamp01((top - y) / length);
            float fade = (1f - along) * (1f - along);
            float taper = halfWidth * (1f - along * 0.8f);

            for (int x = minX; x <= maxX; x++)
            {
                float dx = Mathf.Abs(x + 0.5f - shipX);
                if (dx > taper)
                    continue;

                float across = 1f - dx / taper;
                int index = y * width + x;
                buffer[index] = Color.Lerp(buffer[index], Flame, fade * across * 0.75f);
            }
        }
    }

    static void PaintShip(Color[] buffer, int width, int height, float cx, float cy, float size)
    {
        // Silhueta em espaço normalizado: nariz em (0,1), cauda em y=-1. Assim a
        // forma é legível aqui no código, e mexer nela não vira conta de pixel.
        Vector2[] shape =
        {
            new Vector2(0f, 1f),
            new Vector2(0.34f, -0.20f),
            new Vector2(0.86f, -0.74f),
            new Vector2(0.30f, -0.56f),
            new Vector2(0.19f, -1f),
            new Vector2(-0.19f, -1f),
            new Vector2(-0.30f, -0.56f),
            new Vector2(-0.86f, -0.74f),
            new Vector2(-0.34f, -0.20f),
        };

        int minX = Mathf.Max(0, Mathf.FloorToInt(cx - size));
        int maxX = Mathf.Min(width - 1, Mathf.CeilToInt(cx + size));
        int minY = Mathf.Max(0, Mathf.FloorToInt(cy - size));
        int maxY = Mathf.Min(height - 1, Mathf.CeilToInt(cy + size));

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float px = (x + 0.5f - cx) / size;
                float py = (y + 0.5f - cy) / size;

                if (!InsidePolygon(shape, px, py))
                    continue;

                // Lado direito mais escuro: dá volume sem precisar de sombra.
                float shade = Mathf.InverseLerp(-0.4f, 0.7f, px);
                Color body = Color.Lerp(Hull, HullShade, shade * 0.7f);

                // Motor aceso na traseira.
                float engine = Mathf.InverseLerp(-0.65f, -1f, py);
                body = Color.Lerp(body, Flame, engine);

                buffer[y * width + x] = body;
            }
        }
    }

    static bool InsidePolygon(Vector2[] shape, float x, float y)
    {
        bool inside = false;

        for (int i = 0, j = shape.Length - 1; i < shape.Length; j = i++)
        {
            bool crosses = (shape[i].y > y) != (shape[j].y > y);
            if (!crosses)
                continue;

            float at = (shape[j].x - shape[i].x) * (y - shape[i].y) / (shape[j].y - shape[i].y) + shape[i].x;
            if (x < at)
                inside = !inside;
        }

        return inside;
    }

    /// <summary>
    /// Média dos blocos <see cref="Supersample"/>×<see cref="Supersample"/>. O
    /// alfa sai **sempre 255**: a Play recusa ícone com transparência, e o
    /// desenho é opaco de qualquer forma.
    /// </summary>
    static Color32[] Downsample(Color[] source, int bigWidth, int bigHeight, int width, int height)
    {
        var result = new Color32[width * height];
        float samples = Supersample * Supersample;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float r = 0f, g = 0f, b = 0f;

                for (int sy = 0; sy < Supersample; sy++)
                {
                    int bigY = y * Supersample + sy;
                    for (int sx = 0; sx < Supersample; sx++)
                    {
                        Color sample = source[bigY * bigWidth + x * Supersample + sx];
                        r += sample.r;
                        g += sample.g;
                        b += sample.b;
                    }
                }

                // Sem inverter: o desenho já usa a convenção da Texture2D, com y
                // crescendo para cima, e o EncodeToPNG cuida do resto.
                result[y * width + x] = new Color32(
                    ToByte(r / samples), ToByte(g / samples), ToByte(b / samples), 255);
            }
        }

        return result;
    }

    static byte ToByte(float value) => (byte)Mathf.Clamp(Mathf.RoundToInt(value * 255f), 0, 255);
}
