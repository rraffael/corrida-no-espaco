using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Coisa que vem vindo pela faixa. Tem vida — dá para destruir a tiro — e dá
/// dano na nave se bater. Destruir acelera a corrida; bater freia e machuca.
///
/// Sem Physics2D de propósito: o jogo inteiro é matemática de posição
/// (<see cref="LaneTrack"/>, <see cref="ScrollingBackground"/>), e distância
/// entre dois pontos é mais previsível que colisor mal configurado.
/// </summary>
[RequireComponent(typeof(Health))]
public class Obstacle : MonoBehaviour
{
    /// <summary>Todos os obstáculos vivos. O projétil percorre esta lista para achar no que bateu.</summary>
    public static readonly List<Obstacle> Active = new List<Obstacle>();

    [Tooltip("Dano na nave quando encosta nela.")]
    [SerializeField, Min(0f)] float contactDamage = 20f;

    [Tooltip("Quanto a corrida perde de velocidade na batida.")]
    [SerializeField, Min(0f)] float speedPenaltyOnCrash = 2f;

    [Tooltip("Quanto a corrida ganha de velocidade quando este obstáculo é destruído.")]
    [SerializeField, Min(0f)] float speedBonusOnKill = 1.5f;

    [Tooltip("Distância que conta como batida na nave, em unidades de mundo.")]
    [SerializeField, Min(0.1f)] float crashDistance = 0.7f;

    [Tooltip("Distância que conta como acerto de tiro, em unidades de mundo.")]
    [SerializeField, Min(0.1f)] float hitDistance = 0.5f;

    [Tooltip("Abaixo deste Y o obstáculo já passou da nave e some.")]
    [SerializeField] float despawnY = -7f;

    /// <summary>Raio de acerto para o projétil consultar.</summary>
    public float HitDistance => hitDistance;

    Health health;
    RaceSpeed race;

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

    /// <summary>Configuração vinda do spawner, para o obstáculo não carregar os números dele.</summary>
    public void Configure(float maxHealth, float damage)
    {
        health = GetComponent<Health>();
        health.Configure(maxHealth);
        contactDamage = damage;
    }

    public void TakeDamage(float amount) => health.TakeDamage(amount);

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
        if (ship == null || !ship.Health.IsAlive)
            return;

        // Distância no plano, e não faixa contra faixa: assim bater no meio de
        // uma troca de faixa conta, que é o que o jogador vê acontecer.
        if (Vector2.Distance(transform.position, ship.transform.position) > crashDistance)
            return;

        ship.Health.TakeDamage(contactDamage);
        race?.Nudge(-speedPenaltyOnCrash);

        // O obstáculo se desfaz na batida: ele já cobrou o preço dele, e deixá-lo
        // grudado na nave cobraria de novo no frame seguinte.
        Destroy(gameObject);
    }

    void OnDied()
    {
        race?.Nudge(speedBonusOnKill);
        Destroy(gameObject);
    }
}
