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
        Random.InitState(job.seed);

        var ship = ShipStats.Instance;
        if (ship == null)
        {
            Debug.LogError("[Simulador] A Game.unity carregou sem nave. Rode o Montar e tente de novo.");
            RaceSimulation.Finish();
            return;
        }

        pilot = ship.gameObject.AddComponent<SimulatedPilot>();
        pilot.Configure(RaceSimulation.PersonaAt(job.persona), job.seed * 7919 + job.persona);

        if (RaceSimulation.Current.hideRendering)
        {
            foreach (var camera in Camera.allCameras)
                camera.enabled = false;
        }

        warpReachedAt = -1f;
        racing = true;
    }

    void Update()
    {
        if (!racing || !RaceSimulation.IsActive)
            return;

        var director = RaceDirector.Instance;
        var ship = ShipStats.Instance;
        if (director == null || ship == null)
            return;

        if (director.IsRunning)
        {
            var race = RaceSpeed.Instance;
            if (warpReachedAt < 0f && !director.IsEndless && race != null &&
                race.Current >= director.WarpSpeed)
                warpReachedAt = director.Elapsed;

            var plan = RaceSimulation.Current;
            float limit = director.IsEndless ? plan.maxEndlessSeconds : plan.maxSeconds;
            if (director.Elapsed >= limit)
                Record(director, ship, RaceSimulation.Outcome.TempoEsgotado,
                       CareerStats.RaceKills, CareerStats.RaceCrashes);

            return;
        }

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

        RaceSimulation.Results.Add(new RaceSimulation.Result
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
            powerUses = pilot != null ? pilot.PowerUses : 0,
            hasPower = hasPower,
            diedWithPowerReady = hasPower && outcome == RaceSimulation.Outcome.Derrota && abilities.IsReady,
        });

        index++;
        if (index >= RaceSimulation.Jobs.Count)
        {
            RaceSimulation.Finish();
            return;
        }

        RaceSimulation.Prepare(RaceSimulation.Jobs[index]);
        SceneManager.LoadScene(RaceSimulation.GameScene);
    }
}
