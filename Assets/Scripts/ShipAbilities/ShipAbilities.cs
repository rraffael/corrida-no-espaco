using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// O **poder da nave**: o que está equipado, quantos usos sobram, quanto falta
/// da recarga, e os efeitos que ele ligou e ainda valem.
///
/// **É um sistema à parte do <see cref="LevelModifiers"/>, e não conversa com
/// ele.** Os dois vivem na mesma nave e ambos podem se meter num golpe, mas
/// nenhum sabe da existência do outro — quem os chama é a nave.
///
/// **Um slot só, por enquanto.** De onde vem o que está nele: o campo da cena,
/// se preenchido; senão, o poder que a ficha da nave traz de fábrica. Quando
/// existir tela de equipar, é ela que passa a mandar aqui.
/// </summary>
[DefaultExecutionOrder(-53)]
public class ShipAbilities : MonoBehaviour
{
    public static ShipAbilities Instance { get; private set; }

    [Header("Equipado")]
    [Tooltip("Poder equipado para esta partida. Vazio: usa o que a ficha da nave trouxer de " +
             "fábrica. Quando existir tela de equipar, é ela que preenche isto.")]
    [SerializeField] ShipAbility equipped;

    /// <summary>O poder que o jogador pode ativar. Nulo: esta nave não tem nenhum.</summary>
    public ShipAbility Equipped => equipped;

    /// <summary>
    /// Ativações que ainda sobram nesta partida. Sem significado quando o poder
    /// é de usos ilimitados — ver <see cref="HasUses"/>.
    /// </summary>
    public int UsesLeft { get; private set; }

    /// <summary>O poder tem limite de ativações? Falso: só a recarga o segura.</summary>
    public bool IsLimited => equipped != null && !equipped.HasUnlimitedUses;

    /// <summary>Ainda dá para ativar, do ponto de vista de usos.</summary>
    public bool HasUses => equipped != null && (equipped.HasUnlimitedUses || UsesLeft > 0);

    /// <summary>Segundos que faltam para poder ativar de novo. Zero: pronto.</summary>
    public float CooldownLeft => Mathf.Max(0f, readyAt - RaceClock.Now);

    /// <summary>De 0 (acabou de usar) a 1 (pronto). Para o canto da tela desenhar a espera.</summary>
    public float CooldownFraction =>
        equipped == null || equipped.cooldownSeconds <= 0f
            ? 1f
            : Mathf.Clamp01(1f - CooldownLeft / equipped.cooldownSeconds);

    /// <summary>Dá para ativar agora?</summary>
    public bool IsReady => HasUses && CooldownLeft <= 0f;

    /// <summary>Os atributos da nave, para os poderes que mexem neles.</summary>
    public ShipStats Stats { get; private set; }

    readonly List<ActiveShipAbility> active = new List<ActiveShipAbility>();

    /// <summary>Os efeitos ligados por este poder e que ainda valem.</summary>
    public IReadOnlyList<ActiveShipAbility> Active => active;

    float readyAt;
    ShipLaneController lanes;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        Stats = GetComponent<ShipStats>();

        // O poder é da nave escolhida, e a ficha manda. O campo da cena continua
        // valendo como atalho de teste — preenchido, ele ganha —, mas em jogo de
        // verdade quem decide é quem o jogador escolheu no menu.
        //
        // A versão do poder é a do patamar da nave: com o 1º upgrade no nível 15,
        // é a ficha do upgrade que entra — ver ShipEvolution.Ability.
        var evolved = Stats != null ? ShipEvolution.Ability(Stats.Definition, Stats.Level) : null;
        if (evolved != null)
            equipped = evolved;

