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

    [Header("Regra")]
    [Tooltip("Velocidade que entra em dobra e vence a fase, em unidades por segundo.")]
    [SerializeField, Min(1f)] float warpSpeed = 15f;

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

    public float WarpSpeed => warpSpeed;

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

        if (ShipStats.Instance != null)
        {
            shipHealth = ShipStats.Instance.Health;
            shipHealth.Died += Lose;
        }

        SetActive(victoryPanel, false);
        SetActive(defeatPanel, false);

        Elapsed = 0f;
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

    void Update()
    {
        if (!IsRunning)
            return;

        Elapsed += Time.deltaTime;

        if (race != null && race.Current >= warpSpeed)
            Win();
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
