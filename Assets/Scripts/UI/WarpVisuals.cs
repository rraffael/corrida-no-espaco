using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A dobra na periferia da tela: uma **moldura que se fecha** conforme a carga
/// sobe, e uma **onda** nos dois momentos que importam — perder a carga e
/// completar a dobra.
///
/// <para>
/// **Por que ela existe** *(pedido do Raffael, 30/08/2026)*: a dobra era o único
/// estado do jogo que só existia como texto, e o texto mora no rodapé. Na
/// velocidade em que a dobra acontece — que é, por definição, a maior da corrida
/// — tirar o olho da pista para ler "DOBRA EM 1,4" é entre inviável e
/// impossível. O jogador estava jogando às cegas justamente no trecho decisivo.
/// </para>
///
/// **A moldura carrega o número, não só o estado.** Ela nasce nos quatro cantos
/// e cresce pelas bordas até fechar o quadro: quanto da volta já está desenhado
/// **é** quanto falta para a dobra. Assim o rodapé vira redundância em vez de
/// necessidade, que era o pedido.
///
/// **Na periferia, e nunca no meio.** Tudo o que esta classe desenha vive nas
/// bordas ou é um clarão de um instante. O centro da tela é onde se desvia, e é
/// o último lugar do mundo onde se pode pôr informação nova a 19 unidades por
/// segundo.
///
/// **Perder é mais alto que ganhar.** A onda de quem cai da velocidade é âmbar e
/// recolhe para dentro; a de quem completa é um clarão claro que estoura para
/// fora. Direções contrárias, para o jogador nunca confundir os dois — e a de
/// perder tem de ser a mais visível das duas, porque perder em silêncio é o pior
/// caso possível.
/// </summary>
[DefaultExecutionOrder(55)]
public class WarpVisuals : MonoBehaviour
{
    [Tooltip("Quem manda na dobra. Vazio: procura o RaceDirector da cena.")]
    [SerializeField] RaceDirector director;

    [Tooltip("Os oito pedaços da moldura, em pares por canto: cada canto tem um braço deitado e " +
             "um em pé, e eles crescem em direção ao meio da borda. No cheio, os oito se " +
             "encontram e fecham o quadro.")]
    [SerializeField] RectTransform[] arms;

    [Tooltip("Os Image dos mesmos oito pedaços, para pintar todos de uma vez.")]
    [SerializeField] Image[] armImages;

    [Tooltip("O retângulo de tela cheia que dá o clarão das ondas.")]
    [SerializeField] Image flash;

    [Header("Moldura")]
    [Tooltip("Cor da moldura enquanto a dobra carrega.")]
    [SerializeField] Color chargingColor = new Color(0.55f, 0.9f, 1f, 0.9f);

    [Tooltip("Cor da moldura enquanto a carga está escoando — a nave caiu da velocidade.")]
    [SerializeField] Color losingColor = new Color(1f, 0.68f, 0.3f, 0.9f);

    [Tooltip("Segundos para a moldura acompanhar a carga. Pequeno: ela precisa parecer colada " +
             "no que está acontecendo, não um mostrador com atraso.")]
    [SerializeField, Min(0.01f)] float response = 0.12f;

    [Header("Ondas")]
    [Tooltip("Cor do clarão de quem PERDEU a carga. Âmbar: é aviso.")]
    [SerializeField] Color lostFlashColor = new Color(1f, 0.6f, 0.2f, 0.30f);

    [Tooltip("Cor do clarão de quem COMPLETOU a dobra.")]
    [SerializeField] Color wonFlashColor = new Color(0.85f, 0.95f, 1f, 0.75f);

    [Tooltip("Segundos que um clarão leva para sumir.")]
    [SerializeField, Min(0.05f)] float flashSeconds = 0.45f;

    [Tooltip("Carga mínima, de 0 a 1, para valer um aviso ao perder. Abaixo disto não havia o " +
             "que perder, e o clarão só piscaria à toa.")]
    [SerializeField, Range(0f, 1f)] float lostFlashThreshold = 0.12f;

    [Tooltip("Espera mínima entre dois avisos de perda. Existe porque a nave pode ficar " +
             "beirando a velocidade de dobra, entrando e saindo várias vezes por segundo — sem " +
             "isto, o aviso viraria um estroboscópio.")]
    [SerializeField, Min(0f)] float lostFlashCooldown = 1.5f;

    float shownFraction;
    bool wasCharging;
    float lastLostFlash = float.NegativeInfinity;

    float flashStrength;
    Color flashColor;

