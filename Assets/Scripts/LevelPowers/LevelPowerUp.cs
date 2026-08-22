using UnityEngine;

/// <summary>
/// A ficha de um **poder de fase** — o que aparece na pista e vale ao ser pego.
/// Cada poder é uma classe filha desta, com um arquivo em
/// <c>Assets/Powers/Fase/</c>.
///
/// **Isto não tem nada a ver com <see cref="ShipAbility"/>, e é de propósito**
/// *(decidido pelo Raffael em 22/08/2026)*. Os dois tipos de poder do jogo são
/// sistemas separados de ponta a ponta — classe base, ficha, componente na nave,
/// pasta e vocabulário. Poder de fase se **pega**; poder de nave se **ativa**.
/// Nenhum código é compartilhado, então mexer num não tem como quebrar o outro,
/// e nenhuma ficha mostra campo que não vale para ela.
///
/// **Para que servem:** dinamizar a partida. São simples de propósito — a
/// decisão que eles pedem é uma só, e é dentro da corrida: vale desviar para
/// pegar aquilo?
///
/// **O que fica aqui e o que fica na filha.** Aqui, o que todo poder de fase tem
/// — nome, arte, se é bom ou ruim, quanto dura — e os **ganchos**, que são os
/// momentos da partida em que um poder *pode* querer se meter. Todos vazios por
/// padrão. O **efeito** é sempre da filha.
///
/// A regra para decidir onde uma coisa vai: se é um *momento* da partida, vira
/// gancho aqui; se é *o que acontece* naquele momento, fica na filha.
///
/// **A ficha nunca guarda estado de partida.** Ela é um asset compartilhado, e
/// escrever nela mudaria o arquivo em disco. O que muda durante a corrida vive
/// no <see cref="ActiveLevelPowerUp"/>.
/// </summary>
public abstract class LevelPowerUp : ScriptableObject
{
    /// <summary>Como o poder acaba.</summary>
    public enum Lifetime
    {
        /// <summary>Faz o que tem de fazer na hora e some. Reparo é assim.</summary>
        Instantaneo,

        /// <summary>Vale por alguns segundos.</summary>
        PorTempo,

        /// <summary>
        /// Vale por um número de cargas. Proteção que segura um golpe é assim, e
        /// um poder de tempo lento medido em obstáculos também.
        /// </summary>
        PorUso,
    }

    [Header("Identidade")]
    public string displayName = "Poder";

    [Tooltip("Uma linha dizendo o que ele faz, para quando houver tela mostrando isso.")]
    [TextArea(1, 3)] public string description = "";

    [Tooltip("Arte do item na pista.")]
    public Sprite sprite;

    public Color color = Color.white;

    [Header("Natureza")]
    [Tooltip("Marcado, é um poder RUIM: o jogador tem de desviar dele em vez de pegar. " +
             "Muda só a leitura do jogo e a cor na tela — o efeito continua saindo da classe.")]
    public bool isHarmful;

    [Tooltip("Peso relativo no sorteio de qual poder aparece na fase. Maior, aparece mais. " +
             "NINGUÉM LÊ ISTO AINDA — entra quando existir o sorteio, que depende de decidir " +
             "como o poder chega até a nave.")]
    [Min(0f)] public float spawnWeight = 1f;

    [Header("Duração")]
    public Lifetime lifetime = Lifetime.PorTempo;

    [Tooltip("Só vale com duração PorTempo.")]
    [Min(0f)] public float durationSeconds = 5f;

    [Tooltip("Quantas CARGAS o poder tem antes de se esvair. Só vale com duração PorUso: cada uso " +
             "gasta uma, e na última o poder acaba. Um escudo de 1 segura um golpe; de 3, três.")]
    [Min(1)] public int charges = 1;

    /// <summary>
    /// A cópia viva deste poder. Sobrescreva **só** se a filha precisar guardar
    /// algo além de tempo e cargas — por exemplo, o valor original de um atributo
    /// para devolver quando o poder acabar.
    /// </summary>
    public virtual ActiveLevelPowerUp CreateRuntime() => new ActiveLevelPowerUp();

    /// <summary>Acabou de ser pego. Efeito instantâneo (curar, por exemplo) acontece aqui.</summary>
    public virtual void OnGained(ActiveLevelPowerUp active) { }

    /// <summary>Um quadro se passou, e o poder ainda vale. Para efeito contínuo.</summary>
    public virtual void OnTick(ActiveLevelPowerUp active, float deltaTime) { }

    /// <summary>
    /// A nave vai tomar um golpe, e o poder pode mexer nele: reduzir o dano,
    /// zerar, ou cancelar o custo de tempo da batida.
    ///
    /// **Já chega descontado pela defesa da nave.** Isso importa: o desconto da
    /// defesa tem piso de 1 de dano, de propósito, então um poder que queira
    /// segurar o golpe inteiro precisa rodar depois dele — que é aqui.
    ///
    /// Um poder educado **confere se o golpe ainda não foi segurado** antes de
    /// gastar carga com ele.
    /// </summary>
    public virtual void ModifyIncomingHit(ActiveLevelPowerUp active, ref ShipStats.Hit hit) { }

    /// <summary>
    /// A nave destruiu um obstáculo — a tiro, não na batida. Vem a ficha do que
    /// caiu, então dá para reagir só a um tipo.
    /// </summary>
    public virtual void OnObstacleDestroyed(ActiveLevelPowerUp active, ObstacleStats obstacle) { }

    /// <summary>
    /// Um obstáculo passou pela nave e saiu de cena — desviado, não destruído.
    /// É o que um poder **limitado a X obstáculos** usa para gastar carga.
    /// </summary>
    public virtual void OnObstaclePassed(ActiveLevelPowerUp active, ObstacleStats obstacle) { }

    /// <summary>A nave mudou de faixa. Vem o número da faixa, não "esquerda ou direita".</summary>
    public virtual void OnLaneChanged(ActiveLevelPowerUp active, int lane) { }

    /// <summary>
    /// Um tiro acabou de sair. Para poder que conta tiros.
    ///
    /// **Não serve para mudar dano ou cadência** — isso é modificador de
    /// atributo, e o tiro já sai com os números certos antes de este gancho
    /// rodar. Ver <see cref="ActiveLevelPowerUp.ApplyModifier"/>.
    /// </summary>
    public virtual void OnShotFired(ActiveLevelPowerUp active) { }

    /// <summary>Acabou — tempo esgotado, cargas gastas, ou a corrida terminou.</summary>
    public virtual void OnLost(ActiveLevelPowerUp active) { }
}
