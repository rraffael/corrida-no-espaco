using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Coisa que vem vindo pelo corredor. O comportamento é um só; o que muda de um
/// tipo para outro está na ficha (<see cref="ObstacleStats"/>) — vida, dano,
/// quantas faixas ocupa e se explode em estilhaços.
///
/// Sem Physics2D de propósito: o jogo inteiro é matemática de posição, e
/// distância entre dois pontos é mais previsível que colisor mal configurado.
/// </summary>
[RequireComponent(typeof(Health))]
public class Obstacle : MonoBehaviour
{
    /// <summary>Todos os obstáculos vivos. O tiro percorre isto, e o spawner também.</summary>
    public static readonly List<Obstacle> Active = new List<Obstacle>();

    [SerializeField] ObstacleStats stats;

    [Tooltip("Abaixo deste Y o obstáculo já passou da nave e some.")]
    [SerializeField] float despawnY = -7f;

    /// <summary>Primeira faixa ocupada. Com laneSpan 2, ocupa esta e a seguinte.</summary>
    public int Lane { get; private set; }

    public int LaneSpan => stats != null ? Mathf.Max(1, stats.laneSpan) : 1;

    public float HitDistance => stats != null ? stats.hitDistance : 0.5f;

    public ObstacleStats Stats => stats;

    Health health;
    RaceSpeed race;
    LaneTrack track;
    float contactDamage;

    void Awake()
    {
        health = GetComponent<Health>();
        race = RaceSpeed.Instance;
    }

    void OnEnable()
    {
        Active.Add(this);
        health.Died += OnDied;
    }

    void OnDisable()
    {
        Active.Remove(this);
        health.Died -= OnDied;
    }

    /// <summary>
    /// Configuração vinda do spawner. Os fatores são os da dificuldade: a mesma
    /// ficha rende um obstáculo mais duro no Difícil sem existir uma ficha por
    /// dificuldade.
    /// </summary>
    public void Configure(ObstacleStats ficha, LaneTrack laneTrack, int lane,
                          float healthFactor, float damageFactor)
    {
        stats = ficha;
        track = laneTrack;
        Lane = lane;

        health = GetComponent<Health>();
        health.Configure(stats.maxHealth * healthFactor);
        contactDamage = stats.contactDamage * damageFactor;
    }

    public void TakeDamage(float amount) => health.TakeDamage(amount);

    /// <summary>True se este obstáculo ocupa a faixa indicada.</summary>
    public bool Occupies(int lane) => lane >= Lane && lane < Lane + LaneSpan;

    void Update()
    {
        // Anda na velocidade da corrida: quanto mais rápido a nave, mais rápido
        // o obstáculo vem para cima dela.
        float step = (race != null ? race.Current : 0f) * Time.deltaTime;
        transform.position += Vector3.down * step;

        WatchPass();

        if (transform.position.y < despawnY)
        {
            // Passou pela nave e saiu de cena sem ser destruído: é o obstáculo
            // que o jogador desviou. Poder medido em "X obstáculos" gasta carga
            // aqui.
            if (stats != null)
            {
                if (pullArmed)
                    PullShip();

                // Os dois sistemas de poder são avisados em separado, e nenhum sabe
                // do outro. Quem os junta é a nave, aqui.
                if (LevelModifiers.Instance != null)
                    LevelModifiers.Instance.NotifyObstaclePassed(stats);
                if (ShipAbilities.Instance != null)
                    ShipAbilities.Instance.NotifyObstaclePassed(stats);
            }

            Destroy(gameObject);
            return;
        }

        CheckCrash();
    }

    /// <summary>A nave estava dentro deste obstáculo, atravessando, e ainda não saiu.</summary>
    bool phasedThrough;

    void CheckCrash()
    {
        var ship = ShipStats.Instance;
        if (ship == null || !ship.Health.IsAlive || stats == null)
            return;

        // Distância no plano, e não faixa contra faixa: bater no meio de uma
        // troca de faixa conta, que é o que o jogador vê acontecer. Com obstáculo
        // largo, a folga horizontal cresce junto com a largura dele.
        var delta = ship.transform.position - transform.position;
        float horizontalReach = stats.crashDistance + (LaneSpan - 1) * 0.5f * LaneWidth();
        bool touching = Mathf.Abs(delta.x) <= horizontalReach && Mathf.Abs(delta.y) <= stats.crashDistance;

        // Atravessando: a nave passa por dentro e o obstáculo segue inteiro, para
        // ser destruído ou desviado depois como qualquer outro.
        //
        // E a carência *(decisão do Raffael, 18/10/2026)*: o que estava sendo
        // atravessado quando o efeito acabou não bate mais, até a nave sair de
        // dentro dele. Sem ela, os 3 s acabando no meio de uma Barcaça davam uma
        // batida que o jogador não tinha como evitar.
        if (ship.Has(ShipTrait.Phasing))
        {
            phasedThrough = touching;
            return;
        }

        if (!touching)
        {
            phasedThrough = false;
            return;
        }

        if (phasedThrough)
            return;

        // Pela nave, e não direto na vida: é lá que a defesa desconta e que os
        // poderes têm chance de segurar o golpe.
        var hit = ship.TakeHit(contactDamage);

        if (hit.BlockTimeCost)
        {
            // A batida não aconteceu — foi o escudo que desfez o obstáculo. Então
            // ele morre como quem é destruído, e um Casulo solta estilhaço na
            // cara da nave. E o escudo já foi gasto neste mesmo golpe: se era a
            // última carga, ele some antes de o estilhaço chegar, e a nave leva.
            //
            // A troca é essa, e é de propósito: o escudo transforma uma batida
            // grande em vários raspões. Ainda vale muito a pena, mas não sai de
            // graça — e destruir Casulo com escudo continua sendo escolha, não
            // resposta automática.
            //
            // A menos que o poder diga o contrário: um escudo melhorado pode
            // calar o que o obstáculo soltaria, e aí a batida some inteira.
            if (stats.shrapnelOnDeath && !hit.BlockDeathEffects)
                SpawnShrapnel();
        }
        else
        {
            // O custo de tempo é cobrado à parte do dano, e um poder pode segurar
            // só um dos dois. Quem decide é o poder; aqui só se obedece.
            race?.Crash(stats.speedPenaltyOnCrash);
            CareerStats.AddCrash();
        }

        // O obstáculo se desfaz na batida: já cobrou o preço dele, e deixá-lo
        // grudado na nave cobraria de novo no frame seguinte.
        Destroy(gameObject);
    }