    void Start()
    {
        if (director == null)
            director = RaceDirector.Instance;

        // Fase sem fim não tem dobra: a moldura ali seria um mostrador de uma
        // coisa que não existe.
        if (director == null || director.IsEndless)
        {
            gameObject.SetActive(false);
            return;
        }

        wasCharging = director.IsCharging;
        Apply(0f, chargingColor);
        SetFlash(0f);
    }

    void Update()
    {
        WatchTransitions();

        // A moldura mostra a carga enquanto a corrida roda. Acabada a corrida ela
        // recolhe: o painel de fim é quem fala a partir dali.
        float wanted = director.IsRunning ? director.WarpFraction : 0f;
        shownFraction = Mathf.MoveTowards(shownFraction, wanted, Time.unscaledDeltaTime / response);

        Apply(shownFraction, director.IsCharging ? chargingColor : losingColor);
        FadeFlash();
    }

    /// <summary>
    /// Os dois momentos que ganham onda. **Em tempo não escalado**, como tudo
    /// aqui: a dobra completa congela o jogo no mesmo quadro, e um clarão preso
    /// ao relógio do jogo nunca chegaria a aparecer.
    /// </summary>
    void WatchTransitions()
    {
        bool charging = director.IsCharging;

        // Completou: a corrida acaba no mesmo instante em que a carga enche, e é
        // por isso que o teste é a carga cheia e não o IsRunning — quando este
        // Update roda, a corrida já parou.
        if (director.WarpFraction >= 1f)
        {
            if (wasCharging)
                Flash(wonFlashColor);

            wasCharging = false;
            return;
        }

        if (wasCharging && !charging &&
            director.WarpFraction >= lostFlashThreshold &&
            Time.unscaledTime - lastLostFlash >= lostFlashCooldown)
        {
            lastLostFlash = Time.unscaledTime;
            Flash(lostFlashColor);
        }

        wasCharging = charging;
    }

    /// <summary>
    /// Desenha a moldura. Cada braço ocupa uma fatia da borda **a partir do canto
    /// dele**, e a fatia vai até metade da borda — com os dois braços de cada
    /// borda no cheio, eles se encontram no meio e o quadro fecha.
    ///
    /// Em âncoras, e não em largura de pixel: assim a moldura vale em qualquer
    /// tela sem ninguém calcular o tamanho do Canvas.
    /// </summary>
    void Apply(float fraction, Color color)
    {
        if (arms == null)
            return;

        float half = Mathf.Clamp01(fraction) * 0.5f;

        for (int i = 0; i < arms.Length; i++)
        {
            var arm = arms[i];
            if (arm == null)
                continue;

            var min = arm.anchorMin;
            var max = arm.anchorMax;

            // Deitado ou em pé sai do sizeDelta, e **não das âncoras**: no primeiro
            // quadro os dois braços de um canto têm âncoras idênticas, e olhar
            // para elas faria o braço em pé se achar deitado e crescer para o lado
            // errado. O sizeDelta é o que a montagem fixou e nunca muda — o
            // deitado tem só altura, o em pé só largura.
            bool horizontal = arm.sizeDelta.x <= 0f;

            if (horizontal)
            {
                bool fromLeft = arm.pivot.x < 0.5f;
                min.x = fromLeft ? 0f : 1f - half;
                max.x = fromLeft ? half : 1f;
            }
            else
            {
                bool fromBottom = arm.pivot.y < 0.5f;
                min.y = fromBottom ? 0f : 1f - half;
                max.y = fromBottom ? half : 1f;
            }

            arm.anchorMin = min;
            arm.anchorMax = max;
        }

        if (armImages == null)
            return;

        // Some de vez quando não há carga: moldura apagada mas presente ainda
        // custa oito passadas de desenho por quadro, a corrida inteira.
        bool visible = fraction > 0.001f;

        for (int i = 0; i < armImages.Length; i++)
        {
            var image = armImages[i];
            if (image == null)
                continue;

            if (image.enabled != visible)
                image.enabled = visible;

            if (visible)
                image.color = color;
        }
    }

    void Flash(Color color)
    {
        flashColor = color;
        flashStrength = 1f;
        SetFlash(1f);
    }

    void FadeFlash()
    {
        if (flashStrength <= 0f)
            return;

        flashStrength = Mathf.MoveTowards(flashStrength, 0f, Time.unscaledDeltaTime / flashSeconds);
        SetFlash(flashStrength);
    }

    void SetFlash(float strength)
    {
        if (flash == null)
            return;

        bool visible = strength > 0.001f;
        if (flash.enabled != visible)
            flash.enabled = visible;

        if (!visible)
            return;

        var color = flashColor;
        color.a *= strength;
        flash.color = color;
    }
}
