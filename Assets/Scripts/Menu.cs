using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Menu inicial: jogar, ver as naves, ver as conquistas ou sair.
///
/// "Jogar" não carrega a cena na hora — abre a seleção de fase. Escolhida a
/// **sem fim**, passa pela transição que mostra os três primeiros da tabela;
/// escolhida uma **fase**, vai direto para a corrida. O placar de cada fase se
/// vê pelo troféu da linha dela *(decisão do Raffael, 17/10/2026 — até então a
/// transição mostrava a tabela da sem fim antes de qualquer fase)*.
///
/// Os nomes dos métodos públicos estão ligados aos botões dentro da cena:
/// renomear aqui quebra a fiação de lá.
/// </summary>
public class Menu : MonoBehaviour
{
    [Header("Painéis")]
    [SerializeField] GameObject levelSelectPanel;
    [SerializeField] LeaderboardPanel leaderboardPanel;
    [SerializeField] GameObject shipsPanel;
    [SerializeField] GameObject achievementsPanel;
    [SerializeField] GameObject transitionPanel;

    [Tooltip("Botões do menu, escondidos enquanto um painel está aberto.")]
    [SerializeField] GameObject buttonsRoot;

    [Tooltip("O título do jogo. Some junto com os botões: desde 18/10/2026 ele mora no alto da " +
             "tela, e as caixas dos painéis chegam até lá — aparecendo por trás do fundo escuro, " +
             "ele embolava com o título do painel.")]
    [SerializeField] GameObject title;

    [Header("Transição")]
    [Tooltip("Segundos mostrando o pódio antes de a partida começar.")]
    [SerializeField, Min(0.5f)] float transitionSeconds = 3f;

    [SerializeField] string gameSceneName = "Game";

    bool loading;

    void Awake()
    {
        // Estado pelo código, e não pelo que ficou salvo na cena: quem deixar um
        // painel aberto no Editor para mexer no layout não publica o menu torto.
        Show();
    }

    /// <summary>Botão "Jogar".</summary>
    public void OnPlayButton()
    {
        if (loading)
            return;

        // Sem a tela de seleção montada, o botão faz o que sempre fez: joga a
        // fase que o LevelSelection já tiver escolhido.
        if (levelSelectPanel == null)
        {
            BeginTransition();
            return;
        }

        Show(levelSelect: true);
    }

    /// <summary>
    /// Chamado pela tela de seleção quando o jogador toca numa fase destravada.
    /// É o único lugar que grava a escolha: a tela de seleção só desenha.
    /// </summary>
    public void StartRace(LevelDefinition level, Difficulty difficulty)
    {
        if (loading)
            return;

        LevelSelection.Choose(level, difficulty);

        // O pódio da transição é a tabela da sem fim: antes de uma fase, ele
        // mostraria o placar de outra coisa.
        if (level != null && level.endless)
            BeginTransition();
        else
            LoadGame();
    }

    /// <summary>Troféu de uma linha da seleção: abre o placar daquela fase.</summary>
    public void ShowLeaderboard(LevelDefinition level, Difficulty difficulty)
    {
        if (loading || leaderboardPanel == null)
            return;

        Show(leaderboard: true);
        leaderboardPanel.Open(level, difficulty);
    }

    /// <summary>Botão "Naves".</summary>
    public void OnShipsButton() => Show(ships: true);

    /// <summary>Botão "Voltar" da aba de naves.</summary>
    public void OnCloseShipsButton() => Show();

    /// <summary>Botão "Conquistas".</summary>
    public void OnAchievementsButton() => Show(achievements: true);

    /// <summary>Botão "Voltar" do painel de conquistas.</summary>
    public void OnCloseAchievementsButton() => Show();

    /// <summary>Botão "Voltar" da tela de seleção de fase.</summary>
    public void OnCloseLevelSelectButton() => Show();

    /// <summary>Toque na tela de transição: pula a espera.</summary>
    public void OnSkipTransitionButton() => LoadGame();

    /// <summary>Botão "Sair".</summary>
    public void OnQuitButton() => Application.Quit();

    void BeginTransition()
    {
        // Sem painel de transição montado, vai direto para a partida.
        if (transitionPanel == null)
        {
            LoadGame();
            return;
        }

        Show(transition: true);
        StartCoroutine(TransitionThenPlay());
    }

    IEnumerator TransitionThenPlay()
    {
        // Realtime: se alguma coisa tiver parado o tempo, a transição não trava.
        yield return new WaitForSecondsRealtime(transitionSeconds);
        LoadGame();
    }

    void LoadGame()
    {
        if (loading)
            return;

        loading = true;
        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>
    /// Um painel de cada vez, e os botões só quando nenhum está aberto. Sem
    /// argumento nenhum, volta ao menu.
    /// </summary>
    void Show(bool levelSelect = false, bool leaderboard = false, bool ships = false,
              bool transition = false, bool achievements = false)
    {
        if (achievementsPanel != null)
            achievementsPanel.SetActive(achievements);

        if (levelSelectPanel != null)
            levelSelectPanel.SetActive(levelSelect);

        if (leaderboardPanel != null)
            leaderboardPanel.gameObject.SetActive(leaderboard);

        if (shipsPanel != null)
            shipsPanel.SetActive(ships);

        if (transitionPanel != null)
            transitionPanel.SetActive(transition);

        bool home = !levelSelect && !leaderboard && !ships && !transition && !achievements;

        if (buttonsRoot != null)
            buttonsRoot.SetActive(home);

        if (title != null)
            title.SetActive(home);
    }
}
