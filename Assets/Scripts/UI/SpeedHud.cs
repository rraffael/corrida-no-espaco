using TMPro;
using UnityEngine;

/// <summary>
/// Mostra a velocidade da corrida no rodapé da tela: o número atual e a
/// unidade, e nada mais.
///
/// <para>
/// **A meta de dobra saiu daqui em 01/09/2026** *(decisão do Raffael)*. O rodapé
/// mostrava <c>87 / 160 un/s</c> nas fases de progressão, e o segundo número
/// poluía justamente o canto que precisa ser lido de relance, na velocidade
/// máxima da corrida. A meta não sumiu do jogo: ela é dita na **seleção de
/// fase**, antes de começar, e o quanto falta para ela é o que a **moldura da
/// dobra** desenha em volta da tela enquanto a carga sobe — periferia, que é
/// onde mora informação de estado. Um número no rodapé era a terceira vez de
/// dizer a mesma coisa, e a pior das três.
/// </para>
///
/// <para>Com isso as duas espécies de fase mostram a mesma coisa: só a velocidade.</para>
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class SpeedHud : MonoBehaviour
{
    [Tooltip("De onde vem a velocidade. Vazio: procura o RaceSpeed da cena.")]
    [SerializeField] RaceSpeed speed;

    /// <summary>Sai pronto uma vez: a unidade não muda no meio da corrida.</summary>
    static readonly string UnitSuffix = " " + RaceSpeed.DisplayUnit;

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
        label.text = "0" + UnitSuffix;
        enabled = false;
    }

    void Update()
    {
        int value = RaceSpeed.Shown(speed.Current);
        if (value == shown)
            return;

        // Só monta a string quando o número muda de verdade: enquanto a nave
        // está em cruzeiro, o HUD não gera lixo nenhum por frame.
        shown = value;
        label.text = value + UnitSuffix;
    }
}
