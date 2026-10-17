using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Uma borda que brilha com um risco de luz dando a volta nela — o "pronto para
/// usar" do canto do poder *(pedido do Raffael, 17/10/2026)*. Quem liga e
/// desliga é o <see cref="AbilityHud"/>; esta classe só desenha.
///
/// **Desenhada em código, sem sprite:** é um contorno de quadradinhos, cada um
/// com a sua transparência, e o mais forte é o da cabeça do risco. Arte nenhuma
/// para manter, e o mesmo componente serve a qualquer tamanho de canto.
///
/// Corre em tempo não escalado, como o piscar de "pronto": com o tempo lento de
/// um modificador, o aviso não pode ficar preguiçoso.
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class ReadyGlowBorder : MaskableGraphic
{
    [Tooltip("Grossura da borda, em pixels da tela de referência.")]
    [SerializeField, Min(1f)] float thickness = 6f;

    [Tooltip("Segundos para o risco de luz dar uma volta inteira.")]
    [SerializeField, Min(0.1f)] float lapSeconds = 1.4f;

    [Tooltip("Comprimento do rastro do risco, como fração da volta.")]
    [SerializeField, Range(0.05f, 1f)] float trail = 0.35f;

    [Tooltip("Brilho da borda fora do risco. Zero: só o risco aparece.")]
    [SerializeField, Range(0f, 1f)] float baseAlpha = 0.35f;

    [Tooltip("Quadradinhos por lado. Mais: o risco anda mais liso.")]
    [SerializeField, Min(2)] int segmentsPerSide = 16;

    float head;

    void Update()
    {
        head = Mathf.Repeat(Time.unscaledTime / lapSeconds, 1f);
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        var rect = GetPixelAdjustedRect();
        float t = Mathf.Min(thickness, rect.width * 0.5f, rect.height * 0.5f);
        float width = rect.width;
        float height = rect.height - 2f * t;
        float perimeter = 2f * width + 2f * height;
        if (perimeter <= 0f)
            return;

        // Sentido horário a partir do canto de cima à esquerda. Cima e baixo
        // ocupam a largura inteira; os lados ficam entre eles, sem sobrepor os
        // cantos — senão o canto brilharia em dobro.
        float xMin = rect.xMin, xMax = rect.xMax, yMin = rect.yMin, yMax = rect.yMax;
        AddSide(vh, 0f, width, perimeter, a => new Rect(Mathf.Lerp(xMin, xMax, a.x), yMax - t,
                                                         (xMax - xMin) * (a.y - a.x), t));
        AddSide(vh, width, height, perimeter, a => new Rect(xMax - t, Mathf.Lerp(yMax - t, yMin + t, a.y),
                                                             t, height * (a.y - a.x)));
        AddSide(vh, width + height, width, perimeter, a => new Rect(Mathf.Lerp(xMax, xMin, a.y), yMin,
                                                                     (xMax - xMin) * (a.y - a.x), t));
        AddSide(vh, 2f * width + height, height, perimeter, a => new Rect(xMin, Mathf.Lerp(yMin + t, yMax - t, a.x),
                                                                           t, height * (a.y - a.x)));
    }

    /// <summary>
    /// Um lado da borda, em <see cref="segmentsPerSide"/> quadradinhos. A conta
    /// do retângulo recebe o trecho do lado como (início, fim), de 0 a 1.
    /// </summary>
    void AddSide(VertexHelper vh, float start, float length, float perimeter,
                 System.Func<Vector2, Rect> piece)
    {
        if (length <= 0f)
            return;

        for (int i = 0; i < segmentsPerSide; i++)
        {
            float a0 = (float)i / segmentsPerSide;
            float a1 = (float)(i + 1) / segmentsPerSide;
            float position = (start + (a0 + a1) * 0.5f * length) / perimeter;

            AddQuad(vh, piece(new Vector2(a0, a1)), Brightness(position));
        }
    }

    /// <summary>Forte na cabeça do risco, apagando no rastro que fica para trás dela.</summary>
    float Brightness(float position)
    {
        float behind = Mathf.Repeat(head - position, 1f);
        float streak = behind <= trail ? 1f - behind / trail : 0f;
        return Mathf.Max(baseAlpha, streak);
    }

    void AddQuad(VertexHelper vh, Rect area, float alpha)
    {
        var tint = color;
        tint.a *= alpha;
        Color32 c = tint;

        int index = vh.currentVertCount;
        vh.AddVert(new Vector3(area.xMin, area.yMin), c, Vector2.zero);
        vh.AddVert(new Vector3(area.xMin, area.yMax), c, Vector2.zero);
        vh.AddVert(new Vector3(area.xMax, area.yMax), c, Vector2.zero);
        vh.AddVert(new Vector3(area.xMax, area.yMin), c, Vector2.zero);
        vh.AddTriangle(index, index + 1, index + 2);
        vh.AddTriangle(index + 2, index + 3, index);
    }
}
