using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manda no começo, no fim e no cronômetro da corrida.
///
/// - **Derrota:** a vida da nave chega a zero.
/// - **Vitória:** a velocidade alcança a de dobra.
/// - **Pontuação:** o tempo até entrar em dobra. Menor é melhor.
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

    [Header("Fim de corrida")]
    [SerializeField] GameObject victoryPanel;
    [SerializeField] GameObject defeatPanel;
    [SerializeField] TMP_Text victoryTimeLabel;
    [SerializeField] TMP_InputField nameField;
    [SerializeField] TMP_Text victoryPlacementLabel;
    [SerializeField] RecordsBoard victoryBoard;

    [Tooltip("Some com estes objetos quando a corrida acaba: HUD e botão de menu.")]
    [SerializeField] GameObject[] hideOnEnd;

    /// <summary>Tempo de corrida até agora, em segundos.</summary>
    public float Elapsed { get; private set; }

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
        WarnIfUnwinnable();

        if (ShipStats.Instance != null)
        {
            shipHealth = ShipStats.Instance.Health;
            shipHealth.Died += Lose;
        }

        SetActive(victoryPanel, false);
        SetActive(defeatPanel, false);

        Elapsed = 0f;
        WarpCharge = 0f;
        IsCharging = false;
        scoreSaved = false;
        IsRunning = true;

        // A corrida sempre começa andando: se a cena anterior parou o tempo, aqui
        // é onde ele volta.
        Time.timeScale = 1f;
    }

    void OnDisable()
    {
        if (shipHealth != null)
            shipHealth.Died -= Lose;

        // Nunca deixar a próxima cena nascer congelada.
        Time.timeScale = 1f;
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
    /// A velocidade de dobra acima do teto do <see cref="RaceSpeed"/> faz uma
    /// fase que ninguém consegue vencer, e sem aviso nenhum: o jogador só
    /// percebe correndo atrás de uma meta que a física do jogo não alcança.
    /// </summary>
    void WarnIfUnwinnable()
    {
        if (IsEndless || race == null || warpSpeed <= race.MaxSpeed)
            return;

        Debug.LogError(
            $"[Corrida] Fase invencível: a dobra exige {warpSpeed} u/s, mas o teto de " +
            $"velocidade é {race.MaxSpeed} u/s. Baixe a dobra ou levante o teto no RaceSpeed.",
            this);
    }

    void Update()
    {
        if (!IsRunning)
            return;

        Elapsed += Time.deltaTime;
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

    void Win()
    {
        if (!IsRunning)
            return;

        EndRace();

        if (victoryTimeLabel != null)
            victoryTimeLabel.text = ScoreBoard.FormatTime(Elapsed);

        if (nameField != null)
            nameField.text = ScoreBoard.LastName;

        if (victoryPlacementLabel != null)
            victoryPlacementLabel.text = "Salve seu tempo na tabela.";

        SetActive(victoryPanel, true);
    }

    void Lose()
    {
        if (!IsRunning)
            return;

        EndRace();
        SetActive(defeatPanel, true);
    }

    void EndRace()
    {
        IsRunning = false;

        if (hideOnEnd != null)
        {
            foreach (var target in hideOnEnd)
                SetActive(target, false);
        }

        // Congela a corrida por trás do painel. A UI continua respondendo:
        // o Unity não usa o timeScale para processar toque em botão.
        Time.timeScale = 0f;
    }

    /// <summary>Botão "Salvar" do painel de vitória.</summary>
    public void SaveScore()
    {
        if (scoreSaved)
            return;

        scoreSaved = true;

        string name = nameField != null ? nameField.text : string.Empty;
        int position = ScoreBoard.Submit(name, Elapsed);

        if (victoryPlacementLabel != null)
        {
            victoryPlacementLabel.text = position >= 0
                ? $"{position + 1}º lugar na tabela!"
                : "Tempo guardado, mas fora do top 10.";
        }

        if (victoryBoard != null)
            victoryBoard.Refresh();

        if (nameField != null)
            nameField.interactable = false;
    }

    /// <summary>Botão "Voltar ao menu", nos dois painéis de fim.</summary>
    public void BackToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName);
    }

    static void SetActive(GameObject target, bool value)
    {
        if (target != null)
            target.SetActive(value);
    }
}
