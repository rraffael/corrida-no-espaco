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

    // ─────────────────────────────────────────────────────────────────────
    // PROVISÓRIO — números da nave chutados, sem ficha de status ainda.
    //
    // Quando existir a ficha da nave (velocidade, vida e o que mais vier), ela
    // chama ApplyShipStats() uma vez e estes três campos param de valer. É o
    // único ponto de troca: nada mais no jogo lê estes números direto.
    // ─────────────────────────────────────────────────────────────────────
    [Header("Provisório — trocar pela ficha da nave")]
    [Tooltip("Velocidade de cruzeiro: o padrão da nave, para onde ela acelera sozinha.")]
    [SerializeField, Min(0f)] float cruiseSpeed = 8f;

    [Tooltip("Quanto a velocidade muda por segundo. 4 = leva 2s do zero até a de cruzeiro.")]
    [SerializeField, Min(0.1f)] float acceleration = 4f;

    [Tooltip("Teto de velocidade. Nada consegue empurrar a nave além disto.")]
    [SerializeField, Min(0.1f)] float maxSpeed = 16f;

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
    /// Empurrão imediato, para cima ou para baixo: é isto que um obstáculo ou
    /// um item vai chamar. A velocidade volta para a de cruzeiro sozinha depois.
    /// </summary>
    public void Nudge(float delta)
    {
        float next = Mathf.Clamp(Current + delta, 0f, maxSpeed);
        if (Mathf.Approximately(next, Current))
            return;

        Current = next;
        Changed?.Invoke(Current);
    }

    /// <summary>
    /// Ponto único de troca dos números provisórios. Quando a ficha da nave
    /// existir, ela chama isto no início da fase e os campos do Inspector viram
    /// só valor de partida para o Editor.
    /// </summary>
    public void ApplyShipStats(float cruise, float accelerationPerSecond, float top)
    {
        cruiseSpeed = Mathf.Max(0f, cruise);
        acceleration = Mathf.Max(0.1f, accelerationPerSecond);
        maxSpeed = Mathf.Max(0.1f, top);

        Target = cruiseSpeed;
        Current = Mathf.Clamp(Current, 0f, maxSpeed);
    }
}