        ResetForRace();
    }

    /// <summary>
    /// A passiva da nave escolhida, nesta corrida. Nula: ela não tem, ou ainda não
    /// chegou ao patamar. Guardada aqui e não relida da ficha a cada uso: se o
    /// jogador trocar de nave entre corridas, a que tem de ser desligada no fim é
    /// a que foi ligada no começo.
    /// </summary>
    ActiveShipPassive passive;

    /// <summary>A passiva nesta corrida, para o HUD. Nula: não há.</summary>
    public ActiveShipPassive Passive => passive;

    // ── O que o simulador lê ─────────────────────────────────────────────
    // A corrida acaba com o ResetForRace, e o simulador só anota depois. Por isso
    // estes números não zeram nele; a cena recarrega a cada corrida e eles nascem
    // zerados de novo.

    /// <summary>Quantas vezes o poder ativo disparou nesta corrida, à mão ou sozinho.</summary>
    public int ActivationCount { get; private set; }

    /// <summary>Destas ativações, quantas pegaram o efeito ainda ligado e o renovaram.</summary>
    public int RenewalCount { get; private set; }

    /// <summary>Quantas vezes o poder foi recarregado por modificador de fase.</summary>
    public int RechargeCount { get; private set; }

    /// <summary>Recargas pegas com o poder pronto e cheio: não mudaram nada.</summary>
    public int WastedRechargeCount { get; private set; }

    /// <summary>Segundos de corrida com o efeito do poder ativo ligado.</summary>
    public float EffectSeconds { get; private set; }

    /// <summary>O poder estava pronto quando a corrida acabou, antes de tudo voltar ao começo?</summary>
    public bool WasReadyAtEnd { get; private set; }

    /// <summary>As cargas do poder condicional quando a corrida acabou.</summary>
    public int ConditionalCountAtEnd { get; private set; }

    /// <summary>O máximo que o contador da passiva alcançou nesta corrida.</summary>
    public int PassivePeakCount { get; private set; }

    /// <summary>
    /// O poder condicional da nave escolhida, nesta corrida. Nulo: ela não tem.
    /// Mora aqui, e não num componente próprio, porque os avisos de que ele
    /// precisa — abate, por enquanto — já chegam a este.
    /// </summary>
    ActiveShipConditional conditional;

    /// <summary>O poder condicional nesta corrida, para o HUD e o visual da nave. Nulo: não tem.</summary>
    public ActiveShipConditional Conditional => conditional;

    void OnEnable()
    {
        lanes = GetComponent<ShipLaneController>();
        if (lanes != null)
            lanes.LaneChanged += OnLaneChanged;
    }

    void OnDisable()
    {
        if (lanes != null)
            lanes.LaneChanged -= OnLaneChanged;
    }

    void Start()
    {
        // Em Start e não em OnEnable: o TouchInput é outro objeto, e só depois de
        // todos os Awake é que a instância dele está garantida.
        if (TouchInput.Instance != null)
            TouchInput.Instance.DoubleTapped += OnDoubleTapped;

        // A passiva entra aqui, e não no Awake, pelo mesmo motivo: ela pode mexer
        // em atributo, e o ShipStats precisa ter terminado de montar os números
        // de base antes de alguém escrever por cima deles.
        // Os dois pelo patamar: a passiva só existe no final, e o condicional vem
        // na versão do upgrade que a nave já alcançou.
        var passiveDefinition = Stats != null ? ShipEvolution.Passive(Stats.Definition, Stats.Level) : null;
        if (passiveDefinition != null)
        {
            passive = new ActiveShipPassive(passiveDefinition, this);
            passiveDefinition.OnRaceStarted(passive);
        }

        var conditionalDefinition = Stats != null
            ? ShipEvolution.Conditional(Stats.Definition, Stats.Level)
            : null;
        if (conditionalDefinition != null)
        {
            conditional = new ActiveShipConditional(conditionalDefinition, Stats);
            conditionalDefinition.OnRaceStarted(conditional);
        }
    }

    // ── Ativação ─────────────────────────────────────────────────────────

    /// <summary>
    /// O gesto que ativa o poder da nave. **Toque em cima de UI não conta:**
    /// apertar o botão de pausa duas vezes depressa não pode gastar um uso.
    /// </summary>
    void OnDoubleTapped(Vector2 screenPosition)
    {
        if (TouchInput.Instance != null && TouchInput.Instance.IsOverUI())
            return;

        Activate();
    }

    /// <summary>
    /// Liga o poder equipado, se houver, se sobrar uso e se a recarga tiver
    /// passado. Devolve se ativou — para quem quiser dar um retorno na tela do
    /// "ainda não dá".
    ///
    /// **Gasta o uso e começa a recarga na ativação**, e não no fim do efeito:
    /// assim o jogador sabe quando pode contar com ele de novo sem ter de
    /// acompanhar quanto o efeito anterior ainda dura.
    ///
    /// **Com o efeito ainda ligado, ativar de novo volta a duração ao máximo**
    /// *(regra do Raffael, 01/11/2026)*: na prática, é ter usado o poder de novo.
    /// Não cria um segundo efeito por cima, senão a Intangibilidade cobraria a
    /// velocidade duas vezes no fim.
    /// </summary>
    public bool Activate()
    {
        if (!IsReady)
            return false;

        if (IsLimited)
            UsesLeft--;

        readyAt = RaceClock.Now + equipped.cooldownSeconds;
        ActivationCount++;

        for (int i = 0; i < active.Count; i++)
        {
            if (active[i].Definition != equipped || active[i].Finished)
                continue;

            active[i].Renew();
            RenewalCount++;
            return true;
        }

        var runtime = equipped.CreateRuntime();
        runtime.Bind(equipped, this);

        equipped.OnActivated(runtime);

        // Instantâneo já fez o que tinha de fazer. Guardá-lo na lista só criaria
        // um item que nunca sai dela.
        if (equipped.lifetime == ShipAbility.Lifetime.Instantaneo || runtime.Finished)
        {
            equipped.OnEnded(runtime);
            runtime.RemoveAppliedModifiers();
            return true;
        }

        active.Add(runtime);
        return true;
    }

    /// <summary>
    /// Recarrega o poder equipado: zera a espera e, se ele for de usos limitados
    /// e já tiver gasto algum, devolve **um** — nunca acima do que a corrida dá.
    /// Devolve se mudou alguma coisa; poder pronto e cheio não tem o que ganhar.
    ///
    /// Não liga o poder: só o deixa pronto. Quando gastar continua sendo decisão
    /// do jogador.
    /// </summary>
    public bool Recharge()
    {
        if (equipped == null)
            return false;

        bool changed = false;

        if (CooldownLeft > 0f)
        {
            readyAt = 0f;
            changed = true;
        }

        if (IsLimited && UsesLeft < equipped.usesPerRace)
        {
            UsesLeft++;
            changed = true;
        }

        if (changed)
            RechargeCount++;
        else
            WastedRechargeCount++;

        return changed;
    }

    /// <summary>Troca o que está equipado. Quem vai chamar é a tela de equipar.</summary>
    public void Equip(ShipAbility ability)
    {
        equipped = ability;
        ResetForRace();
    }

    void Update()
    {
        // Antes do retorno de baixo: a passiva corre mesmo sem efeito ativo ligado,
        // e a Predadora nem tem poder ativo.
        if (passive != null)
            passive.Definition.OnTick(passive, Time.deltaTime);

        if (active.Count == 0)
            return;

        EffectSeconds += Time.deltaTime;

        for (int i = active.Count - 1; i >= 0; i--)
        {
            var ability = active[i];
            var definition = ability.Definition;

            if (!ability.Finished)
            {
                definition.OnTick(ability, Time.deltaTime);

                if (definition.lifetime == ShipAbility.Lifetime.PorTempo)
                {
                    ability.SecondsLeft -= Time.deltaTime;
                    if (ability.SecondsLeft <= 0f)
                        ability.Finish();
                }
            }

            if (ability.Finished)
            {
                active.RemoveAt(i);
                definition.OnEnded(ability);
                ability.RemoveAppliedModifiers();
            }
        }
    }

    // ── Ganchos ──────────────────────────────────────────────────────────

    /// <summary>
    /// Passa um golpe pelos efeitos ligados. Chamado pelo
    /// <see cref="ShipStats.TakeHit"/>.
    /// </summary>
    internal void ModifyIncomingHit(ref ShipStats.Hit hit)
    {
        for (int i = 0; i < active.Count; i++)
        {
            var ability = active[i];
            if (!ability.Finished)
                ability.Definition.ModifyIncomingHit(ability, ref hit);
        }
    }

    /// <summary>Chamado pelo obstáculo quando ele morre de tiro — não na batida.</summary>
    public void NotifyObstacleDestroyed(ObstacleStats obstacle)
    {
        for (int i = 0; i < active.Count; i++)
        {
            var ability = active[i];
            if (!ability.Finished)
                ability.Definition.OnObstacleDestroyed(ability, obstacle);
        }

        if (conditional != null)
            conditional.Definition.OnObstacleDestroyed(conditional, obstacle);

        if (passive != null)
        {
            passive.Definition.OnObstacleDestroyed(passive, obstacle);
            PassivePeakCount = Mathf.Max(PassivePeakCount, passive.Count);
        }
    }

    /// <summary>Chamado pelo diretor da corrida quando a nave chega à velocidade de dobra.</summary>
    public void NotifyWarpEntered()
    {
        if (passive != null)
            passive.Definition.OnWarpEntered(passive);
    }

    /// <summary>Chamado pelo obstáculo quando ele passa pela nave e sai de cena.</summary>
    public void NotifyObstaclePassed(ObstacleStats obstacle)
    {
        for (int i = 0; i < active.Count; i++)
        {
            var ability = active[i];
            if (!ability.Finished)
                ability.Definition.OnObstaclePassed(ability, obstacle);
        }
    }

    /// <summary>Chamado pela arma a cada tiro que sai.</summary>
    public void NotifyShotFired()
    {
        for (int i = 0; i < active.Count; i++)
        {
            var ability = active[i];
            if (!ability.Finished)
                ability.Definition.OnShotFired(ability);
        }
    }

    void OnLaneChanged(int lane)
    {
        for (int i = 0; i < active.Count; i++)
        {
            var ability = active[i];
            if (!ability.Finished)
                ability.Definition.OnLaneChanged(ability, lane);
        }
    }

    /// <summary>
    /// Derruba os efeitos ligados e devolve usos e recarga. Serve para o fim da
    /// corrida: usos são da partida, não do jogador.
    /// </summary>
    public void ResetForRace()
    {
        WasReadyAtEnd = IsReady;
        ConditionalCountAtEnd = conditional != null ? conditional.Count : 0;

        for (int i = active.Count - 1; i >= 0; i--)
        {
            var ability = active[i];
            active.RemoveAt(i);
            ability.Finish();
            ability.Definition.OnEnded(ability);
            ability.RemoveAppliedModifiers();
        }

        UsesLeft = equipped != null ? equipped.usesPerRace : 0;
        readyAt = 0f;

        if (conditional != null)
        {
            conditional.Definition.OnRaceEnded(conditional);
            conditional.RemoveModifiers();
            conditional = null;
        }

        if (passive == null)
            return;

        passive.Definition.OnRaceEnded(passive);
        passive.RemoveModifiers();
        passive = null;
    }

    void OnDestroy()
    {
        if (TouchInput.Instance != null)
            TouchInput.Instance.DoubleTapped -= OnDoubleTapped;

        if (Instance == this)
            Instance = null;
    }
}
