using TMPro;
using UnityEngine;

/// <summary>
/// Mostra a vida da nave no rodapé. Sem vida à vista não dá para saber que a
/// derrota está chegando — e derrota é vida em zero.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class HealthHud : MonoBehaviour
{
    [Tooltip("Vida de quem. Vazio: pega a da nave (ShipStats da cena).")]
    [SerializeField] Health target;

    [SerializeField] Color healthyColor = new Color(0.7f, 1f, 0.8f, 0.9f);
    [SerializeField] Color hurtColor = new Color(1f, 0.5f, 0.45f, 0.95f);

    [Tooltip("Abaixo desta fração de vida o número muda de cor.")]
    [SerializeField, Range(0f, 1f)] float hurtBelow = 0.35f;

    TextMeshProUGUI label;

    void Awake()
    {
        label = GetComponent<TextMeshProUGUI>();
    }

    void Start()
    {
        if (target == null && ShipStats.Instance != null)
            target = ShipStats.Instance.Health;

        if (target == null)
        {
            Debug.LogWarning("[HUD] Nenhuma vida para mostrar — a nave tem ShipStats?", this);
            enabled = false;
            return;
        }

        target.Changed += Show;
        Show(target.Current);
    }

    void OnDestroy()
    {
        if (target != null)
            target.Changed -= Show;
    }

    // Por evento, e não por frame: a vida muda pouquíssimas vezes numa partida.
    void Show(float current)
    {
        label.text = Mathf.CeilToInt(current).ToString();
        label.color = target.Fraction <= hurtBelow ? hurtColor : healthyColor;
    }
}
