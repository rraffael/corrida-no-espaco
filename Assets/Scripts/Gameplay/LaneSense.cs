using UnityEngine;

/// <summary>
/// Como um piloto **lê a pista**: a que distância está a ameaça de cada faixa,
/// para onde dá para fugir, e se a travessia chega antes do obstáculo.
///
/// Saiu de dentro do <see cref="Autopilot"/> em 18/10/2026, para o piloto do
/// simulador (<see cref="SimulatedPilot"/>) pensar com o **mesmo cérebro** da
/// Super IA — só que com reflexo, alcance e erro de gente. Uma cópia separada
/// divergiria na primeira correção feita em uma das duas. As contas são as que
/// estavam no Autopilot, linha por linha; o que muda de um piloto para o outro
/// são os números abaixo.
///
/// <para>
/// **Não enxerga estilhaço** — nem antes, dentro da Super IA. O estilhaço não
/// tem lista de vivos como o obstáculo tem; ver o ROADMAP.
/// </para>
/// </summary>
public class LaneSense
{
    /// <summary>A que distância um obstáculo na faixa da nave passa a ser motivo para sair dela.</summary>
    public float DangerDistance = 5f;

    /// <summary>Folga mínima de uma faixa, na altura da nave, para valer como destino.</summary>
    public float ClearDistance = 7f;

    /// <summary>Até que distância ele enxerga. Além disso, a faixa conta como livre.</summary>
    public float LookAhead = 14f;

    /// <summary>Quanto um prêmio na faixa vale, em distância de folga fingida. Só desempate.</summary>
    public float RewardBonus = 2f;

    /// <summary>Folga de lado somada ao alcance de batida de cada obstáculo, na conta do cruzamento.</summary>
    public float CrossingMargin = 0.15f;

    /// <summary>
    /// Debilitante conta como estorvo. A Super IA sempre desvia; o piloto
    /// novato do simulador nem repara nele.
    /// </summary>
    public bool AvoidsDebuffs = true;

    readonly Transform ship;
    readonly ShipLaneController lanes;
    readonly LaneTrack track;

    public LaneSense(Transform ship, ShipLaneController lanes, LaneTrack track)
    {
        this.ship = ship;
        this.lanes = lanes;
        this.track = track;
    }

    public int LaneCount => track.LaneCount;

    /// <summary>
    /// A melhor faixa que dá para alcançar **sem atravessar perigo**. Falso
    /// quando não existe nenhuma: aí a nave fica onde está e leva o golpe, que é
    /// melhor do que sair correndo para bater em outro.
    /// </summary>
    public bool TryPickEscape(int current, out int escape)
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
    public bool IsReachable(int from, int to)
    {
        if (from == to)
            return true;

        if (Threat(to) < ClearDistance)
            return false;

        // Debilitante não bate, então não entra na conta do tempo; mas passar
        // por cima de um é pegá-lo, e o piloto não escolhe isso.
        if (AvoidsDebuffs)
        {
            int step = to > from ? 1 : -1;
            for (int lane = from + step; lane != to; lane += step)
            {
                if (DebuffThreat(lane) < ClearDistance)
                    return false;
            }
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
    public bool CrossingIsSafe(int to)
    {
        var race = RaceSpeed.Instance;
        float speed = race != null ? race.Current : 0f;
        float lateral = Mathf.Max(0.1f, lanes.LaneChangeSpeed);
        float shipX = ship.position.x;
        float shipY = ship.position.y;
        float targetX = track.LaneCenterX(to);

        foreach (var obstacle in Obstacle.Active)
        {
            if (obstacle == null || obstacle.Stats == null)
                continue;

            float dy = obstacle.transform.position.y - shipY;

            // O que já está na faixa de destino, além da folga, é problema da
            // próxima decisão: a regra do destino já o aceitou.
            if (obstacle.Occupies(to) && dy >= ClearDistance)
                continue;

            float reach = obstacle.Stats.crashDistance;
            if (dy < -reach)
                continue;

            float horizontal = reach + (obstacle.LaneSpan - 1) * 0.5f * track.LaneWidth + CrossingMargin;

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

    /// <summary>Distância até o Debilitante mais perto nesta faixa, ou <see cref="LookAhead"/>.</summary>
    float DebuffThreat(int lane)
    {
        float nearest = LookAhead;
        float shipY = ship.position.y;

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
    /// <see cref="LookAhead"/> quando não há nenhum. Debilitante conta como
    /// estorvo para quem desvia dele: desviar é parte de pilotar bem.
    /// </summary>
    public float Threat(int lane)
    {
        float nearest = LookAhead;
        float shipY = ship.position.y;

        foreach (var obstacle in Obstacle.Active)
        {
            if (obstacle == null || !obstacle.Occupies(lane))
                continue;

            float distance = obstacle.transform.position.y - shipY;
            if (distance >= 0f && distance < nearest)
                nearest = distance;
        }

        if (AvoidsDebuffs)
            nearest = Mathf.Min(nearest, DebuffThreat(lane));

        return nearest;
    }

    /// <summary>
    /// O primeiro obstáculo desta faixa à frente da nave, dentro do alcance da
    /// vista. Nulo: não há. Para o piloto do simulador escolher o que caçar.
    /// </summary>
    public Obstacle NearestAhead(int lane)
    {
        Obstacle nearest = null;
        float best = LookAhead;
        float shipY = ship.position.y;

        foreach (var obstacle in Obstacle.Active)
        {
            if (obstacle == null || !obstacle.Occupies(lane))
                continue;

            float distance = obstacle.transform.position.y - shipY;
            if (distance >= 0f && distance < best)
            {
                best = distance;
                nearest = obstacle;
            }
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
        if (RewardBonus <= 0f)
            return 0f;

        float shipY = ship.position.y;

        foreach (var pickup in LevelModifierPickup.Active)
        {
            if (pickup == null || pickup.Lane != lane || pickup.Definition == null)
                continue;

            if (pickup.Definition.category == LevelModifier.Category.Debilitante)
                continue;

            float distance = pickup.transform.position.y - shipY;
            if (distance >= 0f && distance <= LookAhead)
                return RewardBonus;
        }

        return 0f;
    }
}
