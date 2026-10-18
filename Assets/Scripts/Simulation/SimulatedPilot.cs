using UnityEngine;

/// <summary>
/// O piloto do simulador de corridas: uma <see cref="PilotPersona"/> jogando de
/// verdade a Game.unity, no lugar do dedo.
///
/// **Joga pelas mesmas portas do jogador.** Troca de faixa pelo
/// <see cref="ShipLaneController.MoveTo"/>, uma faixa por arraste; ativa o poder
/// pelo <see cref="ShipAbilities.Activate"/>, como o toque duplo. Nada de atalho
/// que o jogador não tenha — senão o número não diria nada sobre o jogo.
///
/// **Lê a pista com o cérebro da Super IA** (<see cref="LaneSense"/>), e o
/// que a persona muda é o que separa gente de máquina: o atraso entre ver e
/// agir, quanto ele enxerga, os erros, e se ele vai atrás de obstáculo para
/// ganhar velocidade.
///
/// **Os sorteios dele usam um dado próprio**, e não o <see cref="Random"/> da
/// Unity: assim errar mais ou menos não muda a sequência de obstáculos que a
/// fase sorteia, e as cinco personas largam na mesma pista.
/// </summary>
public class SimulatedPilot : MonoBehaviour
{
    /// <summary>De quanto em quanto tempo de pista calma ele pensa em caçar.</summary>
    const float HuntCheckSeconds = 0.5f;

    /// <summary>Média de segundos entre um uso e outro do poder, no hábito "à toa".</summary>
    const float RandomPowerSeconds = 12f;

    PilotPersona persona;
    System.Random dice;

    ShipStats stats;
    ShipLaneController lanes;
    LaneSense sense;

    /// <summary>
    /// O relógio **do piloto**, e não o da corrida. No tempo lento a corrida anda
    /// pela metade, mas o reflexo de quem joga não fica mais lento por isso — é
    /// exatamente a vantagem que o modificador dá. Este relógio anda em tempo de
    /// gente.
    /// </summary>
    float clock;

    float nextThinkAt;
    float nextHuntAt;
    float distractedUntil;

    /// <summary>Faixa para onde ele decidiu ir. Negativo: nenhuma.</summary>
    int targetLane = -1;
    float moveAt;

    bool powerPending;
    float powerAt;

    /// <summary>Quantas vezes o poder foi ativado nesta corrida.</summary>
    public int PowerUses { get; private set; }

    public void Configure(PilotPersona pilotPersona, int seed)
    {
        persona = pilotPersona;
        dice = new System.Random(seed);
    }

    void Start()
    {
        stats = GetComponent<ShipStats>();
        lanes = GetComponent<ShipLaneController>();
        var track = LaneTrack.Instance != null ? LaneTrack.Instance : FindAnyObjectByType<LaneTrack>();

        if (persona == null || stats == null || lanes == null || track == null)
        {
            enabled = false;
            return;
        }

        sense = new LaneSense(transform, lanes, track)
        {
            DangerDistance = persona.dangerDistance,
            ClearDistance = persona.clearDistance,
            LookAhead = persona.lookAhead,
            RewardBonus = persona.seeksRewards ? 2f : 0f,
            AvoidsDebuffs = persona.avoidsDebuffs,
        };
    }

    void Update()
    {
        if (GameTime.IsPaused || !stats.Health.IsAlive)
            return;

        if (RaceDirector.Instance != null && !RaceDirector.Instance.IsRunning)
            return;

        clock += Time.deltaTime / Mathf.Max(0.05f, GameTime.SlowFactor);

        // A Super IA ligada é o jogo no comando: o arraste do jogador é ignorado,
        // e o do piloto também.
        if (stats.Has(ShipTrait.Autopilot))
        {
            targetLane = -1;
            return;
        }

        if (powerPending && clock >= powerAt)
        {
            powerPending = false;
            if (ShipAbilities.Instance != null && ShipAbilities.Instance.Activate())
                PowerUses++;
        }

        if (targetLane >= 0)
        {
            Swipe();
            return;
        }

        if (clock < nextThinkAt)
            return;

        nextThinkAt = clock + persona.thinkInterval;
        Think();
    }

    /// <summary>
    /// Executa a decisão já tomada, uma faixa por arraste. **Não confere de
    /// novo** se ainda é boa ideia: decidiu, o dedo vai. É o que o atraso de
    /// reação custa.
    /// </summary>
    void Swipe()
    {
        if (clock < moveAt)
            return;

        int current = lanes.CurrentLane;
        if (current == targetLane)
        {
            targetLane = -1;
            return;
        }

        int next = persona.swipeInterval <= 0f
            ? targetLane
            : current + (targetLane > current ? 1 : -1);

        lanes.MoveTo(next);
        moveAt = clock + persona.swipeInterval;

        if (next == targetLane)
            targetLane = -1;
    }

