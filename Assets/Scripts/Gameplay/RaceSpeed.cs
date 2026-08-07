using System;
using UnityEngine;

/// <summary>
/// Velocidade da corrida, em unidades de mundo por segundo. É a fonte única:
/// cenário, HUD, obstáculos e tiro perguntam aqui em vez de cada um carregar a
/// sua própria constante — o mesmo papel que o <see cref="LaneTrack"/> faz para
/// as faixas.
///
/// A fase começa com a nave parada e acelerando até a velocidade de cruzeiro.
/// Daí em diante a velocidade vive em dois regimes, e é bom não confundi-los:
///
/// - **Até o cruzeiro** manda a nave. A aceleração da ficha é o caminho
///   principal para essa velocidade, e é rápida: bater não deixa o jogador
///   arrastado por meio minuto. Mais que isso, ela é **mais forte quanto mais
///   devagar a nave estiver** (ver <see cref="AccelerationNow"/>) — o arranque
///   é bravo e vai perdendo força ao se aproximar do cruzeiro.
/// - **Acima do cruzeiro** manda a paciência ou o tiro. O ganho passivo é lento,
///   e destruir obstáculo é o atalho — um **bônus**, não o meio principal de
///   andar depressa.
///
/// **Bater é caso à parte** — ver <see cref="Crash"/>. A velocidade despenca na
/// hora, a nave fica um instante sem reagir e a volta tem dois trechos, o último
/// arrastado. A batida custa **tempo**, e não velocidade: é um preço que o
/// jogador vê acontecendo, em vez de ler num número que baixou duas unidades.
/// </summary>
[DefaultExecutionOrder(-60)]
public class RaceSpeed : MonoBehaviour
{
    public static RaceSpeed Instance { get; private set; }

    [Header("Velocidade de cruzeiro")]
    [Tooltip("Valor de partida. O ShipStats da nave sobrescreve isto no início da fase — " +
             "a ficha da nave é quem manda. Este número só vale se não houver nave na cena.")]
    [SerializeField, Min(0f)] float cruiseSpeed = 8f;

    [Header("Regras da corrida")]
    [Tooltip("Valor de partida da aceleração. Também vem da ficha da nave — este só vale " +
             "enquanto não houver nave na cena.")]
    [SerializeField, Min(0.1f)] float acceleration = 4f;

    [Tooltip("Quanto a aceleração ganha a mais com a nave parada, em multiplicador. 2 = +200%, " +
             "ou seja, 300% da aceleração da ficha no zero. O bônus derrete de forma proporcional " +
             "conforme a nave sobe e chega a zero na velocidade de cruzeiro — de lá para cima a " +
             "aceleração é a da ficha, limpa. Zero desliga o bônus.")]
    [SerializeField, Range(0f, 4f)] float lowSpeedAccelerationBonus = 2f;

    [Header("Batida")]
    [Tooltip("Que fatia da velocidade SOBRA na hora da batida. 0,25: a nave despenca para um " +
             "quarto do que estava. A queda é instantânea, e é ela que o jogador sente. " +
             "É fração e não número fixo para a nave nunca parecer PARAR de vez — o tranco pesa " +
             "igual em qualquer velocidade, e a corrida continua andando.")]
    [SerializeField, Range(0.05f, 1f)] float crashSpeedFraction = 0.25f;

    [Tooltip("Pausa depois da batida, em segundos, antes de a nave voltar a acelerar. É o instante " +
             "de nave morta: sem isto a recuperação começa no mesmo frame do impacto. " +
             "Não conta com o jogo pausado.")]
    [SerializeField, Range(0f, 2f)] float recoveryDelaySeconds = 0.5f;

    [Tooltip("Que fatia da retomada é rápida. 0,8: a nave recupera os primeiros 80% da velocidade " +
             "na aceleração normal e o resto se arrasta.")]
    [SerializeField, Range(0.1f, 1f)] float fastRecoveryFraction = 0.8f;

    [Tooltip("Quantos segundos leva o ÚLTIMO trecho da retomada — os 20% finais. É o que faz a " +
             "batida doer depois de já parecer resolvida: a nave chega perto do que era e agoniza " +
             "para fechar a conta.")]
    [SerializeField, Range(0f, 6f)] float finalStretchSeconds = 2f;

    [Header("Raspão (estilhaço)")]
    [Tooltip("Fatia da velocidade que um estilhaço tira na hora. 0,15: a nave perde 15% e já " +
             "começa a retomar. É uma fração e não um número fixo para o tranco parecer o mesmo " +
             "em qualquer velocidade.")]
    [SerializeField, Range(0f, 0.5f)] float grazeSpeedLoss = 0.15f;

    [Tooltip("Pausa depois de um raspão, em segundos. Bem menor que a da batida — é um tropeço, " +
             "não uma parada.")]
    [SerializeField, Range(0f, 1f)] float grazeDelaySeconds = 0.15f;

