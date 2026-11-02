using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Escreve no log, ao fim de cada corrida, uma linha com o que aconteceu nela. Lida
/// no celular pelo <c>tools/logcat.ps1</c>.
///
/// **Por que existe** *(02/11/2026)*: a nave parada rendeu no aparelho mais que o
/// dobro do que no simulador, e o código não explica. Esta linha compara os dois
/// com os mesmos números. Só observa: nada aqui muda a corrida.
///
/// **Ninguém o põe em cena**, como o <see cref="RaceClock"/>.
/// </summary>
public class RaceDiagnostics : MonoBehaviour
{
    /// <summary>Acima disto, o simulador não escreve a linha: uma rodada longa encheria o Console.</summary>
    const int MaxSimulatedRacesToLog = 50;

    /// <summary>Um quadro mais longo que isto é engasgo, para quem deveria rodar a 30 por segundo.</summary>
    const float HitchSeconds = 0.05f;

    /// <summary>Tiros disparados na corrida atual.</summary>
    public static int Shots { get; private set; }

    /// <summary>Trocas de faixa na corrida atual, de qualquer origem: dedo, Super IA ou Rastro.</summary>
    public static int LaneChanges { get; private set; }

    static int hitches;
    static int startFrame;
    static float startRealtime;

    ShipLaneController lanes;

    public static void NoteShot() => Shots++;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnLoad()
    {
        Shots = 0;
        LaneChanges = 0;
        hitches = 0;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Create()
    {
        var host = new GameObject("Diagnóstico da corrida");
        DontDestroyOnLoad(host);
        host.AddComponent<RaceDiagnostics>();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        CareerStats.RaceFinished += OnRaceFinished;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        CareerStats.RaceFinished -= OnRaceFinished;
        WatchLanes(null);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Shots = 0;
        LaneChanges = 0;
        hitches = 0;
        startFrame = Time.frameCount;
        startRealtime = Time.realtimeSinceStartup;

        WatchLanes(ShipStats.Instance != null ? ShipStats.Instance.GetComponent<ShipLaneController>() : null);
    }

    void WatchLanes(ShipLaneController next)
    {
        if (lanes != null)
            lanes.LaneChanged -= OnLaneChanged;

        lanes = next;

        if (lanes != null)
            lanes.LaneChanged += OnLaneChanged;
    }

    static void OnLaneChanged(int lane) => LaneChanges++;

    void Update()
    {
        if (Time.unscaledDeltaTime > HitchSeconds)
            hitches++;
    }

    static void OnRaceFinished(CareerStats.Outcome outcome)
    {
        if (RaceSimulation.IsActive && RaceSimulation.Jobs.Count > MaxSimulatedRacesToLog)
            return;

        var director = RaceDirector.Instance;
        var ship = ShipStats.Instance;
        if (director == null || ship == null)
            return;

        float realSeconds = Mathf.Max(0.001f, Time.realtimeSinceStartup - startRealtime);
        int frames = Time.frameCount - startFrame;

        Debug.Log(
            $"[Diagnostico] {(ship.Definition != null ? ship.Definition.displayName : "?")} nv {ship.Level}" +
            $" | {(director.Level != null ? director.Level.displayName : "?")} {LevelSelection.Difficulty} | {outcome}" +
            $" | {director.Elapsed:0.0} s de jogo em {realSeconds:0.0} s reais" +
            $" | {frames / realSeconds:0.0} q/s, {hitches} engasgos" +
            $" | {ScoreBoard.FormatDistance(director.Distance)}, max {RaceSpeed.Format(director.TopSpeed)}" +
            $" | {CareerStats.LastRaceCrashes} batidas, {CareerStats.LastRaceKills} abates, {Shots} tiros, {LaneChanges} trocas de faixa" +
            $" | vida {ship.Health.Current:0}/{ship.Health.Max:0}, defesa {ship.DefensePercent:0}%, dano {ship.Damage:0.##}," +
            $" cadencia {ship.AttackSpeed:0.##}, abate {ship.KillSpeedGain:0.##}");
    }
}
