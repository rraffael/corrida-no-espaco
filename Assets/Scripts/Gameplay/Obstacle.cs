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

        if (transform.position.y < despawnY)
        {
            Destroy(gameObject);
            return;
        }

        CheckCrash();
    }

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

        if (Mathf.Abs(delta.x) > horizontalReach || Mathf.Abs(delta.y) > stats.crashDistance)
            return;

        // Pela ficha, e não direto na vida: é lá que a defesa da nave desconta.
        ship.TakeHit(contactDamage);
        race?.Crash(stats.speedPenaltyOnCrash);

        // O obstáculo se desfaz na batida: já cobrou o preço dele, e deixá-lo
        // grudado na nave cobraria de novo no frame seguinte.
        Destroy(gameObject);
    }

    float LaneWidth() => track != null ? track.LaneWidth : 1.6f;

    void OnDied()
    {
        if (stats != null)
        {
            race?.Nudge(SpeedGainOnKill());

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
