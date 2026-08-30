using UnityEngine;

/// <summary>
/// Solta os obstáculos da fase. O que vem, e a partir de quando, está na ficha
/// da fase; a dificuldade aperta o ritmo e engrossa os números.
///
/// A regra que mais importa aqui é a da **fuga garantida** — nenhuma leva pode
/// fechar todas as faixas ao mesmo tempo, senão o jogo mataria o jogador por
/// sorteio e não por erro dele. Ela vive no <see cref="LaneOccupancy"/>, que é
/// onde obstáculo e modificador de fase se encontram: os dois nascem nas mesmas
/// faixas, e a fuga só existe se for contada contra os dois juntos.
/// </summary>
public class ObstacleSpawner : MonoBehaviour
{
    [Tooltip("De onde vêm as posições das faixas. Vazio: procura o LaneTrack da cena.")]
    [SerializeField] LaneTrack track;

    [Tooltip("Altura em que o obstáculo nasce, acima do topo da tela.")]
    [SerializeField] float spawnY = 7f;

    [Tooltip("Distância vertical dentro da qual dois obstáculos contam como a mesma leva. " +
             "É o que a regra de fuga considera ao escolher a faixa.")]
    [SerializeField, Min(0.5f)] float waveWindow = 3.5f;

    LevelDefinition level;
    DifficultySettings difficulty;

    /// <summary>Segundo sorteado em que cada tipo entra em cena, nesta corrida.</summary>
    float[] entryTimes;

    float timer;
    float elapsed;

    void Awake()
    {
        if (track == null)
            track = LaneTrack.Instance != null ? LaneTrack.Instance : FindAnyObjectByType<LaneTrack>();
    }

    void Start()
    {
        level = LevelSelection.Level;
        difficulty = LevelSelection.Settings();

        if (level == null)
        {
            Debug.LogError("[Obstáculos] Nenhuma fase selecionada e nenhuma no catálogo. " +
                           "O corredor fica vazio.", this);
            enabled = false;
            return;
        }

        RollEntryTimes();
        timer = level.IntervalAt(0f) * difficulty.intervalFactor;
    }

    /// <summary>
    /// Sorteia, uma vez por corrida, o segundo em que cada tipo estreia. Duas
    /// partidas da mesma fase não ficam idênticas.
    /// </summary>
    void RollEntryTimes()
    {
        entryTimes = new float[level.obstacles.Length];

        for (int i = 0; i < entryTimes.Length; i++)
        {
            var schedule = level.obstacles[i];
            float earliest = Mathf.Min(schedule.earliestSeconds, schedule.latestSeconds);
            float latest = Mathf.Max(schedule.earliestSeconds, schedule.latestSeconds);
            entryTimes[i] = Random.Range(earliest, latest);
        }
    }

    void Update()
    {
        if (track == null || level == null)
            return;

        if (RaceDirector.Instance != null && !RaceDirector.Instance.IsRunning)
            return;

        elapsed += Time.deltaTime;

        timer -= Time.deltaTime;
        if (timer > 0f)
            return;

        float interval = level.IntervalAt(elapsed) * difficulty.intervalFactor;
        timer = Mathf.Max(0.2f, interval + Random.Range(-level.intervalJitter, level.intervalJitter));

        TrySpawn();
    }

    void TrySpawn()
    {
        var stats = PickType();
        if (stats == null)
            return;

        // A regra da faixa livre mora no LaneOccupancy desde 30/08/2026, e não
        // mais aqui: ela passou a valer contra obstáculo E modificador de fase,
        // e cada sorteador garantindo a fuga por conta própria não garantia nada.
        if (!LaneOccupancy.TryPickLane(track.LaneCount, Mathf.Max(1, stats.laneSpan),
                                       spawnY, waveWindow, out int lane))
            return; // A leva atual já não deixa saída: melhor pular a batida.

        Spawn(stats, lane);
    }

    /// <summary>Sorteio por peso entre os tipos já liberados pelo relógio da corrida.</summary>
    ObstacleStats PickType()
    {
        float total = 0f;
        for (int i = 0; i < level.obstacles.Length; i++)
        {
            if (IsReleased(i))
                total += level.obstacles[i].weight;
        }

        if (total <= 0f)
            return null;

        float roll = Random.Range(0f, total);
        for (int i = 0; i < level.obstacles.Length; i++)
        {
            if (!IsReleased(i))
                continue;

            roll -= level.obstacles[i].weight;
            if (roll <= 0f)
                return level.obstacles[i].stats;
        }

        return null;
    }

    bool IsReleased(int index)
    {
        var schedule = level.obstacles[index];
        return schedule != null && schedule.stats != null && elapsed >= entryTimes[index];
    }

    void Spawn(ObstacleStats stats, int lane)
    {
        var obstacle = new GameObject($"Obstaculo {stats.displayName} (faixa {lane})");
        obstacle.transform.SetParent(transform, false);

        // Com laneSpan 2, o centro fica na divisa entre as duas faixas.
        float centerX = (track.LaneCenterX(lane) + track.LaneCenterX(lane + stats.laneSpan - 1)) * 0.5f;
        obstacle.transform.position = new Vector3(centerX, spawnY, 0f);

        var renderer = obstacle.AddComponent<SpriteRenderer>();
        renderer.sprite = stats.sprite;
        renderer.color = stats.color;
        renderer.sortingOrder = 8;

        if (stats.sprite != null)
        {
            var spriteSize = stats.sprite.bounds.size;
            if (spriteSize.x > 0f && spriteSize.y > 0f)
            {
                obstacle.transform.localScale = new Vector3(
                    stats.WidthFor(track) / spriteSize.x,
                    stats.height / spriteSize.y,
                    1f);
            }
        }

        // Health antes de Obstacle: o [RequireComponent] resolveria sozinho, mas
        // na ordem explícita dá para configurar antes de o Update rodar.
        obstacle.AddComponent<Health>();
        obstacle.AddComponent<Obstacle>().Configure(
            stats, track, lane,
            difficulty.obstacleHealthFactor,
            difficulty.obstacleDamageFactor);
    }
}
