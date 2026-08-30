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
///   **todas as faixas do caminho também**.
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

    ShipStats stats;
    ShipLaneController lanes;
    LaneTrack track;

    void Awake()
    {
        stats = GetComponent<ShipStats>();
        lanes = GetComponent<ShipLaneController>();
        track = LaneTrack.Instance != null ? LaneTrack.Instance : FindAnyObjectByType<LaneTrack>();
    }

    void Update()
    {
        if (stats == null || lanes == null || track == null)
            return;

        if (!stats.Has(ShipTrait.Autopilot))
            return;

        int current = lanes.CurrentLane;

        // Faixa atual sem ameaça perto: não mexe. É esta linha que faz a nave
        // parecer pilotada em vez de nervosa.
        if (Threat(current) > dangerDistance)
            return;

        if (TryPickEscape(current, out int escape))
            lanes.MoveTo(escape);
    }

    /// <summary>
    /// A melhor faixa que dá para alcançar **sem atravessar perigo**. Falso
    /// quando não existe nenhuma: aí a nave fica onde está e leva o golpe, que é
    /// melhor do que sair correndo para bater em outro.
    /// </summary>
    bool TryPickEscape(int current, out int escape)
    {
        escape = current;

        float bestScore = float.MinValue;
        int bestDistance = int.MaxValue;
        bool found = false;

        for (int lane = 0; lane < track.LaneCount; lane++)
        {
            if (lane == current || !IsReachable(current, lane))
                continue;

            float score = Threat(lane) + Reward(lane);
            int distance = Mathf.Abs(lane - current);

            // Em empate, a faixa mais perto ganha. Cada faixa a mais é mais tempo
            // no ar e mais tempo sem atirar — não se atravessa a pista de graça.
            if (score < bestScore || (Mathf.Approximately(score, bestScore) && distance >= bestDistance))
                continue;

            bestScore = score;
            bestDistance = distance;
            escape = lane;
            found = true;
        }

        return found;
    }

    /// <summary>
    /// Dá para ir daqui até lá? O destino **e cada faixa do caminho** precisam
    /// estar livres — a nave desliza por elas, e um obstáculo no meio do trajeto
    /// acerta do mesmo jeito.
    ///
    /// É o conserto do bug de 30/08: a versão antiga só olhava para o destino.
    /// </summary>
    bool IsReachable(int from, int to)
    {
        // Já está lá. Sem esta saída o laço abaixo nunca encontraria o destino e
        // sairia contando faixas que não existem, para sempre.
        if (from == to)
            return true;

        int step = to > from ? 1 : -1;

        for (int lane = from + step; ; lane += step)
        {
            if (Threat(lane) < clearDistance)
                return false;

            if (lane == to)
                return true;
        }
    }

    /// <summary>
    /// Distância até o primeiro estorvo desta faixa, à frente da nave.
    /// <see cref="lookAhead"/> quando não há nenhum. Debilitante conta como
    /// estorvo: desviar dele é parte de pilotar bem.
    /// </summary>
    float Threat(int lane)
    {
        float nearest = lookAhead;
        float shipY = transform.position.y;

        foreach (var obstacle in Obstacle.Active)
        {
            if (obstacle == null || !obstacle.Occupies(lane))
                continue;

            float distance = obstacle.transform.position.y - shipY;
            if (distance >= 0f && distance < nearest)
                nearest = distance;
        }

        foreach (var pickup in LevelModifierPickup.Active)
        {
            if (pickup == null || pickup.Lane != lane || pickup.Definition == null)
                continue;

            if (pickup.Definition.category != LevelModifier.Category.Debilitante)
                continue;

            float distance = pickup.transform.position.y - shipY;
            if (distance >= 0f && distance < nearest)
                nearest = distance;
        }

        return nearest;
    }

    /// <summary>
    /// O bônus de desempate por prêmio na faixa. **Somado à ameaça e não
    /// comparado antes dela**, de propósito: um Reforço nunca pode fazer a nave
    /// escolher uma faixa mais perigosa — o bônus é pequeno o bastante para só
    /// pesar entre faixas parecidas.
    /// </summary>
    float Reward(int lane)
    {
        float shipY = transform.position.y;

        foreach (var pickup in LevelModifierPickup.Active)
        {
            if (pickup == null || pickup.Lane != lane || pickup.Definition == null)
                continue;

            if (pickup.Definition.category == LevelModifier.Category.Debilitante)
                continue;

            float distance = pickup.transform.position.y - shipY;
            if (distance >= 0f && distance <= lookAhead)
                return rewardBonus;
        }

        return 0f;
    }
}
