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

    [Tooltip("Quanto a velocidade muda por segundo. Manda em quão rápido a nave se recupera " +
             "de uma freada — 4 leva 2s do zero até a velocidade de cruzeiro.")]
    [SerializeField, Min(0.1f)] float acceleration = 4f;

    [Tooltip("Dano de cada tiro.")]
    [SerializeField, Min(0f)] float damage = 25f;

    [Tooltip("Velocidade de ataque, em tiros por segundo.")]
    [SerializeField, Min(0.1f)] float attackSpeed = 3f;

    [Tooltip("Vida cheia da nave. Zero é derrota.")]
    [SerializeField, Min(1f)] float maxHealth = 100f;

    [Tooltip("Redução de dano, em porcentagem. O dano recebido é sempre arredondado para cima, " +
             "então nem 99% de defesa deixa a nave imune: um golpe sempre tira pelo menos 1.")]
    [SerializeField, Range(0f, 99f)] float defensePercent = 0f;

    [Tooltip("Fração da aceleração da nave que cada obstáculo de PESO 1 rende ao ser destruído. " +
             "0,16 com aceleração 4 dá 0,64 u/s por abate, ou uns 4 segundos de ganho passivo. " +
             "Sai da aceleração, e não de um número solto: uma nave mais potente converte abate " +
             "em velocidade mais depressa, e o abate vale sempre os mesmos segundos de paciência.")]
    [SerializeField, Min(0f)] float killGainFactor = 0.16f;

    public float CruiseSpeed => cruiseSpeed;
    public float Acceleration => acceleration;
    public float Damage => damage;
    public float AttackSpeed => attackSpeed;
    public float MaxHealth => maxHealth;
    public float DefensePercent => defensePercent;

    /// <summary>
    /// Velocidade que um obstáculo de **peso 1** rende ao morrer. A ficha do
    /// obstáculo diz quanto ele vale em relação aos outros; esta ficha diz quanto
    /// a nave tira disso.
    ///
    /// É proporcional à aceleração porque o ganho passivo também é: assim um
    /// abate vale **os mesmos segundos de paciência** em qualquer nave, e a
    /// escolha entre atirar e desviar continua valendo a mesma coisa quando as
    /// naves mudarem. Destruir compensa, desviar também fecha a corrida — só
    /// mais devagar.
    /// </summary>
    public float KillSpeedGain => acceleration * killGainFactor;

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
        // A corrida nasce com números de partida no RaceSpeed; a ficha chega
        // depois e impõe os dela. É o ponto de troca que estava prometido lá.
        if (RaceSpeed.Instance != null)
            RaceSpeed.Instance.ApplyShipStats(cruiseSpeed, acceleration);
    }

    /// <summary>
    /// Dano que sobra depois da defesa. **Sempre arredondado para cima**: um
    /// golpe que acerta nunca é de graça, nem contra 99% de redução.
    /// </summary>
    public float DamageAfterDefense(float rawDamage)
    {
        if (rawDamage <= 0f)
            return 0f;

        float reduced = rawDamage * (1f - defensePercent / 100f);
        return Mathf.Max(1f, Mathf.Ceil(reduced));
    }

    /// <summary>Bater na nave é por aqui: a defesa entra antes da vida.</summary>
    public void TakeHit(float rawDamage) => Health.TakeDamage(DamageAfterDefense(rawDamage));

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
