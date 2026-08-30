using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// O canto fixo do **poder da nave**: qual é, quantos usos sobram e quanto falta
/// da recarga.
///
/// **Por que é um canto fixo e não um botão.** A ativação é toque duplo em
/// qualquer lugar da tela, e as duas coisas são separadas de propósito: botão de
/// ativar obrigaria a tirar o polegar da faixa em que se está desviando, que é o
/// pior momento possível para pedir isso. Este canto só **informa**, e não
/// recebe toque nenhum — <see cref="CanvasGroup.blocksRaycasts"/> desligado,
/// senão ele viraria uma zona morta onde o toque duplo não funciona.
///
/// **Não se parece com modificador de fase, e isso é regra.** Modificador de
/// fase é item redondo na pista e vira um quadradinho com relógio no alto à
/// ESQUERDA; este aqui é um canto fixo no alto à DIREITA, que só **pisca quando
/// fica pronto**. Se os dois se anunciassem igual, o jogador não saberia o que
/// tem e o que pode ativar.
///
/// Nave sem poder equipado: o canto some inteiro. Não existe "slot vazio" na
/// tela — espaço ocupado por nada é espaço que ensina errado.
/// </summary>
[DefaultExecutionOrder(50)]
public class AbilityHud : MonoBehaviour
{
    [Tooltip("De quem este canto fala. Vazio: procura o poder da nave da cena.")]
    [SerializeField] ShipAbilities abilities;

    [Tooltip("O quadrado de trás, que escurece com a recarga correndo.")]
    [SerializeField] Image frame;

    [Tooltip("O ícone do poder equipado.")]
    [SerializeField] Image icon;

    [Tooltip("Usos que sobram na partida.")]
    [SerializeField] TextMeshProUGUI usesLabel;

    [Tooltip("Segundos que faltam da recarga. Some quando está pronto.")]
    [SerializeField] TextMeshProUGUI cooldownLabel;

    [Header("Aparência")]
    [Tooltip("Opacidade do canto com a recarga correndo, ou sem uso sobrando.")]
    [Range(0f, 1f)][SerializeField] float dimAlpha = 0.35f;

    [Tooltip("Quantas vezes por segundo o canto pisca ao ficar pronto.")]
    [Min(0f)][SerializeField] float readyBlinkHz = 2.5f;

    [Tooltip("Quanto tempo o piscar dura, em segundos, depois de ficar pronto.")]
    [Min(0f)][SerializeField] float readyBlinkSeconds = 1.2f;

    CanvasGroup group;

    // Ficar pronto é um acontecimento, e é ele que dispara o piscar. Guardar o
    // estado anterior é o que separa "acabou de ficar pronto" de "está pronto há
    // meio minuto" — o segundo não pode piscar, senão o canto vira um alarme.
    bool wasReady;
    float blinkUntil;

    int shownUses = int.MinValue;
    int shownTenths = int.MinValue;

    void Awake()
    {
        group = GetComponent<CanvasGroup>();
        if (group == null)
            group = gameObject.AddComponent<CanvasGroup>();

        // Informa, não recebe toque: o toque duplo tem de valer aqui também.
        group.blocksRaycasts = false;
        group.interactable = false;
    }

    void Start()
    {
        if (abilities == null)
            abilities = ShipAbilities.Instance;

        if (abilities == null || abilities.Equipped == null)
        {
            // Sem poder equipado o canto não tem o que dizer. Desligar o objeto
            // inteiro, e não só esvaziar os textos, para não sobrar moldura vazia.
            gameObject.SetActive(false);
            return;
        }

        var ability = abilities.Equipped;

        if (icon != null)
        {
            icon.sprite = ability.icon;
            icon.color = ability.color;
            // Poder sem arte ainda não é erro: a maioria vai passar um tempo sem
            // ícone, e o quadrado colorido já diz qual é.
            icon.enabled = ability.icon != null;
        }

        if (frame != null)
            frame.color = new Color(ability.color.r, ability.color.g, ability.color.b, 0.22f);

        wasReady = abilities.IsReady;
    }

    void Update()
    {
        bool ready = abilities.IsReady;
        if (ready && !wasReady)
            blinkUntil = Time.unscaledTime + readyBlinkSeconds;
        wasReady = ready;

        group.alpha = ready ? ReadyAlpha() : dimAlpha;

        UpdateUses();
        UpdateCooldown(ready);
    }

    /// <summary>
    /// Cheio depois que o piscar passa. Em tempo **não escalado** de propósito:
    /// com o jogo pausado o canto continua respirando, em vez de congelar num
    /// meio-tom que parece defeito.
    /// </summary>
    float ReadyAlpha()
    {
        if (Time.unscaledTime >= blinkUntil || readyBlinkHz <= 0f)
            return 1f;

        float wave = Mathf.Sin(Time.unscaledTime * readyBlinkHz * Mathf.PI * 2f);
        return Mathf.Lerp(dimAlpha, 1f, 0.5f + 0.5f * wave);
    }

    /// <summary>
    /// O contador de usos, que **só existe para poder que tem usos contados**.
    /// Poder ilimitado — os Tiros teleguiados da Nay — mostraria "0" a corrida
    /// inteira, que é a pior mentira possível: o número certo ali é nenhum, e
    /// quem informa é a recarga.
    /// </summary>
    void UpdateUses()
    {
        if (usesLabel == null)
            return;

        if (!abilities.IsLimited)
        {
            if (usesLabel.enabled)
                usesLabel.enabled = false;
            return;
        }

        if (!usesLabel.enabled)
        {
            usesLabel.enabled = true;
            shownUses = int.MinValue;
        }

        int uses = abilities.UsesLeft;
        if (uses == shownUses)
            return;

        shownUses = uses;
        usesLabel.text = uses.ToString();
    }

    void UpdateCooldown(bool ready)
    {
        if (cooldownLabel == null)
            return;

        // Pronto, ou acabaram os usos: número de recarga só atrapalharia. No
        // segundo caso o que informa é o contador de usos, já em zero.
        //
        // Pelo HasUses e não pelo UsesLeft: poder ilimitado tem UsesLeft em zero
        // a corrida inteira, e testar o número escondia justamente a recarga da
        // Nay, que é a única coisa que o canto dela tem para dizer.
        if (ready || !abilities.HasUses)
        {
            if (cooldownLabel.enabled)
            {
                cooldownLabel.enabled = false;
                shownTenths = int.MinValue;
            }
            return;
        }

        if (!cooldownLabel.enabled)
            cooldownLabel.enabled = true;

        // Em décimos: a string só é remontada dez vezes por segundo, como no
        // resto do HUD, e não a cada frame.
        int tenths = Mathf.CeilToInt(abilities.CooldownLeft * 10f);
        if (tenths == shownTenths)
            return;

        shownTenths = tenths;
        cooldownLabel.text = $"{tenths / 10f:0.0}";
    }
}
