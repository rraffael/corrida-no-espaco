using UnityEngine;

/// <summary>
/// Muda o visual da nave conforme o **estágio do poder condicional** dela — hoje,
/// a Predadora com 5 e com 10 cargas do Reforço estrutural.
///
/// ⚠️ **É marcador de lugar, e está aqui para ser trocado** *(pedido do Raffael,
/// 26/09/2026)*: só tinge e aumenta um pouco a nave a cada estágio, para ele
/// lembrar de desenhar o visual de verdade da Predadora — um sprite por
/// estágio, provavelmente na ficha da nave.
///
/// Nave sem condicional, ou com um que não tem estágio, fica exatamente como
/// está: o estágio é 0 a corrida inteira e nada é tocado.
///
/// Aumentar a nave não aumenta onde ela bate: obstáculo, estilhaço e item da
/// pista medem a distância ao centro dela, não o tamanho do desenho.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class ShipStageLook : MonoBehaviour
{
    [Tooltip("Cor de cada estágio a partir do 1. O estágio 0 é a cor que a nave já tem.")]
    [SerializeField] Color[] stageTints =
    {
        new Color(1f, 0.72f, 0.62f, 1f),
        new Color(1f, 0.38f, 0.32f, 1f),
    };

    [Tooltip("Tamanho de cada estágio a partir do 1, em relação ao de largada.")]
    [SerializeField] float[] stageScales = { 1.08f, 1.18f };

    SpriteRenderer art;
    ShipAbilities abilities;
    Color baseColor;
    Vector3 baseScale;
    int shownStage;

    void Awake()
    {
        art = GetComponent<SpriteRenderer>();
        abilities = GetComponent<ShipAbilities>();
        baseColor = art.color;
        baseScale = transform.localScale;
    }

    void Update()
    {
        var conditional = abilities != null ? abilities.Conditional : null;
        int stage = conditional != null ? conditional.Definition.Stage(conditional) : 0;
        if (stage == shownStage)
            return;

        shownStage = stage;

        if (stage <= 0)
        {
            art.color = baseColor;
            transform.localScale = baseScale;
            return;
        }

        // Estágio além da lista fica no último: um condicional com mais estágios
        // que cores não pode quebrar, só deixa de mudar.
        art.color = stageTints.Length > 0
            ? stageTints[Mathf.Min(stage, stageTints.Length) - 1]
            : baseColor;
        transform.localScale = stageScales.Length > 0
            ? baseScale * stageScales[Mathf.Min(stage, stageScales.Length) - 1]
            : baseScale;
    }
}