    [Header("Início da fase")]
    [Tooltip("Velocidade no primeiro frame. Zero: a fase começa com a nave parada.")]
    [SerializeField, Min(0f)] float startSpeed = 0f;

    [Header("Ganho passivo acima do cruzeiro")]
    [Tooltip("Fração da aceleração da nave que vira velocidade a cada segundo ACIMA da velocidade " +
             "de cruzeiro. 0,04 com aceleração 4 dá 0,16 u/s por segundo — do cruzeiro até a dobra " +
             "leva perto de 45s. Abaixo do cruzeiro este fator não vale: lá a nave se recupera na " +
             "aceleração da ficha com o bônus de baixa velocidade por cima.")]
    [SerializeField, Range(0f, 0.5f)] float passiveGainFactor = 0.04f;

    /// <summary>Velocidade agora, em unidades de mundo por segundo.</summary>
    public float Current { get; private set; }

    /// <summary>Para onde a velocidade está indo. Começa na de cruzeiro.</summary>
    public float Target { get; private set; }

    public float CruiseSpeed => cruiseSpeed;

    /// <summary>
    /// A partir de que instante a nave volta a acelerar. Uma batida empurra isto
    /// para a frente; <see cref="Time.time"/> e não <c>unscaledTime</c>, para a
    /// pausa do jogo não gastar o tempo do baque.
    /// </summary>
    float recoveryResumesAt;

    /// <summary>Se a nave está subindo de volta de uma batida.</summary>
    bool inCrashRecovery;

    /// <summary>
    /// Velocidade que a retomada persegue: o que a nave tinha antes de bater,
    /// menos a punição da ficha do obstáculo. É contra este número que se mede o
    /// trecho rápido e o arrastado — e não contra o <see cref="Target"/>, que
    /// pode subir no meio da retomada por abate ou ganho passivo.
    /// </summary>
    float crashRecoveryTarget;

    /// <summary>
    /// Aceleração valendo **neste frame**, já com o bônus de baixa velocidade.
    ///
    /// A regra, pedida pelo Raffael em 07/08/2026: quanto mais longe do cruzeiro
    /// para baixo, mais forte a nave puxa. Parada, ela acelera
    /// <c>1 + lowSpeedAccelerationBonus</c> vezes o que diz a ficha (300% no
    /// ajuste atual); no cruzeiro, exatamente o que diz a ficha; entre os dois, a
    /// proporção do caminho que falta andar.
    ///
    /// Acima do cruzeiro **não há bônus** — lá quem manda é o ganho passivo, e
    /// dobrar a aceleração ali só faria a nave alcançar mais depressa um alvo que
    /// já sobe devagar de propósito.
    /// </summary>
    public float AccelerationNow
    {
        get
        {
            if (lowSpeedAccelerationBonus <= 0f || cruiseSpeed <= 0f || Current >= cruiseSpeed)
                return acceleration;

            // 1 com a nave parada, 0 no cruzeiro.
            float belowCruise = 1f - Current / cruiseSpeed;
            return acceleration * (1f + lowSpeedAccelerationBonus * belowCruise);
        }
    }

    /// <summary>
    /// Se a nave ainda está no baque de uma freada. Enquanto for verdade ela não
    /// acelera nem ganha velocidade passiva — só desacelera, se o alvo estiver
    /// abaixo dela.
    /// </summary>
    public bool InCrashRecoil => Time.time < recoveryResumesAt;

    /// <summary>
    /// Até onde o **ganho passivo** empurra sozinho. O RaceDirector põe a dobra
    /// aqui, e infinito na fase sem fim. Não é teto de velocidade: destruir
    /// obstáculo passa por cima disto à vontade.
    /// </summary>
    public float PassiveCeiling { get; private set; }

    /// <summary>Disparado quando a velocidade muda, com o valor novo.</summary>
    public event Action<float> Changed;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        Current = Mathf.Max(0f, startSpeed);
        Target = cruiseSpeed;

        // Sem RaceDirector na cena — testando a Game.unity solta — o ganho
        // passivo não para, que é o comportamento da fase sem fim.
        PassiveCeiling = float.PositiveInfinity;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        // A retomada acaba quando a nave chega no que perseguia — ou quando o
        // alvo, mais baixo, a alcança primeiro. Aqui em cima porque o resto do
        // método tem saídas antecipadas, e ficar preso em "retomando" para
        // sempre travaria a nave no ritmo arrastado.
        if (inCrashRecovery && (Current >= crashRecoveryTarget || Current >= Target))
            inCrashRecovery = false;

        bool recoiling = InCrashRecoil;

        if (!recoiling)
            AdvanceTarget();

