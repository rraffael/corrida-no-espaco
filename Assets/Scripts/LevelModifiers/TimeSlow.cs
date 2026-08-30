using UnityEngine;

/// <summary>
/// **Especial — Redução na velocidade do tempo.** O jogo inteiro passa a correr
/// em câmera lenta por alguns segundos.
///
/// **A implementação é uma linha, e essa é a defesa dela** *(sugestão do Claude
/// aceita em 30/08/2026)*. O pedido original era reduzir nove coisas, uma a uma:
/// velocidade do obstáculo, do fundo, do ganho de pontos, da aceleração, da troca
/// de faixa, da carga de dobra, da recarga dos poderes e do consumo dos
/// modificadores já ativos. Todas as nove são a mesma coisa por baixo — cada uma
/// é um número multiplicado por <c>Time.deltaTime</c> —, então mexer no relógio
/// as pega juntas, de graça e sem esquecer nenhuma.
///
/// Nove reduções separadas custariam nove campos, nove chances de errar o sinal,
/// e — o pior — a décima coisa que entrasse no jogo nasceria em velocidade cheia
/// e ninguém lembraria por quê. Pelo relógio, tudo que for escrito daqui para a
/// frente já nasce obedecendo.
///
/// **O que de propósito NÃO muda** *(pedido do Raffael)*: a velocidade da nave,
/// o número. Ela continua a mesma no HUD porque a nave não ficou mais lenta — o
/// tempo é que ficou. Só o que ela *percorre* por segundo real é que cai, e isso
/// sai sozinho do relógio.
///
/// **É bom ou ruim?** É Especial justamente por não ser nenhum dos dois: dá mais
/// tempo real para desviar e, na mesma medida, atrasa a velocidade, a dobra e a
/// pontuação. Quem está apertado se salva; quem está indo bem perde tempo.
///
/// ⚠️ **Ele desacelera o próprio relógio, e isso é consequência e não descuido.**
/// A duração dos modificadores é contada em tempo de jogo, e o pedido incluía
/// "reduzir o consumo dos modificadores já ativos" — este é um deles. Com fator
/// 0,5 e duração 3, ele vale 3 segundos de jogo e **6 de relógio de parede**. Se
/// no aparelho parecer comprido demais, o número a baixar é a duração, não o
/// fator: o fator é o que o jogador sente, a duração é quanto dura.
/// </summary>
[CreateAssetMenu(fileName = "Especial-TempoLento",
                 menuName = "Corrida no Espaço/Modificador de fase/Especial — tempo lento")]
public class TimeSlow : LevelModifier
{
    [Header("Efeito")]
    [Tooltip("Que fração da velocidade normal o tempo passa a correr. 0,5 = metade.\n\n" +
             "Duas lentidões ao mesmo tempo NÃO se multiplicam: vale a mais lenta. Ver GameTime " +
             "para por quê — multiplicar faria a terceira congelar a tela.")]
    [Range(0.05f, 1f)] public float timeFactor = 0.5f;

    public override ActiveLevelModifier CreateRuntime() => new Runtime();

    public override void OnGained(ActiveLevelModifier active)
    {
        if (active is Runtime runtime)
            runtime.Slow = GameTime.Hold(timeFactor);
    }

    public override void OnLost(ActiveLevelModifier active)
    {
        if (!(active is Runtime runtime))
            return;

        GameTime.Release(runtime.Slow);
        runtime.Slow = null;
    }

    /// <summary>
    /// Guarda a lentidão que este modificador ligou, para soltar exatamente a
    /// dele — e não "a lentidão", que poderia ser a de outro efeito valendo ao
    /// mesmo tempo.
    /// </summary>
    class Runtime : ActiveLevelModifier
    {
        public GameTime.Slow Slow;
    }
}
