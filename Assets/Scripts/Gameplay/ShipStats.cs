using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A nave viva: o que a nave **está** durante esta corrida. Os números de base
/// vêm da ficha (<see cref="ShipDefinition"/>), um arquivo em
/// <c>Assets/Ships/</c>; aqui fica o que só existe enquanto a partida roda — a
/// vida, os poderes mexendo nos atributos, o desconto da defesa, e a ponte com a
/// corrida e com a arma.
///
/// **Por que a divisão.** A ficha é um asset compartilhado e de leitura: mexer
/// nela em tempo de jogo alteraria o arquivo em disco e o valor sobreviveria ao
/// fim da corrida. Este componente é o lugar de tudo que muda durante a partida.
///
/// **Todo mundo lê daqui, e não da ficha** — arma, corrida e HUD não carregam
/// número nenhum por conta própria, e não precisam saber se o valor veio gravado
/// ou se um poder mexeu nele há dois segundos.
/// </summary>
[DefaultExecutionOrder(-55)]
[RequireComponent(typeof(Health))]
public class ShipStats : MonoBehaviour
{
    public static ShipStats Instance { get; private set; }

    [Header("Ficha")]
    [Tooltip("De onde vêm os números desta nave. As fichas ficam em Assets/Ships/ — " +
             "o 'Montar' cria a inicial e liga aqui.")]
    [SerializeField] ShipDefinition definition;

    /// <summary>
    /// Os valores gravados, sem nada aplicado por cima. Só interessa a quem
    /// precisa comparar o de agora com o de fábrica — para jogar, leia as
    /// propriedades deste componente.
    /// </summary>
    public ShipDefinition Definition => definition;

    static readonly int StatCount = System.Enum.GetValues(typeof(ShipStat)).Length;

    readonly List<StatModifier> modifiers = new List<StatModifier>();
    readonly float[] values = new float[StatCount];

    public float CruiseSpeed => values[(int)ShipStat.CruiseSpeed];
    public float Acceleration => values[(int)ShipStat.Acceleration];
    public float Damage => values[(int)ShipStat.Damage];
    public float AttackSpeed => values[(int)ShipStat.AttackSpeed];
    public float MaxHealth => values[(int)ShipStat.MaxHealth];
    public float DefensePercent => values[(int)ShipStat.DefensePercent];

    /// <summary>Fator do preço de uma batida: 1 é o custo cheio, 0,5 é metade.</summary>
    public float CrashCost => values[(int)ShipStat.CrashCost];

    /// <summary>Velocidade que um obstáculo de peso 1 rende ao morrer.</summary>
    public float KillSpeedGain => values[(int)ShipStat.KillSpeedGain];

    /// <summary>Segundos entre um tiro e o próximo. Sai da cadência de agora, não da gravada.</summary>
    public float ShotInterval => 1f / Mathf.Max(0.1f, AttackSpeed);

    public Health Health { get; private set; }

    /// <summary>Os poderes valendo agora. Nulo é caso normal: nave sem o componente não usa poder.</summary>
    ShipPowerUps powerUps;

    /// <summary>Falso até a vida ser configurada no Awake — ver lá.</summary>
    bool ready;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if (definition == null)
        {
            // Sem ficha a nave não tem número nenhum, e cada leitura daqui
            // estouraria. Vale mais uma nave genérica com um erro gritando no
            // Console do que a cena inteira parando na primeira linha — o erro
            // diz o que fazer, e a corrida continua rodando para ser testada.
            Debug.LogError("[Nave] Nenhuma ficha ligada no ShipStats. Rode Tools → Corrida no " +
                           "Espaço → Montar. Usando valores de fábrica por enquanto.", this);
            definition = ScriptableObject.CreateInstance<ShipDefinition>();
        }

        Health = GetComponent<Health>();
        powerUps = GetComponent<ShipPowerUps>();

        Recalculate();

