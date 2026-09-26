using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// O painel de conquistas do menu: os números de carreira no alto, e uma linha
/// por conquista do catálogo. Conquista nova no catálogo aparece aqui sozinha —
/// a mesma arquitetura das abas de fase e de naves.
///
/// **Os números de carreira aparecem desde já** *(26/09/2026)*, mesmo sem
/// conquista nenhuma: é o jeito de conferir no aparelho que as estatísticas
/// estão contando certo antes de alguma conquista depender delas.
/// </summary>
public class AchievementsMenu : MonoBehaviour
{
    [SerializeField] RectTransform rowsRoot;
    [SerializeField] AchievementRow rowTemplate;
    [SerializeField] TMP_Text careerLabel;
    [SerializeField] TMP_Text emptyLabel;

    readonly List<AchievementRow> rows = new List<AchievementRow>();
    readonly List<Achievement> shown = new List<Achievement>();
    bool built;

    void OnEnable()
    {
        Build();
        Refresh();
    }

    void Build()
    {
        if (built)
            return;

        built = true;

        if (rowTemplate == null || rowsRoot == null)
        {
            Debug.LogError("[Conquistas] Falta a linha modelo ou o contêiner das linhas.", this);
            return;
        }

        rowTemplate.gameObject.SetActive(false);

        foreach (var achievement in Achievements.All)
        {
            if (achievement == null)
                continue;

            shown.Add(achievement);

            var row = Instantiate(rowTemplate, rowsRoot);
            row.name = $"Conquista {achievement.displayName}";
            row.gameObject.SetActive(true);
            rows.Add(row);
        }
    }

    void Refresh()
    {
        for (int i = 0; i < rows.Count; i++)
            rows[i].Bind(shown[i], Refresh);

        if (emptyLabel != null)
            emptyLabel.gameObject.SetActive(shown.Count == 0);

        if (careerLabel == null)
            return;

        var career = CareerStats.Data;
        careerLabel.text =
            $"Corridas {career.races}   Vitórias {career.wins}   Abates {career.kills}   " +
            $"Batidas {career.crashes}   Evoluções {career.shipLevelUps}\n" +
            $"Distância {ScoreBoard.FormatDistance(career.distance)}   " +
            $"Melhor sem fim {ScoreBoard.FormatDistance(career.bestEndlessDistance)}   " +
            $"{Wallet.CurrencyName} {Wallet.Balance}";
    }
}