    void Think()
    {
        int current = lanes.CurrentLane;

        if (sense.Threat(current) <= persona.dangerDistance)
        {
            Dodge(current);
            return;
        }

        ConsiderPower(trapped: false, current);

        // Pista calma também tem erro: uma troca de faixa sem motivo, de vez em
        // quando — e às vezes para dentro do problema.
        if (Roll(persona.mistakeChance * Mathf.Max(persona.thinkInterval, Time.deltaTime) * 0.5f))
        {
            Commit(Neighbor(current));
            return;
        }

        if (clock >= nextHuntAt)
        {
            nextHuntAt = clock + HuntCheckSeconds;
            if (Roll(persona.huntChance))
                Hunt(current);
        }
    }

    void Dodge(int current)
    {
        if (clock < distractedUntil)
            return;

        if (Roll(persona.mistakeChance))
        {
            // Metade das vezes ele trava; metade foge para o lado sem olhar.
            if (Roll(0.5f))
                distractedUntil = clock + Mathf.Max(0.2f, persona.reactionSeconds * 2f);
            else
                Commit(Neighbor(current));

            return;
        }

        if (sense.TryPickEscape(current, out int escape))
        {
            Commit(escape);
            return;
        }

        ConsiderPower(trapped: true, current);
    }

    /// <summary>
    /// Vai para a faixa de um obstáculo que **dá tempo de destruir** antes de
    /// ele virar perigo. É assim que quem joga bem ganha velocidade: abate vale
    /// velocidade, e só desviar deixa a nave no ganho passivo.
    /// </summary>
    void Hunt(int current)
    {
        // Já tem alvo na frente e longe o bastante: fica e atira.
        if (IsGoodTarget(sense.NearestAhead(current)))
            return;

        int best = -1;
        for (int lane = 0; lane < sense.LaneCount; lane++)
        {
            if (lane == current || !IsGoodTarget(sense.NearestAhead(lane)))
                continue;

            if (!sense.CrossingIsSafe(lane))
                continue;

            if (best < 0 || Mathf.Abs(lane - current) < Mathf.Abs(best - current))
                best = lane;
        }

        if (best >= 0)
            Commit(best);
    }

    bool IsGoodTarget(Obstacle obstacle)
    {
        if (obstacle == null)
            return false;

        float distance = obstacle.transform.position.y - transform.position.y;
        if (distance < persona.clearDistance)
            return false;

        var health = obstacle.GetComponent<Health>();
        float damage = Mathf.Max(0.01f, stats.Damage);
        float shots = health != null ? Mathf.Ceil(health.Current / damage) : 1f;
        if (stats.AttackSpeed <= 0f)
            return false; // arma calada: não há o que caçar

        float killSeconds = shots * stats.ShotInterval + persona.reactionSeconds;

        var race = RaceSpeed.Instance;
        float speed = race != null ? Mathf.Max(0.01f, race.Current) : 1f;
        float secondsUntilDanger = (distance - persona.dangerDistance) / speed;

        return killSeconds < secondsUntilDanger;
    }

    void ConsiderPower(bool trapped, int current)
    {
        var abilities = ShipAbilities.Instance;
        if (powerPending || abilities == null || abilities.Equipped == null || !abilities.IsReady)
            return;

        bool use;
        switch (persona.powerHabit)
        {
            case PilotPersona.PowerHabit.AToa:
                use = Roll(Mathf.Max(persona.thinkInterval, Time.deltaTime) / RandomPowerSeconds);
                break;

            case PilotPersona.PowerHabit.NoAperto:
                use = trapped;
                break;

            case PilotPersona.PowerHabit.Esperto:
                use = trapped || ChargingUnderThreat(current) || (!abilities.IsLimited && ObstaclesAhead() >= 2);
                break;

            default:
                use = false;
                break;
        }

        if (!use)
            return;

        powerPending = true;
        powerAt = clock + persona.reactionSeconds;
    }

    /// <summary>Carregando a dobra, e o perigo chegando: é a hora de gastar o que guardou.</summary>
    bool ChargingUnderThreat(int current)
    {
        var director = RaceDirector.Instance;
        return director != null && director.IsCharging &&
               sense.Threat(current) <= persona.dangerDistance * 1.5f;
    }

    int ObstaclesAhead()
    {
        int count = 0;
        float shipY = transform.position.y;

        foreach (var obstacle in Obstacle.Active)
        {
            if (obstacle == null)
                continue;

            float distance = obstacle.transform.position.y - shipY;
            if (distance >= 0f && distance <= persona.lookAhead)
                count++;
        }

        return count;
    }

    void Commit(int lane)
    {
        if (lane == lanes.CurrentLane)
            return;

        targetLane = lane;
        moveAt = clock + persona.reactionSeconds;
    }

    int Neighbor(int current)
    {
        if (current <= 0)
            return Mathf.Min(1, sense.LaneCount - 1);

        if (current >= sense.LaneCount - 1)
            return current - 1;

        return Roll(0.5f) ? current - 1 : current + 1;
    }

    bool Roll(float chance) => chance > 0f && dice.NextDouble() < chance;
}
