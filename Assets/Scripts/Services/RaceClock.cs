using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// O relógio **da corrida**: quanto tempo de jogo passou desde que a cena carregou.
/// Quem marca "até quando" (recarga do poder, baque da batida) usa este, e não o
/// <see cref="Time.time"/>.
///
/// **Por que não o <see cref="Time.time"/>:** ele conta desde o início do Play, em
/// float. Em horas de simulador, o passo dele engrossa, e duas corridas iguais
/// passam a dar resultados diferentes conforme a posição na rodada. Aqui cada
/// corrida começa do zero. Para o jogador, nada muda: anda com o mesmo
/// <see cref="Time.deltaTime"/>, e também para na pausa.
///
/// **Ninguém o põe em cena.** Ele nasce sozinho e sobrevive às trocas de cena, e
/// roda antes de todo mundo, para ninguém ler a hora do quadro anterior.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class RaceClock : MonoBehaviour
{
    /// <summary>Segundos de jogo desde que a cena atual carregou.</summary>
    public static float Now { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnLoad() => Now = 0f;

    /// <summary>
    /// O maior pedaço de tempo que um quadro pode andar. Num engasgo mais longo, o
    /// jogo desacelera em vez de saltar: com a colisão por trecho, um salto grande
    /// poderia cobrar duas batidas de uma vez, sem chance de reação. O simulador não
    /// sente isto, porque o quadro dele é bem menor.
    /// </summary>
    const float MaxFrameSeconds = 0.1f;

    /// <summary>
    /// O passo fixo do jogo. **Regra do Raffael, 02/11/2026: consistência.** Toda a lógica
    /// da corrida anda em FixedUpdate, sempre neste passo, para o resultado não depender
    /// dos quadros por segundo do aparelho. Só o que é visual anda em Update.
    /// </summary>
    public const float TickSeconds = 1f / 60f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Create()
    {
        Time.fixedDeltaTime = TickSeconds;
        Time.maximumDeltaTime = MaxFrameSeconds;

        var host = new GameObject("Relógio da corrida");
        DontDestroyOnLoad(host);
        host.AddComponent<RaceClock>();
    }

    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;

    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Now = 0f;
        RaceLuck.NewRace();
    }

    void FixedUpdate() => Now += Time.deltaTime;
}
