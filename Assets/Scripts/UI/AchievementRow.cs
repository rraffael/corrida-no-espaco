using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Uma linha do painel de conquistas: nome, o que pede, quanto já andou, e o
/// botão de resgatar quando completa. Clonada uma vez por conquista do catálogo.
/// </summary>
public class AchievementRow : MonoBehaviour
{
    [SerializeField] TMP_Text nameLabel;
    [SerializeField] TMP_Text detailLabel;
    [SerializeField] Button claimButton;
    [SerializeField] TMP_Text claimLabel;
    [SerializeField] Image background;

    const string Hidden = "????????";

    [Header("Cores")]
    [SerializeField] Color pendingColor = new Color(0.13f, 0.15f, 0.22f, 0.95f);
    [SerializeField] Color doneColor = new Color(0.14f, 0.32f, 0.22f, 0.95f);

    public void Bind(Achievement achievement, Action onChanged)
    {
        if (achievement == null)
            return;

        bool completed = Achievements.IsCompleted(achievement);
        bool claimed = Achievements.IsClaimed(achievement);
        bool revealed = Achievements.IsRevealed(achievement);

        if (nameLabel != null)
            nameLabel.text = revealed ? achievement.displayName : Hidden;

        if (detailLabel != null)
        {
            // Escondida mostra só a recompensa (decidido pelo Raffael): é o que dá
            // vontade de descobrir sem entregar o que é.
            if (!revealed)
            {
                detailLabel.text = achievement.reward > 0
                    ? $"{Hidden}\nRecompensa: {achievement.reward} {Wallet.CurrencyName}"
                    : Hidden;
            }
            else
            {
                float value = Mathf.Min(Achievements.Value(achievement), achievement.target);
                string progress = completed ? "completa" : $"{value:0} / {achievement.target:0}";
                detailLabel.text = $"{achievement.description}\n{progress}";
            }
        }

        if (background != null)
            background.color = completed ? doneColor : pendingColor;

        if (claimButton == null)
            return;

        claimButton.onClick.RemoveAllListeners();
        claimButton.gameObject.SetActive(completed);
        claimButton.interactable = !claimed;

        if (claimLabel != null)
        {
            claimLabel.text = claimed
                ? "Resgatada"
                : achievement.reward > 0 ? $"Resgatar {achievement.reward} {Wallet.CurrencyName}" : "Resgatar";
        }

        claimButton.onClick.AddListener(() =>
        {
            if (Achievements.Claim(achievement))
                onChanged?.Invoke();
        });
    }
}
