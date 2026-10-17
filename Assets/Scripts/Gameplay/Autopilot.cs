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
/// Barcaça na beirada. Ver <see cref="CrossingIsSafe"/>.
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
    RaceSpeed race;

    void Awake()
    {
        stats = GetComponent<ShipStats>();
        lanes = GetComponent<ShipLaneController>();
        track = LaneTrack.Instance != null ? LaneTrack.Instance : FindAnyObjectByType<LaneTrack>();
    }

    void Start() => race = RaceSpeed.Instance;

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
    /// Dá para ir daqui até lá? Duas perguntas, e as duas têm de dar sim:
    ///
    /// - **O destino tem folga** — não adianta fugir para uma faixa que fica
    ///   perigosa no segundo seguinte. Esta é a regra de 30/08, e continua.
    /// - **O caminho dá tempo** *(17/10/2026)*: a nave desliza pelas faixas do
    ///   meio, e um obstáculo ali acerta do mesmo jeito — **se ele chegar
    ///   enquanto ela passa**. Até 17/10 qualquer obstáculo perto numa faixa do
    ///   caminho a fechava, e foi assim que a Super IA batia sempre numa Barcaça
    ///   na beirada: a única saída era cruzar a faixa do meio, que a própria
    ///   Barcaça ocupa, mesmo dando tempo de sobra para passar.
    /// </summary>
    bool IsReachable(int from, int to)
    {
        if (from == to)
            return true;

        if (Threat(to) < clearDistance)
            return false;

        // Debilitante não bate, então não entra na conta do tempo; mas passar
        // por cima de um é pegá-lo, e o piloto não escolhe isso.
        int step = to > from ? 1 : -1;
        for (int lane = from + step; lane != to; lane += step)
        {
            if (DebuffThreat(lane) < clearDistance)
                return false;
        }

        return CrossingIsSafe(to);
    }

    /// <summary>
    /// A nave sai de onde está agora e desliza até a faixa <paramref name="to"/>
    /// na velocidade de troca de faixa. Algum obstáculo a alcança no caminho?
    ///
    /// É a mesma batida do <see cref="Obstacle"/>, prevista: o obstáculo bate
    /// enquanto estiver a menos do alcance dele na vertical, então ele tem uma
    /// **janela de tempo**; nessa janela a nave percorre um trecho de lado; se
    /// esse trecho encosta no alcance horizontal do obstáculo, é batida.
    ///
    /// Inclui o obstáculo de que se está fugindo: se não der tempo de sair da
    /// frente dele, a saída não serve — e é aqui que a velocidade da corrida
    /// passa a contar.
    /// </summary>
    bool CrossingIsSafe(int to)
    {
        float speed = race != null ? race.Current : 0f;
        float lateral = Mathf.Max(0.1f, lanes.LaneChangeSpeed);
        float shipX = transform.position.x;
        float shipY = transform.position.y;
        float targetX = track.LaneCenterX(to);

        foreach (var obstacle in Obstacle.Active)
        {
            if (obstacle == null || obstacle.Stats == null)
                continue;

            float dy = obstacle.transform.position.y - shipY;

            // O que já está na faixa de destino, além da folga, é problema da
            // próxima decisão: a regra do destino já o aceitou.
            if (obstacle.Occupies(to) && dy >= clearDistance)
                continue;

            float reach = obstacle.Stats.crashDistance;
            if (dy < -reach)
                continue;

            float horizontal = reach + (obstacle.LaneSpan - 1) * 0.5f * track.LaneWidth + crossingMargin;

            // Um quadro de folga na entrada: quem decide e quem anda rodam em
            // quadros, e o obstáculo dá um passo inteiro antes de a nave dar o dela.
            float enter, exit;
            if (speed <= 0.01f)
            {
                if (Mathf.Abs(dy) > reach)
                    continue;

                enter = 0f;
                exit = float.PositiveInfinity;
            }
            else
            {
                enter = Mathf.Max(0f, (dy - reach) / speed - Time.deltaTime);
                exit = (dy + reach) / speed;
            }

            float xEnter = Mathf.MoveTowards(shipX, targetX, lateral * enter);
            float xExit = float.IsPositiveInfinity(exit)
                ? targetX
                : Mathf.MoveTowards(shipX, targetX, lateral * exit);

            float obstacleX = obstacle.transform.position.x;
            if (Mathf.Max(xEnter, xExit) >= obstacleX - horizontal &&
                Mathf.Min(xEnter, xExit) <= obstacleX + horizontal)
                return false;
        }

        return true;
    }

    /// <summary>Distância até o Debilitante mais perto nesta faixa, ou <see cref="lookAhead"/>.</summary>
    float DebuffThreat(int lane)
    {
        float nearest = lookAhead;
        float shipY = transform.position.y;

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
