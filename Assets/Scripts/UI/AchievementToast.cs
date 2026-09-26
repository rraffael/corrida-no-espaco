using TMPro;
using UnityEngine;

/// <summary>
/// O cartão de **"Conquista completa"** no alto do menu *(pedido do Raffael,
/// 26/09/2026)*. Mostra uma de cada vez, da fila de
/// <see cref="Achievements.Announcements"/>, e some sozinho.
///
/// Vale para o que completou agora mesmo — evoluir uma nave — e para o que
/// completou numa corrida: esse espera na fila e aparece quando o jogador volta
/// ao menu.
///
/// **Só informa, e não recebe toque** — o resgate continua no painel de
/// conquistas. Um cartão clicável por cima do menu viraria zona morta para os
/// botões de baixo enquanto estivesse na tela.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class AchievementToast : MonoBehaviour
{
    [SerializeField] TMP_Text label;

    [Tooltip("Segundos de cartão cheio, sem contar a entrada e a saída.")]
    [SerializeField, Min(0.5f)] float holdSeconds = 2.5f;

    [SerializeField, Min(0.05f)] float fadeSeconds = 0.3f;

    CanvasGroup group;
    float shownAt = -1f;

    void Awake()
    {
        group = GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;
    }

    void Update()
    {
        // Em tempo real: o menu pode estar com o tempo parado, e o cartão não.
        float now = Time.unscaledTime;

        if (shownAt < 0f)
        {
            if (Achievements.Announcements.Count == 0)
                return;

            var achievement = Achievements.Announcements.Dequeue();
            if (achievement == null)
                return;

            if (label != null)
            {
                string reward = achievement.reward > 0
                    ? $" · {achievement.reward} {Wallet.CurrencyName} para resgatar"
                    : string.Empty;
                label.text = $"<b>Conquista completa!</b>\n{achievement.displayName}{reward}";
            }

            shownAt = now;
        }

        float elapsed = now - shownAt;
        float total = fadeSeconds * 2f + holdSeconds;

        if (elapsed >= total)
        {
            group.alpha = 0f;
            shownAt = -1f;
            return;
        }

        float fadeIn = Mathf.Clamp01(elapsed / fadeSeconds);
        float fadeOut = Mathf.Clamp01((total - elapsed) / fadeSeconds);
        group.alpha = Mathf.Min(fadeIn, fadeOut);
    }
}
