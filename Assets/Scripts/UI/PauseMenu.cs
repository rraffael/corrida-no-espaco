using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Menu de pausa da cena do jogo. O botão de hambúrguer no canto superior
/// direito abre o painel e congela o jogo; de dentro dele dá para voltar a
/// jogar ou sair para o menu inicial.
///
/// A pausa passa pelo <see cref="GameTime"/>, que é quem manda no
/// <c>Time.timeScale</c> — e o resto do jogo lê de lá para saber que está
/// parado, sem precisar conhecer esta classe.
///
/// **Não escreve no timeScale direto, e é de propósito.** Desde que existe
/// modificador de tempo lento, "sair da pausa" não é mais "voltar a 1": pode
/// haver um efeito segurando o tempo em 0,5, e devolver 1 aqui o apagaria — o
/// jogador veria o poder dele evaporar por ter aberto o menu.
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
        // Sair da cena com o jogo pausado deixaria o tempo em zero para quem vier
        // depois, e a cena seguinte nasceria congelada.
        GameTime.ResetAll();
    }

    public void Open() => Apply(true);

    public void Close() => Apply(false);

    public void Toggle() => Apply(!IsOpen);

    public void QuitToMenu()
    {
        GameTime.ResetAll();
        SceneManager.LoadScene(menuSceneName);
    }

    void Apply(bool open)
    {
        if (panel != null)
            panel.SetActive(open);

        if (openButton != null)
            openButton.SetActive(!open);

        GameTime.SetPaused(open);
    }
}
