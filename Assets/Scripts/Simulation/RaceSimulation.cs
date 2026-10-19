using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// O **simulador de corridas** *(18/10/2026)*: joga a Game.unity de verdade,
/// corrida atrás de corrida, com um <see cref="SimulatedPilot"/> no lugar do
/// dedo, e anota o desfecho de cada uma.
///
/// Quem monta o plano e escreve o relatório é a janela do Editor
/// (Tools → Corrida no Espaço → Simulador de corridas). Daqui para baixo é só a
/// corrida: escolher nave, nível, fase e dificuldade, recarregar a cena, pôr o
/// piloto e esperar o fim.
///
/// <para>
/// **O que garante que o número é do jogo, e não do simulador:**
/// </para>
///
/// - **A cena é a mesma do jogador**, recarregada a cada corrida, como no menu.
/// - **O quadro tem tamanho fixo** (<see cref="Time.captureDeltaTime"/>): a
///   corrida anda tão depressa quanto o Editor aguenta, mas cada quadro vale o
///   mesmo tempo de jogo — 1/30 s por padrão, como no celular.
/// - **A sorte é numerada.** Cada repetição tem uma semente; as cinco personas
///   largam com a mesma, e rodar o mesmo plano amanhã dá a mesma sequência de
///   largadas. Mudou uma nave e rodou de novo: a diferença é da mudança, não do
///   sorteio. *(Quase toda: um obstáculo destruído muda onde o próximo pode
///   nascer, então a pista se afasta aos poucos entre pilotos diferentes.)*
/// - **O salvamento vira rascunho** (<see cref="SaveGame.UseSandbox"/>): o
///   simulador evolui nave, vence fase e entra em placar sem gravar nada.
/// </summary>
public static class RaceSimulation
{
    public const string GameScene = "Game";

    /// <summary>Chave do plano na sessão do Editor — é por ela que a janela passa o plano ao Play.</summary>
    public const string PlanKey = "cne.simulador.plano";

    public enum Outcome
    {
        Vitoria,
        Derrota,

        /// <summary>A sem fim acabou como acaba sempre: a nave caiu.</summary>
        FimDaSemFim,

        /// <summary>Passou do teto de segundos sem vencer nem cair.</summary>
        TempoEsgotado,
    }

    [Serializable]
    public class Plan
    {
        /// <summary>Cada persona como JSON — a ficha não atravessa o Play por referência.</summary>
        public List<string> personas = new List<string>();

        /// <summary>Nomes de asset das naves.</summary>
        public List<string> ships = new List<string>();

        public List<int> shipLevels = new List<int>();

        /// <summary>Nomes de asset das fases; a sem fim entra pelo nome dela também.</summary>
        public List<string> levels = new List<string>();

        public List<Difficulty> difficulties = new List<Difficulty>();

        public int repetitions = 5;
        public int firstSeed = 1;

        /// <summary>Teto de uma fase com dobra, em segundos de jogo.</summary>
        public float maxSeconds = 240f;

        /// <summary>Teto da sem fim, em segundos de jogo. Ela não tem fim, e alguém tem de dar um.</summary>
        public float maxEndlessSeconds = 300f;

        /// <summary>Segundos de jogo por quadro.</summary>
        public float frameSeconds = 1f / 30f;

        /// <summary>Desliga as câmeras: sem desenhar, cada corrida sai bem mais depressa.</summary>
        public bool hideRendering = true;

        /// <summary>
        /// O **teto do poder** *(19/10/2026)*: toda persona aperta o poder no
        /// primeiro perigo em que ele estiver pronto, em vez do hábito dela. O
        /// retrato de 18/10 mostrou Casual e Regular sem usar o poder em 70 a 80%
        /// das corridas — com isto ligado, o que se mede é o poder, e não o hábito.
        /// </summary>
        public bool powerOnDanger;
    }

    [Serializable]
    public class Job
    {
        public int persona;
        public string ship;
        public int shipLevel;
        public string level;
        public Difficulty difficulty;
        public int seed;
    }

    [Serializable]
    public class Result
    {
        public string persona;
        public int personaRank;
        public string ship;
        public string shipName;
        public int shipLevel;
        public string level;
        public string levelName;
        public int levelOrder;
        public bool endless;
        public Difficulty difficulty;
        public int seed;
        public Outcome outcome;
        public float seconds;
        public float distance;
        public float topSpeed;

        /// <summary>Segundo em que chegou à velocidade de dobra pela primeira vez. Negativo: não chegou.</summary>
        public float warpReachedAt = -1f;

        public int kills;
        public int crashes;

        /// <summary>Vida no fim, de 0 a 1.</summary>
        public float healthLeft;

        public int powerUses;

        /// <summary>A nave tem poder ativo — só para estas faz sentido o campo de baixo.</summary>
        public bool hasPower;

        /// <summary>Perdeu com o poder pronto e sem usar: o poder estava ali e não serviu.</summary>
        public bool diedWithPowerReady;
    }

