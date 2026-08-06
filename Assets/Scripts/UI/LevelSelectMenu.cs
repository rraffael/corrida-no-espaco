using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// A tela que o "Jogar" abre: todas as fases que existem, um seletor de
/// dificuldade e um cadeado no que ainda não foi destravado.
///
/// A lista sai do <see cref="LevelCatalog"/> em runtime, e não da cena: fase
/// nova no catálogo aparece aqui sozinha. Quem decide o que está trancado é o
/// <see cref="LevelProgress"/> — esta classe só desenha.
/// </summary>
public class LevelSelectMenu : MonoBehaviour
{
    [SerializeField] Menu menu;
    [SerializeField] TMP_Dropdown difficultyDropdown;

    [Tooltip("Onde as linhas nascem. Tem o VerticalLayoutGroup que as empilha.")]
    [SerializeField] RectTransform rowsRoot;

    [Tooltip("Linha modelo, desligada na cena. É clonada uma vez por fase.")]
    [SerializeField] LevelSelectRow rowTemplate;

    [Tooltip("Linha de rodapé com o andamento da progressão.")]
    [SerializeField] TMP_Text progressLabel;

    readonly List<LevelSelectRow> rows = new List<LevelSelectRow>();
    readonly List<LevelDefinition> levels = new List<LevelDefinition>();

    LevelCatalog catalog;
    Difficulty difficulty;
    bool built;

    void OnEnable()
    {
        catalog = LevelCatalog.Load();
        if (catalog == null)
            return;

        // A escolha anterior sobrevive a fechar e reabrir a tela.
        difficulty = LevelSelection.Difficulty;

        Build();
        Refresh();
    }

    /// <summary>Uma vez por sessão: a lista de fases não muda enquanto o jogo roda.</summary>
    void Build()
    {
        if (built)
            return;

        built = true;

        BuildDropdown();

        if (rowTemplate == null || rowsRoot == null)
        {
            Debug.LogError("[Seleção] Falta a linha modelo ou o contêiner das linhas.", this);
            return;
        }

        if (catalog.levels != null)
        {
            foreach (var level in catalog.levels)
                CreateRow(level);
        }

        // A sem fim é a última da lista porque é o último degrau da corrente.
        if (catalog.endlessLevel != null)
            CreateRow(catalog.endlessLevel);

        rowTemplate.gameObject.SetActive(false);
    }

    void CreateRow(LevelDefinition level)
    {
        if (level == null)
            return;

        // Sem manter a posição de mundo: o VerticalLayoutGroup é quem coloca a
        // linha no lugar, e herdar a escala do canvas por engano deformaria tudo.
        var row = Instantiate(rowTemplate, rowsRoot, false);
        row.gameObject.SetActive(true);
        row.name = "Fase - " + level.displayName;

        rows.Add(row);
        levels.Add(level);
    }

    void BuildDropdown()
    {
        if (difficultyDropdown == null || catalog.difficulties == null)
            return;

        var options = new List<TMP_Dropdown.OptionData>(catalog.difficulties.Length);
        foreach (var entry in catalog.difficulties)
            options.Add(new TMP_Dropdown.OptionData(entry.displayName));

        difficultyDropdown.ClearOptions();
        difficultyDropdown.AddOptions(options);
        difficultyDropdown.onValueChanged.AddListener(OnDifficultyChanged);

        // Sem notificar: ainda não há linha nenhuma para o Refresh redesenhar.
        difficultyDropdown.SetValueWithoutNotify(IndexOf(difficulty));
        difficultyDropdown.RefreshShownValue();
    }

    int IndexOf(Difficulty value)
    {
        if (catalog.difficulties == null)
            return 0;

        for (int i = 0; i < catalog.difficulties.Length; i++)
        {
            if (catalog.difficulties[i].difficulty == value)
                return i;
        }

        return 0;
    }

    void OnDifficultyChanged(int index)
    {
        if (catalog == null || catalog.difficulties == null ||
            index < 0 || index >= catalog.difficulties.Length)
            return;

        difficulty = catalog.difficulties[index].difficulty;
        Refresh();
    }

    /// <summary>Redesenha as linhas com os cadeados da dificuldade escolhida.</summary>
    void Refresh()
    {
        for (int i = 0; i < rows.Count; i++)
        {
            var level = levels[i];
            bool unlocked = LevelProgress.IsUnlocked(catalog, level, difficulty);
            rows[i].Bind(level, unlocked, DetailFor(level, unlocked), Choose);
        }

        if (progressLabel == null)
            return;

        int total = LevelProgress.TotalStages(catalog);
        int cleared = Mathf.Min(LevelProgress.StagesCleared, total);
        progressLabel.text = $"{cleared} de {total} vencidas — a próxima só abre quando a anterior cair.";
    }

    string DetailFor(LevelDefinition level, bool unlocked)
    {
        if (level == null)
            return string.Empty;

        // A sem fim nunca cai aqui: ela está aberta desde o começo.
        if (!unlocked)
        {
            int index = LevelProgress.StageIndex(catalog, level, difficulty);
            return "Vença antes: " + LevelProgress.DescribeStage(catalog, index - 1);
        }

        if (level.endless)
            return "Sem dobra — vale a distância percorrida";

        var settings = catalog.SettingsFor(difficulty);
        float warp = level.warpSpeed + settings.warpSpeedBonus;
        return $"Dobra em {warp:0.#} u/s, segurando {level.warpChargeSeconds:0.#}s";
    }

    void Choose(LevelDefinition level)
    {
        if (menu != null)
            menu.StartRace(level, difficulty);
    }
}
