using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manda no começo, no fim e no cronômetro da corrida. São três desfechos, e
/// qual deles vale depende do tipo de fase:
///
/// - **Fase de progressão, vitória:** a dobra completa. Destrava a próxima fase.
///   Não marca placar — estas fases existem para ensinar o jogo e apresentar
///   obstáculo novo, não para competir.
/// - **Fase de progressão, derrota:** a vida da nave chega a zero.
/// - **Fase sem fim:** a nave cair **é o fim previsto**, e não uma derrota. Lá a
///   corrida vale a **distância percorrida**, que é a única pontuação do jogo.
/// </summary>
[DefaultExecutionOrder(-30)]
public class RaceDirector : MonoBehaviour
{
    public static RaceDirector Instance { get; private set; }

    [Header("Regra da fase")]
    [Tooltip("Velocidade que entra em dobra e vence a fase, em unidades por segundo. " +
             "É regra da FASE, não atributo da nave: é a fase que decide o quanto se exige " +
             "para completar a dobra.")]
    [SerializeField, Min(1f)] float warpSpeed = 15f;

    [Tooltip("Segundos segurando a velocidade de dobra para a dobra completar. " +
             "É o ajuste de dificuldade mais direto da fase: quanto maior, mais tempo o jogador " +
             "precisa aguentar já correndo depressa demais para desviar com folga.")]
    [SerializeField, Min(0f)] float warpChargeSeconds = 5f;

    [Tooltip("Quanto da carga escoa por segundo abaixo da velocidade de dobra, como fração do " +
             "ritmo de carga. 0 = a carga só pausa; 1 = escoa tão rápido quanto enche.")]
    [SerializeField, Range(0f, 3f)] float warpDecayRate = 0.5f;

    [SerializeField] string menuSceneName = "Menu";

    [Header("Fim de corrida — fases de progressão")]
    [SerializeField] GameObject victoryPanel;
    [SerializeField] GameObject defeatPanel;
    [SerializeField] TMP_Text victoryTimeLabel;

    [Tooltip("Onde a vitória conta o que foi destravado.")]
    [SerializeField] TMP_Text victoryUnlockLabel;

    [Header("Fim de corrida — fase sem fim")]
    [SerializeField] GameObject endlessPanel;
    [SerializeField] TMP_Text endlessDistanceLabel;
    [SerializeField] TMP_InputField nameField;
    [SerializeField] TMP_Text placementLabel;
    [SerializeField] RecordsBoard recordsBoard;

    [Tooltip("Some com estes objetos quando a corrida acaba: HUD e botão de menu.")]
    [SerializeField] GameObject[] hideOnEnd;

    /// <summary>Tempo de corrida até agora, em segundos.</summary>
    public float Elapsed { get; private set; }

    /// <summary>
    /// Distância percorrida, em unidades de mundo. É a velocidade **integrada no
    /// tempo**, e não tempo vezes velocidade final: a velocidade muda o tempo
    /// todo, e quem acelerou cedo tem de levar vantagem sobre quem acelerou no
    /// fim. É a pontuação da fase sem fim.
    /// </summary>
    public float Distance { get; private set; }

    /// <summary>Falso depois da vitória ou da derrota — arma e spawner consultam.</summary>
    public bool IsRunning { get; private set; }

    /// <summary>Segundos de dobra já acumulados. A vitória é encher isto até o tempo da fase.</summary>
    public float WarpCharge { get; private set; }

    /// <summary>A nave está agora na velocidade de dobra, carregando.</summary>
    public bool IsCharging { get; private set; }

    /// <summary>Quanto falta de carga, em segundos.</summary>
    public float WarpSecondsLeft => Mathf.Max(0f, warpChargeSeconds - WarpCharge);

    /// <summary>Carga de 0 a 1, para barra e cor de HUD.</summary>
    public float WarpFraction =>
        warpChargeSeconds > 0f ? Mathf.Clamp01(WarpCharge / warpChargeSeconds) : 1f;

    public float WarpSpeed => warpSpeed;

    /// <summary>A fase que está rodando. Nula só se o catálogo não carregar.</summary>
    public LevelDefinition Level { get; private set; }

    /// <summary>Fase sem fim: não tem dobra, e a corrida só acaba quando a nave cai.</summary>
    public bool IsEndless => Level != null && Level.endless;

