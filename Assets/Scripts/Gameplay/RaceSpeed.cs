using System;
using UnityEngine;

/// <summary>
/// Velocidade da corrida, em unidades de mundo por segundo. É a fonte única:
/// cenário, HUD e, mais tarde, obstáculos e tiro perguntam aqui em vez de cada
/// um carregar a sua própria constante — o mesmo papel que o
/// <see cref="LaneTrack"/> faz para as faixas.
///
/// A fase começa com a nave parada e acelerando até a velocidade de cruzeiro.
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

    [Tooltip("Teto de velocidade. Nada consegue empurrar a nave além disto.")]
    [SerializeField, Min(0.1f)] float maxSpeed = 18f;

    [Header("Início da fase")]
    [Tooltip("Velocidade no primeiro frame. Zero: a fase começa com a nave parada.")]
    [SerializeField, Min(0f)] float startSpeed = 0f;

    /// <summary>Velocidade agora, em unidades de mundo por segundo.</summary>
    public float Current { get; private set; }

    /// <summary>Para onde a velocidade está indo. Começa na de cruzeiro.</summary>
    public float Target { get; private set; }

    public float CruiseSpeed => cruiseSpeed;
    public float MaxSpeed => maxSpeed;

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

        Current = Mathf.Clamp(startSpeed, 0f, maxSpeed);
        Target = cruiseSpeed;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        // Time.deltaTime é zero com o jogo pausado, então a corrida congela
        // junto com o menu sem precisar de nenhuma checagem aqui.
        float next = Mathf.MoveTowards(Current, Target, acceleration * Time.deltaTime);
        next = Mathf.Clamp(next, 0f, maxSpeed);

        if (Mathf.Approximately(next, Current))
            return;

        Current = next;
        Changed?.Invoke(Current);
    }

    /// <summary>
    /// Muda para onde a velocidade caminha. A nave chega lá na aceleração
    /// normal, não de um frame para o outro.
    /// </summary>
    public void SetTarget(float value) => Target = Mathf.Clamp(value, 0f, maxSpeed);

    /// <summary>Volta a acelerar rumo à velocidade de cruzeiro.</summary>
    public void ResumeCruise() => Target = cruiseSpeed;

    /// <summary>
    /// Empurrão para cima ou para baixo: obstáculo destruído acelera a corrida,
    /// batida freia. Mexe no <see cref="Target"/> e não na velocidade atual, de
    /// propósito — o ganho fica, e a nave chega nele acelerando, sem salto.
    /// </summary>
    public void Nudge(float delta) => SetTarget(Target + delta);

    /// <summary>
    /// Ponto único onde a ficha da nave impõe os números dela. Chamado pelo
    /// <see cref="ShipStats"/> no início da fase; os valores do Inspector aqui
    /// são só o que vale enquanto não há nave na cena.
    /// </summary>
    public void ApplyShipStats(float cruise, float accelerationPerSecond)
    {
        cruiseSpeed = Mathf.Max(0f, cruise);
        acceleration = Mathf.Max(0.1f, accelerationPerSecond);

        // Só puxa o alvo para a nova velocidade de cruzeiro se ninguém tiver
        // mexido nele ainda: no meio da corrida, o ganho dos obstáculos vale mais.
        if (Mathf.Approximately(Target, 0f) || Target < cruiseSpeed)
            Target = cruiseSpeed;
    }
}