        // Time.deltaTime é zero com o jogo pausado, então a corrida congela
        // junto com o menu sem precisar de nenhuma checagem aqui.
        float step = acceleration * Time.deltaTime;

        if (Target > Current)
        {
            // Subir é que fica travado no baque; a queda da batida é instantânea
            // e já aconteceu no Crash. Segurar a subida é o que faz o impacto
            // durar mais que um frame.
            if (recoiling)
                return;

            step = ClimbRate() * Time.deltaTime;
        }

        float next = Mathf.MoveTowards(Current, Target, step);

        if (Mathf.Approximately(next, Current))
            return;

        Current = next;
        Changed?.Invoke(Current);
    }

    /// <summary>
    /// A que ritmo a velocidade sobe neste frame.
    ///
    /// Fora de uma batida é a aceleração da nave, com o bônus de baixa velocidade
    /// (<see cref="AccelerationNow"/>). Voltando de uma batida são **dois
    /// trechos**, e é essa quebra que dá o peso do impacto (desenho do Raffael,
    /// 07/08/2026):
    ///
    /// - **Os primeiros 80%** voltam na aceleração normal. Como a nave despencou
    ///   para um quarto do que tinha, boa parte desse trecho corre abaixo do
    ///   cruzeiro, com o bônus de baixa velocidade ajudando: a nave "recarrega"
    ///   e o jogador vê que está voltando.
    /// - **Os 20% finais** se arrastam por <see cref="finalStretchSeconds"/>. É
    ///   aqui que a batida cobra de verdade — a nave parece recuperada, mas
    ///   custa a fechar a conta.
    /// </summary>
    float ClimbRate()
    {
        if (!inCrashRecovery)
            return AccelerationNow;

        float fastUntil = crashRecoveryTarget * fastRecoveryFraction;

        if (Current < fastUntil)
            return AccelerationNow;

        if (finalStretchSeconds <= 0f)
            return AccelerationNow;

        // O trecho final dividido pelo tempo que ele deve levar: quanto mais
        // rápida a nave estava, mais velocidade tem de recuperar no mesmo prazo.
        return (crashRecoveryTarget - fastUntil) / finalStretchSeconds;
    }

    /// <summary>
    /// Para onde a corrida caminha sozinha, sem destruir nada. São **dois ritmos
    /// muito diferentes**, e a diferença entre eles é a regra do jogo (decidida
    /// pelo Raffael em 06/08/2026):
    ///
    /// - **Abaixo da velocidade de cruzeiro**, a nave se recupera na aceleração
    ///   da ficha **turbinada pelo bônus de baixa velocidade**
    ///   (<see cref="AccelerationNow"/>). Voltar ao cruzeiro é atributo da nave,
    ///   não prêmio por atirar: uma sequência de batidas custa alguns segundos,
    ///   não a corrida. Com aceleração 4 e bônus 2, sair de 1 e chegar aos 8 do
    ///   cruzeiro leva perto de 1s — mais a pausa e o trecho arrastado da batida.
    /// - **Acima do cruzeiro**, o ganho é uma fração pequena disso
    ///   (<see cref="passiveGainFactor"/>). É o que leva até a dobra para quem
    ///   prefere desviar, e é aí que destruir obstáculo vira um bônus que vale a
    ///   pena — **um extra, e não o caminho principal para a velocidade**.
    ///
    /// O teto do ritmo lento é a **própria dobra**: acima do cruzeiro o ganho
    /// leva até lá e para. Na fase sem fim não há teto.
    /// </summary>
    void AdvanceTarget()
    {
        // Recuperação até o cruzeiro: rápida, e independente do que a fase ou os
        // obstáculos tenham feito com a velocidade.
        //
        // Aqui vai a aceleração **com o bônus** de propósito. Abaixo do cruzeiro
        // o alvo e a velocidade sobem colados, então é o alvo quem dita o ritmo:
        // deixar este passo na aceleração limpa esconderia o bônus justamente no
        // caso que ele existe para resolver.
        if (Target < cruiseSpeed)
        {
            Target = Mathf.Min(cruiseSpeed, Target + AccelerationNow * Time.deltaTime);
            return;
        }

        if (passiveGainFactor <= 0f || Target >= PassiveCeiling)
            return;

        float gain = acceleration * passiveGainFactor * Time.deltaTime;
        Target = Mathf.Min(PassiveCeiling, Target + gain);
    }

    /// <summary>
    /// Muda para onde a velocidade caminha. A nave chega lá na aceleração
    /// normal, não de um frame para o outro. **Não há teto** — o único piso é o
    /// zero, para uma sequência de batidas não empurrar a corrida para trás.
    /// </summary>
    public void SetTarget(float value) => Target = Mathf.Max(0f, value);

    /// <summary>
    /// Até onde o ganho passivo empurra. Quem manda é a fase, pelo RaceDirector:
    /// nas fases normais é a velocidade de dobra, na sem fim é infinito.
    /// </summary>
    public void SetPassiveCeiling(float value) => PassiveCeiling = Mathf.Max(0f, value);

    /// <summary>
    /// Empurrão para cima ou para baixo: obstáculo destruído acelera a corrida,
    /// batida freia. Mexe no <see cref="Target"/> e não na velocidade atual, de
    /// propósito — o ganho fica, e a nave chega nele acelerando, sem salto.
    ///
    /// Bater é pelo <see cref="Crash"/>, que é outra coisa: lá a velocidade cai
    /// na hora. Aqui é só o empurrão do abate, que a nave vai buscar acelerando.
    /// </summary>
    public void Nudge(float delta) => SetTarget(Target + delta);

    /// <summary>
    /// A nave bateu. **Não é um empurrão para baixo** — é um tranco, e o
    /// desenho é do Raffael (07/08/2026):
    ///
    /// 1. A velocidade **despenca na hora** para <see cref="crashSpeedFraction"/>
    ///    do que era — um quarto. Nada de desacelerar bonitinho: é um tranco.
    ///    Fração, e não um valor fixo, para a nave nunca parecer *parar de vez*:
    ///    a corrida segue andando, só que humilhada.
    /// 2. Fica <see cref="recoveryDelaySeconds"/> sem reagir.
    /// 3. Volta correndo até 80% do que tinha, e **se arrasta nos 20% finais**
    ///    por <see cref="finalStretchSeconds"/>. Ver <see cref="ClimbRate"/>.
    ///
    /// O que mudou de fundo: a batida passou a custar **tempo**, e não
    /// velocidade. É um custo que o jogador enxerga acontecendo, em vez de ler
    /// num número que baixou 2 unidades. O <paramref name="speedPenalty"/> da
    /// ficha do obstáculo continua valendo por cima, como custo permanente —
    /// zerá-lo nas fichas deixa a batida custando só os segundos.
    /// </summary>
    public void Crash(float speedPenalty)
    {
        float penalty = Mathf.Max(0f, speedPenalty);

        // O que o jogador via antes de bater é a referência da retomada — e não
        // o Target, que pode estar bem acima por causa de abates recentes.
        float before = Current;

        SetTarget(Target - penalty);

        // O piso é o cruzeiro: abaixo dele a nave voltaria para lá de qualquer
        // jeito, e mirar mais baixo criaria um degrau lento no meio da subida.
        crashRecoveryTarget = Mathf.Max(cruiseSpeed, before - penalty);
        inCrashRecovery = true;
        recoveryResumesAt = Time.time + recoveryDelaySeconds;

        float dropped = before * crashSpeedFraction;

        if (dropped >= Current)
            return;

        Current = dropped;
        Changed?.Invoke(Current);
    }

    /// <summary>
    /// Raspão: a nave levou um estilhaço. **É um tropeço, não uma batida** — a
    /// diferença entre os dois é o ponto todo desta função (pedido do Raffael em
    /// 07/08/2026).
    ///
    /// A nave perde uma fatia da velocidade e um instante de reação, e retoma no
    /// ritmo normal — **sem o trecho arrastado do <see cref="Crash"/>**, que é o
    /// que faz a batida custar quatro segundos. Aqui o custo é um pisão no freio:
    /// menos de um segundo.
    ///
    /// **Não mexe no <see cref="Target"/> de propósito.** O raspão cobra só o
    /// tempo perdido; a velocidade que a nave tinha conquistado continua lá
    /// esperando. Um Casulo solta vários estilhaços, e cada um levar velocidade
    /// embora somaria uma punição de batida em prestações.
    /// </summary>
    public void Graze()
    {
        if (grazeDelaySeconds > 0f)
            recoveryResumesAt = Mathf.Max(recoveryResumesAt, Time.time + grazeDelaySeconds);

        float loss = Current * grazeSpeedLoss;

        if (loss <= 0f)
            return;

        Current = Mathf.Max(0f, Current - loss);
        Changed?.Invoke(Current);
    }

    /// <summary>
    /// Ponto único onde a ficha da nave impõe os números dela. Chamado pelo
    /// <see cref="ShipStats"/> no início da fase; os valores do Inspector aqui
    /// são só o que vale enquanto não há nave na cena.
    ///
    /// Não mexe no <see cref="Target"/> de propósito: o <see cref="AdvanceTarget"/>
    /// leva sozinho até o cruzeiro novo, na aceleração da ficha. Alvo já acima do
    /// cruzeiro fica onde está — o que os obstáculos deram é do jogador.
    /// </summary>
    public void ApplyShipStats(float cruise, float accelerationPerSecond)
    {
        cruiseSpeed = Mathf.Max(0f, cruise);
        acceleration = Mathf.Max(0.1f, accelerationPerSecond);
    }
}
