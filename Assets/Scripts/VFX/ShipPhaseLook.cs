using UnityEngine;

/// <summary>
/// A nave fica translúcida enquanto atravessa obstáculos — o traço
/// <see cref="ShipTrait.Phasing"/>. Sem isto o jogador veria a nave passar por
/// dentro de um bloco e acharia que a batida falhou.
///
/// Mexe **só na transparência**, e no <c>LateUpdate</c>: a cor é do
/// <see cref="ShipStageLook"/>, e assim os dois nunca brigam pelo mesmo número.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class ShipPhaseLook : MonoBehaviour
{
    [Tooltip("Opacidade da nave enquanto atravessa.")]
    [SerializeField, Range(0.05f, 1f)] float phasingAlpha = 0.35f;

    SpriteRenderer art;
    ShipStats stats;
    float baseAlpha;

    void Awake()
    {
        art = GetComponent<SpriteRenderer>();
        stats = GetComponent<ShipStats>();
        baseAlpha = art.color.a;
    }

    void LateUpdate()
    {
        float alpha = stats != null && stats.Has(ShipTrait.Phasing) ? phasingAlpha : baseAlpha;

        var color = art.color;
        if (Mathf.Approximately(color.a, alpha))
            return;

        color.a = alpha;
        art.color = color;
    }
}
