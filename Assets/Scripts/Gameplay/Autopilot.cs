using UnityEngine;

/// <summary>
/// O jogo pilotando a nave, enquanto o traço <see cref="ShipTrait.Autopilot"/>
/// estiver ligado. É o que sustenta a Super IA da Raffa.
///
/// **Fica na nave e liga sozinho pelo traço**, em vez de o poder chamar este
/// componente. Assim o poder não precisa saber que existe um piloto, e um segundo
/// poder que queira pilotar — ou um modificador de fase que faça a nave fugir do
/// controle do jogador — reaproveita tudo isto sem uma linha nova.
///
/// <para>
/// **Como ele decide, e por que assim** *(reescrito em 30/08/2026, depois de o
/// Raffael ver a primeira versão bater no aparelho)*.
/// </para>
///
/// A primeira versão perseguia, todo quadro, a faixa mais desimpedida à frente.
/// Errava de duas maneiras, e as duas ele viu acontecer:
///
/// 1. **Ela se mexia por causa de coisa distante.** Um obstáculo lá em cima já
///    bastava para trocar de faixa, e no caminho ela passava por cima de um que
///    estava perto — o obstáculo que ela *acabara* de desviar. Trocar de faixa
///    não é teletransporte: a nave **atravessa** as faixas do meio, e a versão
///    antiga nunca olhava para elas.
/// 2. **Ela não parava quieta.** Perseguir o máximo a cada quadro faz a nave
///    ficar trocando de faixa num corredor vazio — parece defeito, e ainda cala
///    a arma a cada troca.
///
/// As duas regras de agora resolvem os dois, e são as que o Raffael propôs:
///
/// - **Só desvia quando o perigo está perto.** Longe da ameaça, fica onde está.
/// - **Só desvia para onde dá para chegar:** o destino tem de estar livre, e
///   **nada pode alcançá-la no caminho**.
///
/// O caminho passou a ser medido **em tempo** em 17/10/2026 — antes, qualquer
/// obstáculo perto numa faixa do meio a fechava, e a Super IA batia sempre numa
/// Barcaça na beirada. Ver <see cref="LaneSense.CrossingIsSafe"/>.
/// </summary>
[DefaultExecutionOrder(-52)]
public class Autopilot : MonoBehaviour
{
    [Tooltip("A que distância um obstáculo na faixa da nave passa a ser motivo para sair dela.\n\n" +
             "Curto de propósito: é o que impede a nave de se mexer por causa de coisa que ainda " +
             "está longe, e foi assim que a primeira versão bateu — ela desviava de um obstáculo " +
             "distante e no caminho passava por cima de outro que estava perto.")]
    [SerializeField, Min(1f)] float dangerDistance = 5f;

    [Tooltip("Folga mínima que uma faixa precisa ter, na altura da nave, para valer como destino " +
             "OU como passagem. Maior que a distância de perigo: não adianta fugir para uma faixa " +
             "que fica perigosa no segundo seguinte.")]
    [SerializeField, Min(1f)] float clearDistance = 7f;

    [Tooltip("Até que distância ele enxerga. Além disso, tanto faz — a faixa conta como livre.")]
    [SerializeField, Min(1f)] float lookAhead = 14f;

    [Tooltip("Quanto um Reforço na faixa vale, em 'distância de folga' fingida. É só desempate: " +
             "entre duas faixas igualmente seguras, ele passa por cima do prêmio.")]
    [SerializeField, Min(0f)] float rewardBonus = 2f;

    [Tooltip("Folga de lado, em unidades de mundo, somada ao alcance de batida de cada obstáculo " +
             "na conta do cruzamento. Sem ela, a conta aceitaria passar raspando por um fio.")]
    [SerializeField, Min(0f)] float crossingMargin = 0.15f;


    ShipStats stats;
    ShipLaneController lanes;
    LaneTrack track;

    /// <summary>A leitura da pista — a mesma do piloto do simulador. Ver <see cref="LaneSense"/>.</summary>
    LaneSense sense;

    void Awake()
    {
        stats = GetComponent<ShipStats>();
        lanes = GetComponent<ShipLaneController>();
        track = LaneTrack.Instance != null ? LaneTrack.Instance : FindAnyObjectByType<LaneTrack>();

        if (lanes != null && track != null)
            sense = new LaneSense(transform, lanes, track);
    }

    void Update()
    {
        if (stats == null || sense == null)
            return;

        if (!stats.Has(ShipTrait.Autopilot))
            return;

        // A cada quadro, e não só no Awake: assim mexer num campo do Inspector
        // com o jogo rodando continua valendo na hora, como valia antes.
        sense.DangerDistance = dangerDistance;
        sense.ClearDistance = clearDistance;
        sense.LookAhead = lookAhead;
        sense.RewardBonus = rewardBonus;
        sense.CrossingMargin = crossingMargin;

        int current = lanes.CurrentLane;

        // Faixa atual sem ameaça perto: não mexe. É esta linha que faz a nave
        // parecer pilotada em vez de nervosa.
        if (sense.Threat(current) > dangerDistance)
            return;

        if (sense.TryPickEscape(current, out int escape))
            lanes.MoveTo(escape);
    }
}
