using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// O placar de uma fase, aberto pelo troféu da tela de seleção. Mostra o topo e
/// onde o jogador está, e deixa jogar a fase dali mesmo — quem foi olhar o
/// placar quase sempre sai dele querendo bater um tempo.
/// </summary>
public class LeaderboardPanel : MonoBehaviour
{
    [SerializeField] Menu menu;
    [SerializeField] LeaderboardView view;
    [SerializeField] TMP_Text titleLabel;

    [Tooltip("Desligado quando a fase ainda está trancada.")]
    [SerializeField] Button playButton;

    LevelDefinition level;
    Difficulty difficulty;

    /// <summary>Chamado pelo <see cref="Menu"/> depois de ligar o painel.</summary>
    public void Open(LevelDefinition chosen, Difficulty chosenDifficulty)
    {
        level = chosen;
        difficulty = chosenDifficulty;

        var catalog = LevelCatalog.Load();

        if (titleLabel != null)
        {
            string difficultyName = catalog != null ? catalog.SettingsFor(difficulty).displayName : difficulty.ToString();
            titleLabel.text = level != null ? $"{level.displayName} ({difficultyName})" : "Placar";
        }

        if (playButton != null)
            playButton.interactable = LevelProgress.IsUnlocked(catalog, level, difficulty);

        if (view != null)
            view.Show(ScoreBoard.BoardId(level, difficulty));
    }

    /// <summary>Botão "Jogar" do painel.</summary>
    public void OnPlayButton()
    {
        if (menu != null && level != null)
            menu.StartRace(level, difficulty);
    }

    /// <summary>Botão "Voltar": volta para a lista de fases, e não para o menu.</summary>
    public void OnBackButton()
    {
        if (menu != null)
            menu.OnPlayButton();
    }
}