        // Só agora a vida entra, e cheia. O Recalculate acima não podia encostar
        // nela: este componente acorda antes do Health (ordem de execução -55),
        // e ajustar o teto sem encher deixaria a nave com a vida em zero pelo
        // resto do quadro — o HUD chegaria a receber esse zero.
        Health.Configure(MaxHealth);
        ready = true;
    }

    void Start()
    {
        // A corrida nasce com números de partida no RaceSpeed; a ficha chega
        // depois e impõe os dela. É o ponto de troca que estava prometido lá.
        PushToRace();
    }

    // ── Modificadores ────────────────────────────────────────────────────

    /// <summary>
    /// Mexe num número da nave a partir de agora. Quem aplicou **guarda a
    /// referência devolvida** e a passa para <see cref="RemoveModifier"/> quando
    /// o efeito acabar — poder por tempo não precisa fazer isso na mão, o
    /// <see cref="ActivePowerUp.ApplyModifier"/> cuida.
    /// </summary>
    public void AddModifier(StatModifier modifier)
    {
        if (modifier == null)
            return;

        modifiers.Add(modifier);
        Recalculate();
    }

    /// <summary>Desfaz um modificador. Passar o que não está aplicado não faz nada.</summary>
    public void RemoveModifier(StatModifier modifier)
    {
        if (modifier != null && modifiers.Remove(modifier))
            Recalculate();
    }

    /// <summary>Valor de fábrica de um atributo, sem nada aplicado por cima.</summary>
    public float BaseValue(ShipStat stat)
    {
        switch (stat)
        {
            case ShipStat.CruiseSpeed: return definition.CruiseSpeed;
            case ShipStat.Acceleration: return definition.Acceleration;
            case ShipStat.Damage: return definition.Damage;
            case ShipStat.AttackSpeed: return definition.AttackSpeed;
            case ShipStat.MaxHealth: return definition.MaxHealth;
            case ShipStat.DefensePercent: return definition.DefensePercent;
            case ShipStat.KillSpeedGain: return definition.KillSpeedGain;
            case ShipStat.CrashCost: return definition.CrashCost;
            default: return 0f;
        }
    }

    /// <summary>Valor de agora, já com tudo que estiver aplicado.</summary>
    public float Value(ShipStat stat) => values[(int)stat];

    /// <summary>
    /// Refaz a conta de todos os atributos e avisa quem guardou cópia.
    ///
    /// Recalcula tudo de uma vez, e não só o atributo mexido, porque
    /// <see cref="ShipStat.KillSpeedGain"/> depende da aceleração já modificada —
    /// e porque isto roda quando um poder entra ou sai, não a cada quadro.
    /// </summary>
    void Recalculate()
    {
        for (int i = 0; i < StatCount; i++)
        {
            var stat = (ShipStat)i;
            if (stat == ShipStat.KillSpeedGain)
                continue;

            values[i] = Clamp(stat, Combine(stat, BaseValue(stat)));
        }

        // O ganho por abate fica para o fim, e fora do laço, porque é derivado: a
        // base dele é a aceleração DE AGORA vezes a fração da ficha, e não o valor
        // gravado. Assim um poder que acelera a nave já faz o abate render mais,
        // que é a promessa do atributo — e um modificador direto em KillSpeedGain
        // ainda entra por cima disso.
        //
        // Fora do laço, e não confiando na ordem do enum: bastaria alguém mover
        // uma linha lá em cima para o abate passar a ser calculado com a
        // aceleração do quadro anterior, e isso não daria erro nenhum — só um
        // número levemente errado, que é o pior tipo de bug.
        float killBase = values[(int)ShipStat.Acceleration] * definition.KillGainFactor;
        values[(int)ShipStat.KillSpeedGain] =
            Clamp(ShipStat.KillSpeedGain, Combine(ShipStat.KillSpeedGain, killBase));

        PushToRace();
        PushToHealth();
    }

    /// <summary>
    /// **Todo modificador conta em cima do valor de base, e os bônus se somam**
    /// *(regra do Raffael, 21/08/2026)*. Com aceleração 4, um <c>+2</c> e um
    /// <c>×1,5</c> valem 2 e 2 — os dois medidos contra o 4 —, e o resultado é 8.
    ///
    /// Duas consequências, e as duas são o motivo de ser assim:
    ///
    /// **A ordem nunca importa.** Não existe "quem pegou primeiro leva vantagem",
    /// então o mesmo par de poderes dá o mesmo número em qualquer sequência.
    ///
    /// **Empilhar não estoura.** Dois <c>×1,5</c> dão o dobro da base, e não
    /// 2,25 vezes. Com multiplicação encadeada, quatro poderes modestos viram um
    /// número que nenhuma fase foi equilibrada para aguentar.
    /// </summary>
    float Combine(ShipStat stat, float baseValue)
    {
        float bonus = 0f;

        for (int i = 0; i < modifiers.Count; i++)
        {
            var modifier = modifiers[i];
            if (modifier.Stat != stat)
                continue;

            bonus += modifier.Add;

            // O fator vira o quanto ele acrescenta SOBRE a base: ×1,5 é meia base
            // a mais, ×0,5 é meia base a menos. É isto que põe soma e fator na
            // mesma moeda e permite somá-los.
            bonus += baseValue * (modifier.Multiply - 1f);
        }

        return baseValue + bonus;
    }

    /// <summary>
    /// Os mesmos limites que a ficha impõe no Inspector. Sem isto, dois poderes
    /// de defesa somando 120% fariam o dano virar negativo, e um poder que
    /// zerasse a cadência travaria a arma numa divisão por quase zero.
    /// </summary>
    static float Clamp(ShipStat stat, float value)
    {
        switch (stat)
        {
            case ShipStat.Acceleration: return Mathf.Max(0.1f, value);
            case ShipStat.AttackSpeed: return Mathf.Max(0.1f, value);
            case ShipStat.MaxHealth: return Mathf.Max(1f, value);
            case ShipStat.DefensePercent: return Mathf.Clamp(value, 0f, 99f);
            default: return Mathf.Max(0f, value);
        }
    }

    /// <summary>
    /// O RaceSpeed guarda cópia do cruzeiro e da aceleração, então mudança aqui
    /// tem de ser empurrada — senão um poder de aceleração não faria nada.
    /// </summary>
    void PushToRace()
    {
        if (RaceSpeed.Instance == null)
            return;

        RaceSpeed.Instance.ApplyShipStats(CruiseSpeed, Acceleration);

        // A razão vai junto, e não a aceleração crua, porque quem alivia o trecho
        // arrastado da batida é o quanto a nave passou da PRÓPRIA base — uma nave
        // que já nasce acelerada não deve levar alívio de graça por isso.
        float baseAcceleration = Mathf.Max(0.01f, definition.Acceleration);
        RaceSpeed.Instance.ApplyCrashProfile(CrashCost, Acceleration / baseAcceleration);
    }

    /// <summary>
    /// A vida cheia também é cópia. **Sem encher a barra**: um poder que sobe a
    /// vida máxima dá espaço, não cura — quem quiser curar junto que cure no
    /// próprio efeito. E quando ele acabar, a vida atual é aparada no teto novo.
    /// </summary>
    void PushToHealth()
    {
        if (ready && Health != null && !Mathf.Approximately(Health.Max, MaxHealth))
            Health.Configure(MaxHealth, refill: false);
    }

    // ── Dano ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Um golpe chegando na nave, já resolvido: quanto de dano sobrou e o que
    /// mais foi cancelado junto.
    ///
    /// **Dano e tempo são coisas separadas neste jogo** — bater custa ~3,5 s de
    /// corrida além da vida, e o raspão custa menos de 1 s. Um poder pode querer
    /// segurar um sem segurar o outro, então quem cobra o tempo (o obstáculo e o
    /// estilhaço) pergunta aqui antes.
    /// </summary>
    public struct Hit
    {
        /// <summary>Dano que sobrou depois da defesa e dos poderes. Zero: foi segurado.</summary>
        public float Damage;

        /// <summary>O custo de tempo — a freada da batida ou o tropeço do raspão — não deve ser cobrado.</summary>
        public bool BlockTimeCost;

        /// <summary>
        /// O que o obstáculo soltaria ao morrer não deve sair — no Casulo, o
        /// estilhaço. Nasce desmarcado: **por padrão o obstáculo desfeito no
        /// escudo faz tudo o que faria ao ser destruído**, e quem quiser calar
        /// isso é um poder que diga expressamente.
        /// </summary>
        public bool BlockDeathEffects;
    }

    /// <summary>
    /// Dano que sobra depois da defesa. **Sempre arredondado para cima**: um
    /// golpe que acerta nunca é de graça, nem contra 99% de redução.
    /// </summary>
    public float DamageAfterDefense(float rawDamage)
    {
        if (rawDamage <= 0f)
            return 0f;

        float reduced = rawDamage * (1f - DefensePercent / 100f);
        return Mathf.Max(1f, Mathf.Ceil(reduced));
    }

    /// <summary>
    /// Bater na nave é por aqui, e só por aqui: é o único caminho onde a defesa
    /// desconta e onde os poderes têm chance de se meter no golpe.
    ///
    /// A ordem é **defesa e depois poderes**, e ela importa: o desconto da defesa
    /// tem piso de 1 de dano de propósito, então um poder que rodasse antes dele
    /// veria o golpe que segurou ressuscitar como 1.
    /// </summary>
    public Hit TakeHit(float rawDamage)
    {
        var hit = new Hit { Damage = DamageAfterDefense(rawDamage) };

        if (powerUps != null)
            powerUps.ModifyIncomingHit(ref hit);

        if (hit.Damage > 0f)
            Health.TakeDamage(hit.Damage);

        return hit;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
