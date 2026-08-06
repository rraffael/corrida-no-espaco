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
///   arrastado por meio minuto.
/// - **Acima do cruzeiro** manda a paciência ou o tiro. O ganho passivo é lento,
///   e destruir obstáculo é o atalho — um **bônus**, não o meio principal de
///   andar depressa.
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

    [Header("Início da fase")]
    [Tooltip("Velocidade no primeiro frame. Zero: a fase começa com a nave parada.")]
    [SerializeField, Min(0f)] float startSpeed = 0f;

    [Header("Ganho passivo acima do cruzeiro")]
    [Tooltip("Fração da aceleração da nave que vira velocidade a cada segundo ACIMA da velocidade " +
             "de cruzeiro. 0,04 com aceleração 4 dá 0,16 u/s por segundo — do cruzeiro até a dobra " +
             "leva perto de 45s. Abaixo do cruzeiro este fator não vale: lá a nave se recupera na " +
             "aceleração cheia da ficha.")]
    [SerializeField, Range(0f, 0.5f)] float passiveGainFactor = 0.04f;

    /// <summary>Velocidade agora, em unidades de mundo por segundo.</summary>
    public float Current { get; private set; }

    /// <summary>Para onde a velocidade está indo. Começa na de cruzeiro.</summary>
    public float Target { get; private set; }

    public float CruiseSpeed => cruiseSpeed;

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
        AdvanceTarget();

        // Time.deltaTime é zero com o jogo pausado, então a corrida congela
        // junto com o menu sem precisar de nenhuma checagem aqui.
        float next = Mathf.MoveTowards(Current, Target, acceleration * Time.deltaTime);

        if (Mathf.Approximately(next, Current))
            return;

        Current = next;
        Changed?.Invoke(Current);
    }

    /// <summary>
    /// Para onde a corrida caminha sozinha, sem destruir nada. São **dois ritmos
    /// muito diferentes**, e a diferença entre eles é a regra do jogo (decidida
    /// pelo Raffael em 06/08/2026):
    ///
    /// - **Abaixo da velocidade de cruzeiro**, a nave se recupera na **aceleração
    ///   cheia da ficha**. Voltar ao cruzeiro é atributo da nave, não prêmio por
    ///   atirar: uma sequência de batidas custa alguns segundos, não a corrida.
    ///   Com aceleração 4, sair de 1 e chegar aos 8 do cruzeiro leva menos de 2s.
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
        if (Target < cruiseSpeed)
        {
            Target = Mathf.Min(cruiseSpeed, Target + acceleration * Time.deltaTime);
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
    /// A punição da batida pesa diferente conforme onde ela acontece, e é assim
    /// que tem de ser: **acima do cruzeiro** ela custa caro, porque recuperar
    /// aquilo é no ritmo lento e é progresso perdido rumo à dobra; **abaixo do
    /// cruzeiro** ela custa poucos segundos, porque a nave volta na aceleração
    /// dela. Bater dói, mas não mata a corrida.
    /// </summary>
    public void Nudge(float delta) => SetTarget(Target + delta);

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
