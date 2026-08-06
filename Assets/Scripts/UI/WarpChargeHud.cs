using TMPro;
using UnityEngine;

/// <summary>
/// Contagem da dobra, logo acima do velocímetro. Só aparece quando há carga
/// acumulada — enquanto a nave estiver longe da velocidade de dobra, não há o
/// que mostrar e o rodapé fica limpo.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class WarpChargeHud : MonoBehaviour
{
    [Tooltip("Quem manda na corrida. Vazio: procura o RaceDirector da cena.")]
    [SerializeField] RaceDirector director;

    [Tooltip("Cor com a carga escoando — a nave caiu abaixo da velocidade de dobra.")]
    [SerializeField] Color losingColor = new Color(1f, 0.6f, 0.4f, 0.85f);

    [Tooltip("Cor com a dobra carregando.")]
    [SerializeField] Color chargingColor = new Color(0.6f, 1f, 1f, 1f);

    TextMeshProUGUI label;
    int shownTenths = int.MinValue;

    void Awake()
    {
        label = GetComponent<TextMeshProUGUI>();
        label.enabled = false;
    }

    void Start()
    {
        if (director == null)
            director = RaceDirector.Instance;

        if (director != null)
            return;

        Debug.LogWarning("[HUD] Nenhum RaceDirector na cena. A contagem da dobra não aparece.", this);
        enabled = false;
    }

    void Update()
    {
        bool visible = director.IsRunning && director.WarpCharge > 0f;
        if (label.enabled != visible)
        {
            label.enabled = visible;
            shownTenths = int.MinValue;
        }

        if (!visible)
            return;

        label.color = director.IsCharging ? chargingColor : losingColor;

        // Em décimos: a string só é remontada dez vezes por segundo, e não a cada
        // frame, no trecho em que o jogo mais precisa de fôlego.
        int tenths = Mathf.CeilToInt(director.WarpSecondsLeft * 10f);
        if (tenths == shownTenths)
            return;

        shownTenths = tenths;
        label.text = $"DOBRA EM {tenths / 10f:0.0}";
    }
}
