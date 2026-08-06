using TMPro;
using UnityEngine;

/// <summary>
/// A linha logo acima do velocímetro, que mostra coisa diferente conforme a fase:
///
/// - **Fase de progressão:** a contagem da dobra, e só quando há carga acumulada.
///   Enquanto a nave estiver longe da velocidade de dobra, o rodapé fica limpo.
/// - **Fase sem fim:** a **distância percorrida**, o tempo todo. Lá não há dobra,
///   então esta linha ficaria vazia a corrida inteira — e a distância é a
///   pontuação, que o jogador precisa ver subindo para decidir se arrisca mais.
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

    [Tooltip("Cor da distância na fase sem fim.")]
    [SerializeField] Color distanceColor = new Color(0.85f, 0.92f, 1f, 0.9f);

    TextMeshProUGUI label;
    int shownTenths = int.MinValue;
    int shownDistance = int.MinValue;

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
        if (director.IsEndless)
        {
            ShowDistance();
            return;
        }

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

    void ShowDistance()
    {
        if (!label.enabled)
        {
            label.enabled = true;
            label.color = distanceColor;
        }

        // Só remonta a string quando o quilômetro mostrado muda de verdade: em
        // velocidade alta isto ainda é várias vezes por segundo, mas não a cada
        // frame, e a fase sem fim é justamente a que fica pesada com o tempo.
        int shown = Mathf.RoundToInt(director.Distance * ScoreBoard.DisplayScale);
        if (shown == shownDistance)
            return;

        shownDistance = shown;
        label.text = ScoreBoard.FormatDistance(director.Distance);
    }
}