    float LaneWidth() => track != null ? track.LaneWidth : 1.6f;

    // ── Vácuo (o Rastro) ─────────────────────────────────────────────────

    bool passChecked;
    bool pullArmed;

    /// <summary>
    /// O instante em que o obstáculo cruza a altura da nave decide se ele vai
    /// puxar: só puxa quem passou **na faixa vizinha**. Decidir aqui, e não na
    /// hora de sumir, é o que faz a regra ser legível — o jogador vê o Rastro
    /// passar colado nele e já sabe o que vem.
    /// </summary>
    void WatchPass()
    {
        if (passChecked || stats == null || !stats.pullsOnPass)
            return;

        var ship = ShipStats.Instance;
        if (ship == null || transform.position.y > ship.transform.position.y)
            return;

        passChecked = true;

        var lanes = ShipLanes(ship);
        if (lanes == null)
            return;

        // Imune passando ao lado, não puxa depois — mesmo que o efeito já tenha
        // acabado quando o Rastro sumir da tela.
        int lane = lanes.CurrentLane;
        pullArmed = !ship.IsUntouchable && (lane == Lane - 1 || lane == Lane + LaneSpan);
    }

    /// <summary>
    /// Puxa a nave uma faixa na direção do obstáculo. Uma faixa só, mesmo que
    /// ela já tenha se afastado: o vácuo é forte perto e fraco longe, e uma nave
    /// que fugiu duas faixas sente só um tranco.
    /// </summary>
    void PullShip()
    {
        var ship = ShipStats.Instance;
        if (ship == null || !ship.Health.IsAlive || ship.IsUntouchable)
            return;

        if (RaceDirector.Instance != null && !RaceDirector.Instance.IsRunning)
            return;

        var lanes = ShipLanes(ship);
        if (lanes == null)
            return;

        int lane = lanes.CurrentLane;
        if (Occupies(lane))
            return;

        lanes.MoveTo(lane < Lane ? lane + 1 : lane - 1);
    }

    static ShipLaneController shipLanes;

    static ShipLaneController ShipLanes(ShipStats ship)
    {
        if (shipLanes == null || shipLanes.gameObject != ship.gameObject)
            shipLanes = ship.GetComponent<ShipLaneController>();

        return shipLanes;
    }

    void OnDied()
    {
        if (stats != null)
        {
            race?.Nudge(SpeedGainOnKill());

            // Morreu de tiro (a batida destrói o objeto por outro caminho), então
            // é abate de verdade e os poderes podem reagir.
            if (LevelModifiers.Instance != null)
                LevelModifiers.Instance.NotifyObstacleDestroyed(stats);
            if (ShipAbilities.Instance != null)
                ShipAbilities.Instance.NotifyObstacleDestroyed(stats);

            CareerStats.AddKill();

            if (stats.shrapnelOnDeath)
                SpawnShrapnel();
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// O que este abate rende de velocidade. A ficha do obstáculo diz o **peso**
    /// dele, a ficha da nave diz quanta velocidade a nave tira de um peso 1 — as
    /// duas coisas são separadas porque a Barcaça valer mais que o Detrito é
    /// propriedade da Barcaça, e converter abate em velocidade é da nave.
    ///
    /// Sem nave na cena — Game.unity aberta solta para teste — o peso vale como
    /// velocidade, para a cena continuar jogável.
    /// </summary>
    float SpeedGainOnKill()
    {
        var ship = ShipStats.Instance;
        return ship != null ? ship.KillSpeedGain * stats.killWeight : stats.killWeight;
    }

    /// <summary>
    /// Estilhaços descem pelas faixas que o obstáculo ocupava. É o que impede o
    /// jogador de ficar parado numa faixa só atirando em tudo: destruir este tipo
    /// cria um perigo exatamente onde ele está.
    /// </summary>
    void SpawnShrapnel()
    {
        int count = Mathf.Max(1, stats.shrapnelCount);

        for (int i = 0; i < count; i++)
        {
            // Espalha pelas faixas ocupadas; com uma faixa só, todos saem dela.
            int lane = Lane + (LaneSpan > 1 ? i % LaneSpan : 0);
            float x = track != null ? track.LaneCenterX(lane) : transform.position.x;

            var piece = new GameObject("Estilhaco");
            piece.transform.position = new Vector3(x, transform.position.y, 0f);

            var renderer = piece.AddComponent<SpriteRenderer>();
            renderer.sprite = stats.sprite;
            renderer.color = new Color(1f, 0.85f, 0.4f, 1f);
            renderer.sortingOrder = 9;

            if (stats.sprite != null)
            {
                var size = stats.sprite.bounds.size;
                if (size.x > 0f && size.y > 0f)
                    piece.transform.localScale = new Vector3(0.25f / size.x, 0.25f / size.y, 1f);
            }

            piece.AddComponent<Shrapnel>().Configure(stats.shrapnelDamage, stats.shrapnelSpeed, despawnY);
        }
    }
}
