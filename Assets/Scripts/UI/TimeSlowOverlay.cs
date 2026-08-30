using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// O véu azulado que cobre a tela enquanto o tempo está lento.
///
/// **Existe porque o efeito era invisível** *(pedido do Raffael, 30/08/2026)*.
/// Câmera lenta se sente muito melhor do que se vê: sem um aviso na tela, o
/// jogador percebe que "alguma coisa ficou estranha" antes de entender que foi
/// ele quem pegou um poder. O véu dá nome à sensação no instante em que ela
/// começa.
///
/// **Azul, e de leve.** Azul porque é a cor que o olho lê como frio e parado —
/// gelo, congelamento —, que é exatamente a leitura que se quer; de leve porque
/// isto cobre a pista inteira, e um véu que atrapalhe enxergar obstáculo
/// transforma um poder neutro num castigo.
///
/// **Lê do <see cref="GameTime"/>, e não do modificador.** Assim qualquer coisa
/// que venha a segurar o tempo — um poder de nave, um chefe, uma fase — já
/// nasce com o véu, sem uma linha nova.
/// </summary>
[DefaultExecutionOrder(60)]
public class TimeSlowOverlay : MonoBehaviour
{
    [Tooltip("O retângulo que cobre a tela. Vazio: procura o Image deste objeto.")]
    [SerializeField] Image veil;

    [Tooltip("Cor do véu no auge. O alfa daqui é o teto — nunca fica mais opaco que isso.")]
    [SerializeField] Color tint = new Color(0.35f, 0.65f, 1f, 0.16f);

    [Tooltip("Segundos para o véu aparecer e sumir. Curto, mas não instantâneo: " +
             "piscar de uma vez parece defeito de renderização.")]
    [SerializeField, Min(0.01f)] float fadeSeconds = 0.25f;

    float shown;

    void Awake()
    {
        if (veil == null)
            veil = GetComponent<Image>();

        if (veil == null)
        {
            enabled = false;
            return;
        }

        veil.raycastTarget = false;
        Apply(0f);
    }

    void Update()
    {
        // Quanto mais lento o tempo, mais forte o véu: a 50% ele fica na metade
        // do teto. Assim um efeito futuro mais forte se anuncia mais forte, sem
        // ninguém precisar escolher um alfa por poder.
        float wanted = GameTime.IsSlowed ? 1f - GameTime.SlowFactor : 0f;

        // Em tempo NÃO escalado, e é o detalhe que faz a diferença: o véu entra
        // com o relógio já desacelerado, e se usasse o tempo do jogo ele
        // apareceria em câmera lenta — chegaria depois do efeito que anuncia.
        shown = Mathf.MoveTowards(shown, wanted, Time.unscaledDeltaTime / fadeSeconds);
        Apply(shown);
    }

    void Apply(float strength)
    {
        var color = tint;
        color.a = tint.a * strength;
        veil.color = color;

        // Desligar o Image quando não há véu: um retângulo de tela cheia com alfa
        // zero ainda custa uma passada de desenho, e isto roda a corrida inteira.
        if (veil.enabled != strength > 0.001f)
            veil.enabled = strength > 0.001f;
    }
}
