using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Menu inicial: jogar, ver as naves, ver recordes ou sair.
///
/// "Jogar" não carrega a cena na hora — abre a seleção de fase e, escolhida a
/// fase, passa pela transição que mostra os três primeiros da tabela. Os nomes
/// dos métodos públicos estão ligados aos botões dentro da cena: renomear aqui
/// quebra a fiação de lá.
/// </summary>
public class Menu : MonoBehaviour
{
    [Header("Painéis")]
    [SerializeField] GameObject recordsPanel;
    [SerializeField] GameObject levelSelectPanel;
    [SerializeField] GameObject shipsPanel;
    [SerializeField] GameObject transitionPanel;

    [Tooltip("Botões do menu, escondidos enquanto um painel está aberto.")]
    [SerializeField] GameObject buttonsRoot;

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
        BeginTransition();
    }

    /// <summary>Botão "Naves".</summary>
    public void OnShipsButton() => Show(ships: true);

    /// <summary>Botão "Voltar" da aba de naves.</summary>
    public void OnCloseShipsButton() => Show();

    /// <summary>Botão "Recordes".</summary>
    public void OnRecordsButton() => Show(records: true);

    /// <summary>Botão "Voltar" do painel de recordes.</summary>
    public void OnCloseRecordsButton() => Show();

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
    void Show(bool records = false, bool levelSelect = false, bool ships = false,
              bool transition = false)
    {
        if (recordsPanel != null)
            recordsPanel.SetActive(records);

        if (levelSelectPanel != null)
            levelSelectPanel.SetActive(levelSelect);

        if (shipsPanel != null)
            shipsPanel.SetActive(ships);

        if (transitionPanel != null)
            transitionPanel.SetActive(transition);

        if (buttonsRoot != null)
            buttonsRoot.SetActive(!records && !levelSelect && !ships && !transition);
    }
}
