using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Uma linha da tela de seleção: nome da fase, uma linha de detalhe e o cadeado.
///
/// O <see cref="LevelSelectMenu"/> clona esta linha uma vez por fase do catálogo,
/// em vez de a montagem criar uma por fase na cena. Assim, fase nova no catálogo
/// aparece no menu sem ninguém rodar ferramenta de novo.
/// </summary>
public class LevelSelectRow : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] Image background;
    [SerializeField] TMP_Text nameLabel;
    [SerializeField] TMP_Text detailLabel;

    [Tooltip("O cadeado. Ligado só quando a fase está trancada.")]
    [SerializeField] GameObject padlock;

    [Header("Cores")]
    [SerializeField] Color unlockedColor = new Color(0.16f, 0.42f, 0.75f, 0.95f);
    [SerializeField] Color lockedColor = new Color(0.13f, 0.15f, 0.22f, 0.95f);
    [SerializeField] Color unlockedTextColor = new Color(0.92f, 0.96f, 1f, 1f);
    [SerializeField] Color lockedTextColor = new Color(0.55f, 0.60f, 0.72f, 1f);

    /// <summary>
    /// Põe a fase na linha. <paramref name="detail"/> é o que a linha de baixo
    /// diz: a regra da dobra quando dá para jogar, o que falta vencer quando não.
    /// </summary>
    public void Bind(LevelDefinition level, bool unlocked, string detail, Action<LevelDefinition> onChosen)
    {
        if (nameLabel != null)
        {
            nameLabel.text = level != null ? level.displayName : "—";
            nameLabel.color = unlocked ? unlockedTextColor : lockedTextColor;
        }

        if (detailLabel != null)
            detailLabel.text = detail;

        if (padlock != null)
            padlock.SetActive(!unlocked);

        if (background != null)
            background.color = unlocked ? unlockedColor : lockedColor;

        if (button == null)
            return;

        // Religar do zero: a mesma linha é reaproveitada quando a dificuldade
        // muda, e um listener velho mandaria para a fase da dificuldade anterior.
        button.onClick.RemoveAllListeners();
        button.interactable = unlocked;

        if (unlocked && level != null && onChosen != null)
            button.onClick.AddListener(() => onChosen(level));
    }
}
