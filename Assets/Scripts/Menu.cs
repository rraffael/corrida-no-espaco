using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Menu inicial: jogar, ver recordes ou sair.
///
/// "Jogar" não carrega a cena na hora — passa antes pela transição, que mostra
/// os três primeiros da tabela. Os nomes dos métodos públicos estão ligados aos
/// botões dentro da cena: renomear aqui quebra a fiação de lá.
/// </summary>
public class Menu : MonoBehaviour
{
    [Header("Painéis")]
    [SerializeField] GameObject recordsPanel;
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
        ShowPanels(records: false, transition: false);
    }

    /// <summary>Botão "Jogar".</summary>
    public void OnPlayButton()
    {
        if (loading)
            return;

        // Sem painel de transição montado, o botão faz o que sempre fez.
        if (transitionPanel == null)
        {
            LoadGame();
            return;
        }

        ShowPanels(records: false, transition: true);
        StartCoroutine(TransitionThenPlay());
    }

    /// <summary>Botão "Recordes".</summary>
    public void OnRecordsButton() => ShowPanels(records: true, transition: false);

    /// <summary>Botão "Voltar" do painel de recordes.</summary>
    public void OnCloseRecordsButton() => ShowPanels(records: false, transition: false);

    /// <summary>Toque na tela de transição: pula a espera.</summary>
    public void OnSkipTransitionButton() => LoadGame();

    /// <summary>Botão "Sair".</summary>
    public void OnQuitButton() => Application.Quit();

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

    void ShowPanels(bool records, bool transition)
    {
        if (recordsPanel != null)
            recordsPanel.SetActive(records);

        if (transitionPanel != null)
            transitionPanel.SetActive(transition);

        if (buttonsRoot != null)
            buttonsRoot.SetActive(!records && !transition);
    }
}
