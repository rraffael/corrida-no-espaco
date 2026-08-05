using UnityEngine;

/// <summary>
/// A ficha da nave: os atributos que definem do que ela é capaz. É aqui que se
/// mexe para a nave ficar melhor ou pior, e é daqui que o resto do jogo lê —
/// arma, corrida e HUD não carregam número nenhum por conta própria.
///
/// Por enquanto são quatro atributos. Quando entrarem outros (escudo, manobra,
/// o que vier), entram neste arquivo e ninguém mais precisa mudar.
/// </summary>
[DefaultExecutionOrder(-55)]
[RequireComponent(typeof(Health))]
public class ShipStats : MonoBehaviour
{
    public static ShipStats Instance { get; private set; }

    [Header("Ficha da nave")]
    [Tooltip("Velocidade de cruzeiro: o padrão para onde a nave acelera sozinha, em unidades por segundo.")]
    [SerializeField, Min(0f)] float cruiseSpeed = 8f;

    [Tooltip("Dano de cada tiro.")]
    [SerializeField, Min(0f)] float damage = 25f;

    [Tooltip("Velocidade de ataque, em tiros por segundo.")]
    [SerializeField, Min(0.1f)] float attackSpeed = 3f;

    [Tooltip("Vida cheia da nave. Zero é derrota.")]
    [SerializeField, Min(1f)] float maxHealth = 100f;

    public float CruiseSpeed => cruiseSpeed;
    public float Damage => damage;
    public float AttackSpeed => attackSpeed;
    public float MaxHealth => maxHealth;

    /// <summary>Segundos entre um tiro e o próximo.</summary>
    public float ShotInterval => 1f / Mathf.Max(0.1f, attackSpeed);

    public Health Health { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        Health = GetComponent<Health>();
        Health.Configure(maxHealth);
    }

    void Start()
    {
        // A corrida nasce com um número provisório no RaceSpeed; a ficha chega
        // depois e impõe o dela. É o ponto de troca que estava prometido lá.
        if (RaceSpeed.Instance != null)
            RaceSpeed.Instance.ApplyShipStats(cruiseSpeed);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
