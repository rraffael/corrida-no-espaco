using TMPro;
using UnityEngine;

/// <summary>
/// Mostra a velocidade da corrida no rodapé da tela. Hoje é instrumento de
/// teste — responde "está acelerando mesmo?" sem precisar de log — e depois
/// vira informação de jogo.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class SpeedHud : MonoBehaviour
{
    [Tooltip("De onde vem a velocidade. Vazio: procura o RaceSpeed da cena.")]
    [SerializeField] RaceSpeed speed;

    [Tooltip("Multiplica o número só para exibir. As unidades de mundo por segundo são " +
             "poucas para dar sensação de velocidade no painel; a conta do jogo não muda.")]
    [SerializeField] float displayScale = 10f;

    TextMeshProUGUI label;
    int shown = int.MinValue;

    void Awake()
    {
        label = GetComponent<TextMeshProUGUI>();
    }

    void Start()
    {
        if (speed == null)
            speed = RaceSpeed.Instance != null ? RaceSpeed.Instance : FindAnyObjectByType<RaceSpeed>();

        if (speed != null)
            return;

        Debug.LogWarning("[HUD] Nenhum RaceSpeed na cena. O painel de velocidade fica zerado.", this);
        label.text = "0";
        enabled = false;
    }

    void Update()
    {
        int value = Mathf.RoundToInt(speed.Current * displayScale);
        if (value == shown)
            return;

        // Só monta a string quando o número muda de verdade: enquanto a nave
        // está em cruzeiro, o HUD não gera lixo nenhum por frame.
        shown = value;
        label.text = value.ToString();
    }
}
