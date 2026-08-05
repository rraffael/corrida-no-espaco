using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Menu de pausa da cena do jogo. O botão de hambúrguer no canto superior
/// direito abre o painel e congela o jogo; de dentro dele dá para voltar a
/// jogar ou sair para o menu inicial.
///
/// A pausa é o <c>Time.timeScale</c> em zero — é o que o resto do jogo lê para
/// saber que está parado, sem precisar conhecer esta classe.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Tooltip("Painel que cobre a tela com o menu aberto. Fica desligado enquanto se joga.")]
    [SerializeField] GameObject panel;

    [Tooltip("Botão de hambúrguer. Some enquanto o menu está aberto.")]
    [SerializeField] GameObject openButton;

    [Tooltip("Cena para onde o botão 'Sair' leva. Precisa estar na lista de build.")]
    [SerializeField] string menuSceneName = "Menu";

    public bool IsOpen => panel != null && panel.activeSelf;

    void Awake()
    {
        // Estado inicial pelo código, e não pelo que ficou salvo na cena: quem
        // deixar o painel ligado no Editor para mexer no layout não publica o
        // jogo começando pausado.
        Apply(false);
    }

    void OnDisable()
    {
        // Sair da cena com o jogo pausado deixaria o timeScale em zero para
        // quem vier depois, e a cena seguinte nasceria congelada.
        Time.timeScale = 1f;
    }

    public void Open() => Apply(true);

    public void Close() => Apply(false);

    public void Toggle() => Apply(!IsOpen);

    public void QuitToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName);
    }

    void Apply(bool open)
    {
        if (panel != null)
            panel.SetActive(open);

        if (openButton != null)
            openButton.SetActive(!open);

        Time.timeScale = open ? 0f : 1f;
    }
}
