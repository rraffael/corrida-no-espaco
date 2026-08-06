using UnityEngine;

/// <summary>
/// A ficha de um tipo de obstáculo — o equivalente ao <see cref="ShipStats"/> da
/// nave. Cada tipo é um arquivo em <c>Assets/Levels/Obstaculos/</c>, então criar
/// um obstáculo novo é preencher números, não escrever código.
///
/// É ScriptableObject, e não componente, porque o dado é o mesmo para todos os
/// obstáculos daquele tipo: a fase carrega a ficha uma vez e cada obstáculo que
/// nasce só aponta para ela.
/// </summary>
[CreateAssetMenu(fileName = "Obstaculo", menuName = "Corrida no Espaço/Ficha de obstáculo")]
public class ObstacleStats : ScriptableObject
{
    [Header("Identidade")]
    public string displayName = "Obstáculo";

    [Tooltip("Arte. Vazio: o obstáculo fica invisível, mas continua batendo.")]
    public Sprite sprite;

    public Color color = new Color(1f, 0.55f, 0.4f, 1f);

    [Header("Tamanho")]
    [Tooltip("Quantas faixas ele ocupa. 2 num corredor de 3 já tranca dois terços da pista.")]
    [Min(1)] public int laneSpan = 1;

    [Tooltip("Altura em unidades de mundo. A largura vem do laneSpan.")]
    [Min(0.1f)] public float height = 0.9f;

    [Tooltip("Folga entre a arte e a divisa da faixa, para o obstáculo não encostar na linha.")]
    [Min(0f)] public float lanePadding = 0.15f;

    [Header("Combate")]
    [Min(1f)] public float maxHealth = 50f;

    [Tooltip("Dano na nave ao bater. A defesa da nave desconta disto.")]
    [Min(0f)] public float contactDamage = 20f;

    [Tooltip("Quanto a corrida ganha de velocidade quando este obstáculo é destruído.")]
    [Min(0f)] public float speedBonusOnKill = 1.5f;

    [Tooltip("Quanto a corrida perde de velocidade na batida.")]
    [Min(0f)] public float speedPenaltyOnCrash = 2f;

    [Header("Alcance")]
    [Tooltip("Distância que conta como batida na nave.")]
    [Min(0.1f)] public float crashDistance = 0.7f;

    [Tooltip("Distância que conta como acerto de tiro.")]
    [Min(0.1f)] public float hitDistance = 0.5f;

    [Header("Estilhaços")]
    [Tooltip("Ao ser destruído, joga estilhaços para a frente na própria faixa — quem ficar " +
             "parado atirando de baixo toma dano, e é obrigado a trocar de faixa.")]
    public bool shrapnelOnDeath;

    [Min(0f)] public float shrapnelDamage = 15f;

    [Tooltip("Velocidade dos estilhaços somada à da corrida, em unidades por segundo.")]
    [Min(0.1f)] public float shrapnelSpeed = 6f;

    [Tooltip("Quantos estilhaços saem, espalhados pelas faixas que o obstáculo ocupava.")]
    [Min(1)] public int shrapnelCount = 1;

    /// <summary>Largura em unidades de mundo, dada a largura de faixa do corredor.</summary>
    public float WidthFor(LaneTrack track) =>
        Mathf.Max(0.1f, laneSpan * track.LaneWidth - lanePadding * 2f);
}