    RaceSpeed race;
    Health shipHealth;
    bool scoreSaved;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        race = RaceSpeed.Instance;
        ApplyLevel();
        ApplySpeedCeiling();

        if (ShipStats.Instance != null)
        {
            shipHealth = ShipStats.Instance.Health;
            shipHealth.Died += Lose;
        }

        SetActive(victoryPanel, false);
        SetActive(defeatPanel, false);
        SetActive(endlessPanel, false);

        Elapsed = 0f;
        Distance = 0f;
        WarpCharge = 0f;
        IsCharging = false;
        scoreSaved = false;
        IsRunning = true;

        // A corrida sempre começa andando: se a cena anterior parou o tempo — ou
        // a deixou lenta —, aqui é onde ele volta.
        GameTime.ResetAll();
    }

    void OnDisable()
    {
        if (shipHealth != null)
            shipHealth.Died -= Lose;

        // Nunca deixar a próxima cena nascer congelada, nem arrastada.
        GameTime.ResetAll();
    }

    /// <summary>
    /// A fase escolhida no menu manda nas regras. Os valores do Inspector viram
    /// só o que vale ao abrir a Game.unity sem catálogo — útil para testar a
    /// cena solta no Editor.
    /// </summary>
    void ApplyLevel()
    {
        Level = LevelSelection.Level;
        if (Level == null)
            return;

        var settings = LevelSelection.Settings();

        warpSpeed = Level.warpSpeed + settings.warpSpeedBonus;
        warpChargeSeconds = Level.warpChargeSeconds;
    }

    /// <summary>
    /// A velocidade de dobra é o **teto da corrida**: chegar nela é o objetivo da
    /// fase, e passar dela não serve para nada. Na fase sem fim não existe dobra,
    /// então lá não há teto — a corrida fica perigosa com o tempo mesmo para quem
    /// não atira em nada.
    ///
    /// <para>
    /// **Virou teto de verdade em 31/08/2026** *(pedido do Raffael)*. Até então
    /// ele só segurava o ganho passivo, e o abate passava por cima: quem destruía
    /// obstáculo entrava em dobra com folga acima do limiar, e quem só desviava
    /// entrava colado nele. Sem querer, **destruir comprava um seguro contra
    /// errar** nos 2,5 segundos mais tensos da corrida. Agora os dois jeitos de
    /// jogar fazem a mesma prova. Ver <see cref="RaceSpeed.SpeedCeiling"/>.
    /// </para>
    /// </summary>
    void ApplySpeedCeiling()
    {
        if (race == null)
            return;

        race.SetSpeedCeiling(IsEndless ? float.PositiveInfinity : warpSpeed);
    }

    void Update()
    {
        if (!IsRunning)
            return;

        Elapsed += Time.deltaTime;

        // Integra a velocidade a cada frame, em vez de multiplicar no fim: é o
        // que faz acelerar cedo valer mais do que acelerar no último segundo.
        if (race != null)
            Distance += race.Current * Time.deltaTime;

        UpdateWarpCharge();
    }

    /// <summary>
    /// A dobra não acontece no instante em que a velocidade chega: a nave tem de
    /// **segurar** a velocidade pelo tempo que a fase pedir. É o que faz o fim da
    /// corrida ser o trecho mais tenso — já rápido demais para desviar com folga,
    /// e ainda faltando aguentar.
    /// </summary>
    void UpdateWarpCharge()
    {
        // Na fase sem fim não existe dobra: a corrida acaba quando a nave cai, e
        // a pontuação é quanto tempo ela aguentou.
        if (race == null || IsEndless)
            return;

        IsCharging = race.Current >= warpSpeed;

        if (IsCharging)
        {
            WarpCharge += Time.deltaTime;

            if (WarpCharge >= warpChargeSeconds)
                Win();

            return;
        }

        // Escoa em vez de zerar: uma batida no fim da carga custa caro, mas não
        // apaga a corrida inteira e manda o jogador recomeçar do zero.
        if (WarpCharge > 0f)
            WarpCharge = Mathf.Max(0f, WarpCharge - Time.deltaTime * warpDecayRate);
    }

    /// <summary>
    /// Fase de progressão vencida. Sem placar de propósito: o prêmio destas
    /// fases é a próxima fase, não uma posição em tabela.
    /// </summary>
    void Win()
    {
        if (!IsRunning)
            return;

        EndRace();

        if (victoryTimeLabel != null)
            victoryTimeLabel.text = ScoreBoard.FormatTime(Elapsed);

        if (victoryUnlockLabel != null)
            victoryUnlockLabel.text = RegisterProgress();

        SetActive(victoryPanel, true);
    }

    /// <summary>
    /// Vencer anda com a corrente de progressão. Devolve o que contar ao jogador
    /// — repetir uma fase já vencida não destrava nada, e é melhor dizer isso do
    /// que deixar o painel mudo.
    /// </summary>
    string RegisterProgress()
    {
        var catalog = LevelCatalog.Load();
        if (catalog == null || Level == null)
            return string.Empty;

        if (!LevelProgress.MarkCleared(catalog, Level, LevelSelection.Difficulty))
            return "Você já tinha vencido esta fase.";

        int next = LevelProgress.StagesCleared;
        return next >= LevelProgress.TotalStages(catalog)
            ? "Você fechou todas as fases!"
            : "Destravou: " + LevelProgress.DescribeStage(catalog, next);
    }

    void Lose()
    {
        if (!IsRunning)
            return;

        EndRace();

        // Na fase sem fim, a nave cair é o fim previsto da corrida — foi até
        // onde deu. Chamar aquilo de derrota seria punir o jogador pela única
        // coisa que a fase permite que aconteça.
        if (IsEndless)
        {
            ShowEndlessResult();
            return;
        }

        SetActive(defeatPanel, true);
    }

    void ShowEndlessResult()
    {
        if (endlessDistanceLabel != null)
            endlessDistanceLabel.text = ScoreBoard.FormatDistance(Distance);

        if (nameField != null)
            nameField.text = ScoreBoard.LastName;

        if (placementLabel != null)
            placementLabel.text = "Salve sua distância na tabela.";

        SetActive(endlessPanel, true);
    }

    void EndRace()
    {
        IsRunning = false;

        // Poder é da partida, não do jogador: a corrida acabou, tudo cai. Hoje a
        // cena sempre recarrega antes de uma corrida nova, então isto não muda
        // nada visível — mas é o que faz cada poder receber o fim dele em vez de
        // simplesmente ser destruído, que é o contrato dos dois sistemas.
        if (LevelModifiers.Instance != null)
            LevelModifiers.Instance.ClearAll();

        if (ShipAbilities.Instance != null)
            ShipAbilities.Instance.ResetForRace();

        if (hideOnEnd != null)
        {
            foreach (var target in hideOnEnd)
                SetActive(target, false);
        }

        // Congela a corrida por trás do painel. A UI continua respondendo:
        // o Unity não usa o timeScale para processar toque em botão.
        //
        // Pelo GameTime, e não no timeScale direto: um modificador de tempo lento
        // pode estar valendo na hora em que a corrida acaba, e a pausa precisa
        // ganhar dele sem que um dependa de saber do outro.
        GameTime.SetPaused(true);
    }

    /// <summary>
    /// Botão "Salvar" do painel da fase sem fim. Só ela tem placar — a guarda do
    /// <see cref="IsEndless"/> é o que impede uma fase de progressão de entrar na
    /// tabela, se um dia alguém ligar este botão no painel errado.
    /// </summary>
    public void SaveScore()
    {
        if (scoreSaved || !IsEndless)
            return;

        scoreSaved = true;

        string name = nameField != null ? nameField.text : string.Empty;
        int position = ScoreBoard.Submit(name, Distance);

        if (placementLabel != null)
        {
            placementLabel.text = position >= 0
                ? $"{position + 1}º lugar na tabela!"
                : "Fora do top 10 desta vez.";
        }

        if (recordsBoard != null)
            recordsBoard.Refresh();

        if (nameField != null)
            nameField.interactable = false;
    }

    /// <summary>Botão "Voltar ao menu", nos três painéis de fim.</summary>
    public void BackToMenu()
    {
        GameTime.ResetAll();
        SceneManager.LoadScene(menuSceneName);
    }

    static void SetActive(GameObject target, bool value)
    {
        if (target != null)
            target.SetActive(value);
    }
}