    public static Plan Current { get; private set; }
    public static readonly List<Job> Jobs = new List<Job>();
    public static readonly List<Result> Results = new List<Result>();

    public static bool IsActive { get; private set; }
    public static bool IsDone { get; private set; }

    /// <summary>Segundos de relógio de parede desde o começo, para a janela estimar o resto.</summary>
    public static float RealSeconds =>
        IsDone ? endedAt - startedAt
        : IsActive ? Time.realtimeSinceStartup - startedAt
        : 0f;

    static PilotPersona[] personas;
    static float startedAt;
    static float endedAt;
    static RaceSimulationRunner runner;

    internal static PilotPersona PersonaAt(int index) => personas[index];

    /// <summary>
    /// Começa um plano. Configura a primeira corrida **antes** de a cena
    /// carregar: a nave lê nível e ficha no Awake, e depois seria tarde.
    /// </summary>
    public static void Begin(Plan plan)
    {
        Current = plan;
        Jobs.Clear();
        Results.Clear();
        IsDone = false;
        IsActive = true;
        startedAt = Time.realtimeSinceStartup;

        personas = new PilotPersona[plan.personas.Count];
        for (int i = 0; i < personas.Length; i++)
        {
            personas[i] = ScriptableObject.CreateInstance<PilotPersona>();
            JsonUtility.FromJsonOverwrite(plan.personas[i], personas[i]);
        }

        BuildJobs(plan);

        if (Jobs.Count == 0)
        {
            Debug.LogWarning("[Simulador] O plano não tem corrida nenhuma — marque ao menos uma persona, " +
                             "nave, nível, fase e dificuldade.");
            Finish();
            return;
        }

        SaveGame.UseSandbox();
        Time.captureDeltaTime = Mathf.Max(1f / 240f, plan.frameSeconds);

        var host = new GameObject("Simulador de corridas");
        UnityEngine.Object.DontDestroyOnLoad(host);
        runner = host.AddComponent<RaceSimulationRunner>();

        Prepare(Jobs[0]);
    }

    /// <summary>Para no meio. O que já rodou fica nos resultados.</summary>
    public static void Stop()
    {
        if (IsActive)
            Finish();
    }

    /// <summary>
    /// A ordem do laço importa: as personas ficam no laço de dentro, então as
    /// cinco jogam a mesma largada uma atrás da outra, e um relatório
    /// interrompido no meio ainda compara todo mundo no que rodou.
    /// </summary>
    static void BuildJobs(Plan plan)
    {
        foreach (string level in plan.levels)
        foreach (var difficulty in plan.difficulties)
        foreach (string ship in plan.ships)
        foreach (int shipLevel in plan.shipLevels)
        for (int repetition = 0; repetition < plan.repetitions; repetition++)
        for (int persona = 0; persona < plan.personas.Count; persona++)
        {
            Jobs.Add(new Job
            {
                persona = persona,
                ship = ship,
                shipLevel = shipLevel,
                level = level,
                difficulty = difficulty,
                seed = plan.firstSeed + repetition,
            });
        }
    }

    internal static void Prepare(Job job)
    {
        var ship = FindShip(job.ship);
        if (ship != null)
        {
            ShipSelection.Choose(ship);
            ShipProgress.SetLevel(ship, job.shipLevel);
        }

        LevelSelection.Choose(FindLevel(job.level), job.difficulty);
        UnityEngine.Random.InitState(job.seed);
    }

    internal static void Finish()
    {
        IsActive = false;
        IsDone = true;
        endedAt = Time.realtimeSinceStartup;
        Time.captureDeltaTime = 0f;

        if (runner != null)
            UnityEngine.Object.Destroy(runner.gameObject);
        runner = null;
    }

    static ShipDefinition FindShip(string assetName)
    {
        var catalog = ShipCatalog.Load();
        return catalog != null ? catalog.ByName(assetName) : null;
    }

    static LevelDefinition FindLevel(string assetName)
    {
        var catalog = LevelCatalog.Load();
        if (catalog == null)
            return null;

        if (catalog.endlessLevel != null && catalog.endlessLevel.name == assetName)
            return catalog.endlessLevel;

        foreach (var level in catalog.levels)
        {
            if (level != null && level.name == assetName)
                return level;
        }

        return null;
    }

#if UNITY_EDITOR
    /// <summary>
    /// A janela do Editor deixa o plano na sessão e aperta o Play; aqui ele é
    /// lido antes da primeira cena, que é o único momento em que a nave ainda
    /// não acordou.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void BeginFromEditor()
    {
        string json = UnityEditor.SessionState.GetString(PlanKey, string.Empty);
        if (string.IsNullOrEmpty(json))
            return;

        UnityEditor.SessionState.EraseString(PlanKey);
        Begin(JsonUtility.FromJson<Plan>(json));
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnLoad()
    {
        Current = null;
        Jobs.Clear();
        Results.Clear();
        IsActive = false;
        IsDone = false;
        runner = null;
    }
#endif
}
