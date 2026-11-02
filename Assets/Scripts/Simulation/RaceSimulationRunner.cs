using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// O que fica vivo entre uma cena e outra durante o simulador de corridas: põe
/// o piloto em cada corrida, olha o fim e passa para a próxima. Quem o cria é o
/// <see cref="RaceSimulation.Begin"/>; ninguém o põe em cena.
/// </summary>
public class RaceSimulationRunner : MonoBehaviour
{
    int index;
    bool racing;
    float warpReachedAt;
    SimulatedPilot pilot;

    /// <summary>De 0 a 4 passos por quadro. Dado próprio: o da pista não pode saber dos quadros.</summary>
    const float ShortestVariableFrame = 1f / 120f;
    const float LongestVariableFrame = 1f / 15f;
    readonly System.Random frameDice = new System.Random(1);

    int offStepFrames;
    float lastTimeScale;
    float secondsAtHighSpeed;
    int crashesAtHighSpeed;
    int powerUsesWhileCharging;
    bool reachedWarpWithEffect;
    int lastCrashes;
    int lastActivations;
    int lastShownSpeed;
    ShipLaneController shipLanes;
    int lastLane;
    float secondsInLane;
    float longestInLane;
    int longStaysInLane;

    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;

    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    /// <summary>
    /// Depois dos Awake e antes dos Start da cena nova — a hora certa de pôr o
    /// piloto e de semear a sorte, porque é no Start que a fase sorteia.
    /// </summary>
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!RaceSimulation.IsActive)
            return;

        if (scene.name != RaceSimulation.GameScene)
        {
            SceneManager.LoadScene(RaceSimulation.GameScene);
            return;
        }

        var job = RaceSimulation.Jobs[index];

        var ship = ShipStats.Instance;
        if (ship == null)
        {
            Debug.LogError("[Simulador] A Game.unity carregou sem nave. Rode o Montar e tente de novo.");
            RaceSimulation.Finish();
            return;
        }

        pilot = ship.gameObject.AddComponent<SimulatedPilot>();

        // Pelo número da persona, e não pela posição no plano: senão o Regular
        // rodando sozinho teria outro dado, e não compararia com a base.
        var persona = RaceSimulation.PersonaAt(job.persona);
        pilot.Configure(persona, RaceLuck.TrackSeed("piloto" + persona.skillRank));

        if (RaceSimulation.Current.hideRendering)
        {
            foreach (var camera in Camera.allCameras)
                camera.enabled = false;
        }

        warpReachedAt = -1f;
        offStepFrames = 0;
        lastTimeScale = Time.timeScale;
        secondsAtHighSpeed = 0f;
        crashesAtHighSpeed = 0;
        powerUsesWhileCharging = 0;
        reachedWarpWithEffect = false;
        lastCrashes = 0;
        lastActivations = 0;
        lastShownSpeed = 0;
        CollisionSweep.ResetHits();
        shipLanes = ship.GetComponent<ShipLaneController>();
        lastLane = -1;
        secondsInLane = 0f;
        longestInLane = 0f;
        longStaysInLane = 0;
        racing = true;
    }

    /// <summary>
    /// Anota o que a corrida fez neste quadro. Só lê: nada aqui pode mudar a corrida,
    /// senão o simulador mediria a si mesmo.
    /// </summary>
    void Observe(RaceDirector director)
    {
        ObserveLane();

        var race = RaceSpeed.Instance;
        if (race == null)
            return;

        // A velocidade do quadro anterior: a batida já derrubou a deste.
        if (CareerStats.RaceCrashes > lastCrashes && lastShownSpeed >= RaceSimulation.HighSpeedShown)
            crashesAtHighSpeed += CareerStats.RaceCrashes - lastCrashes;
        lastCrashes = CareerStats.RaceCrashes;

        lastShownSpeed = RaceSpeed.Shown(race.Current);
        if (lastShownSpeed >= RaceSimulation.HighSpeedShown)
            secondsAtHighSpeed += Time.deltaTime;

        var abilities = ShipAbilities.Instance;
        if (abilities == null)
            return;

        if (director.IsCharging && abilities.ActivationCount > lastActivations)
            powerUsesWhileCharging += abilities.ActivationCount - lastActivations;
        lastActivations = abilities.ActivationCount;
    }

    void ObserveLane()
    {
        if (shipLanes == null)
            return;

        if (shipLanes.CurrentLane != lastLane)
        {
            lastLane = shipLanes.CurrentLane;
            secondsInLane = 0f;
            return;
        }

        float before = secondsInLane;
        secondsInLane += Time.deltaTime;
        longestInLane = Mathf.Max(longestInLane, secondsInLane);

        if (before < RaceSimulation.LongStayInLane && secondsInLane >= RaceSimulation.LongStayInLane)
            longStaysInLane++;
    }

    /// <summary>Cada passo do jogo, como a corrida: o que se observa não depende dos quadros.</summary>
    void FixedUpdate()
    {
        if (!racing || !RaceSimulation.IsActive)
            return;

        var director = RaceDirector.Instance;
        var ship = ShipStats.Instance;
        if (director == null || ship == null || !director.IsRunning)
            return;

        Observe(director);

        var race = RaceSpeed.Instance;
        if (warpReachedAt < 0f && !director.IsEndless && race != null &&
            race.Current >= director.WarpSpeed)
        {
            warpReachedAt = director.Elapsed;
            reachedWarpWithEffect = ShipAbilities.Instance != null && ShipAbilities.Instance.Active.Count > 0;
        }

        var plan = RaceSimulation.Current;
        float limit = director.IsEndless ? plan.maxEndlessSeconds : plan.maxSeconds;
        if (director.Elapsed >= limit)
            Record(director, ship, RaceSimulation.Outcome.TempoEsgotado,
                   CareerStats.RaceKills, CareerStats.RaceCrashes);
    }

    void Update()
    {
        if (!racing || !RaceSimulation.IsActive)
            return;

        if (RaceSimulation.Current.variableFrames)
        {
            // Sem conferir o tamanho do quadro: aqui ele varia de propósito.
            Time.captureDeltaTime = Mathf.Lerp(ShortestVariableFrame, LongestVariableFrame,
                                               (float)frameDice.NextDouble());
        }
        else
        {
            // Contra a escala atual e a do quadro anterior: o Tempo Lento pode mudar a
            // escala no meio de um quadro, e isso não é erro de passo.
            float step = RaceSimulation.Current.frameSeconds;
            if (Mathf.Abs(Time.deltaTime - step * Time.timeScale) > 0.00001f &&
                Mathf.Abs(Time.deltaTime - step * lastTimeScale) > 0.00001f)
                offStepFrames++;
        }

        lastTimeScale = Time.timeScale;

        var director = RaceDirector.Instance;
        var ship = ShipStats.Instance;
        if (director == null || ship == null || director.IsRunning)
            return;

        var outcome = director.IsEndless ? RaceSimulation.Outcome.FimDaSemFim
            : ship.Health.IsAlive ? RaceSimulation.Outcome.Vitoria
            : RaceSimulation.Outcome.Derrota;
        Record(director, ship, outcome, CareerStats.LastRaceKills, CareerStats.LastRaceCrashes);
    }

    void Record(RaceDirector director, ShipStats ship, RaceSimulation.Outcome outcome, int kills, int crashes)
    {
        racing = false;

        var job = RaceSimulation.Jobs[index];
        var persona = RaceSimulation.PersonaAt(job.persona);
        var level = director.Level;

        var abilities = ShipAbilities.Instance;
        bool hasPower = abilities != null && abilities.Equipped != null;
        var spawner = FindAnyObjectByType<ObstacleSpawner>();
        var modifierSpawner = FindAnyObjectByType<LevelModifierSpawner>();

        // A batida letal fecha a corrida antes de o Observe vê-la.
        if (crashes > lastCrashes && lastShownSpeed >= RaceSimulation.HighSpeedShown)
            crashesAtHighSpeed += crashes - lastCrashes;

        var results = job.sentinel ? RaceSimulation.Sentinels : RaceSimulation.Results;
        results.Add(new RaceSimulation.Result
        {
            persona = persona.displayName,
            personaRank = persona.skillRank,
            ship = job.ship,
            shipName = ship.Definition != null ? ship.Definition.displayName : job.ship,
            shipLevel = ship.Level,
            level = job.level,
            levelName = level != null ? level.displayName : job.level,
            levelOrder = level != null ? (level.endless ? int.MaxValue : level.order) : 0,
            endless = director.IsEndless,
            difficulty = job.difficulty,
            seed = job.seed,
            outcome = outcome,
            seconds = director.Elapsed,
            distance = director.Distance,
            topSpeed = director.TopSpeed,
            warpReachedAt = warpReachedAt,
            kills = kills,
            crashes = crashes,
            healthLeft = ship.Health.Fraction,
            powerUses = abilities != null ? abilities.ActivationCount : 0,
            hasPower = hasPower,

            // O WasReadyAtEnd, e não o IsReady: o fim da corrida devolve usos e
            // recarga, e o IsReady daria "pronto" em toda derrota.
            diedWithPowerReady = hasPower && outcome == RaceSimulation.Outcome.Derrota && abilities.WasReadyAtEnd,
            powerRenewals = abilities != null ? abilities.RenewalCount : 0,
            powerRecharges = abilities != null ? abilities.RechargeCount : 0,
            effectSeconds = abilities != null ? abilities.EffectSeconds : 0f,
            conditionalCharges = abilities != null ? abilities.ConditionalCountAtEnd : 0,
            passivePeak = abilities != null ? abilities.PassivePeakCount : 0,
            offStepFrames = offStepFrames,
            trackHash = spawner != null ? spawner.TrackHash : string.Empty,
            secondsAtHighSpeed = secondsAtHighSpeed,
            crashesAtHighSpeed = crashesAtHighSpeed,
            powerUsesWhileCharging = powerUsesWhileCharging,
            reachedWarpWithEffect = reachedWarpWithEffect,
            wastedRecharges = abilities != null ? abilities.WastedRechargeCount : 0,
            sweepHits = CollisionSweep.Hits,
            emptyModifierTurns = modifierSpawner != null ? modifierSpawner.EmptyTurns : 0,
            longestInLane = longestInLane,
            longStaysInLane = longStaysInLane,
            shots = RaceDiagnostics.Shots,
            laneChanges = RaceDiagnostics.LaneChanges,
        });

        index++;
        if (index >= RaceSimulation.Jobs.Count)
        {
            RaceSimulation.Finish();
            return;
        }

        RaceSimulation.Prepare(RaceSimulation.Jobs[index]);

        // A corrida que acabou deixou o tempo parado. Solto agora, e não só ao
        // descarregar a cena: senão a próxima nasce com um quadro de tempo zero, o
        // piloto pensa um quadro antes, e só a primeira corrida do Play joga diferente.
        GameTime.ResetAll();
        SceneManager.LoadScene(RaceSimulation.GameScene);
    }
}
