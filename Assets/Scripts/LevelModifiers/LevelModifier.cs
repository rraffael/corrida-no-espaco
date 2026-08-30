using UnityEngine;

/// <summary>
/// A ficha de um **modificador de fase** — o que aparece na pista e vale ao ser
/// pego. Cada um é uma classe filha desta, com um arquivo em
/// <c>Assets/Modificadores/</c>.
///
/// **Isto não tem nada a ver com <see cref="ShipAbility"/>, e é de propósito**
/// *(decidido pelo Raffael em 22/08/2026)*. Os dois sistemas de poder do jogo
/// são separados de ponta a ponta — classe base, ficha, componente na nave,
/// pasta e vocabulário. Modificador de fase se **pega**; poder de nave se
/// **ativa**. Nenhum código é compartilhado, então mexer num não tem como
/// quebrar o outro, e nenhuma ficha mostra campo que não vale para ela.
///
/// **Para que servem:** dinamizar a partida. São simples de propósito — a
/// decisão que eles pedem é uma só, e é dentro da corrida: vale desviar para
/// pegar aquilo?
///
/// **O que fica aqui e o que fica na filha.** Aqui, o que todo modificador tem —
/// nome, arte, categoria, quanto dura — e os **ganchos**, que são os momentos da
/// partida em que um modificador *pode* querer se meter. Todos vazios por
/// padrão. O **efeito** é sempre da filha.
///
/// A regra para decidir onde uma coisa vai: se é um *momento* da partida, vira
/// gancho aqui; se é *o que acontece* naquele momento, fica na filha.
///
/// **A ficha nunca guarda estado de partida.** Ela é um asset compartilhado, e
/// escrever nela mudaria o arquivo em disco. O que muda durante a corrida vive
/// no <see cref="ActiveLevelModifier"/>.
/// </summary>
public abstract class LevelModifier : ScriptableObject
{
    /// <summary>
    /// O que este modificador é, para o jogador. **É rótulo e é regra ao mesmo
    /// tempo:** manda na cor do item na pista e é o que diz se ele deve ser
    /// pego ou desviado.
    /// </summary>
    public enum Category
    {
        /// <summary>Aumenta alguma coisa da nave. Pegar é bom.</summary>
        Reforco,

        /// <summary>Reduz alguma coisa da nave. Desviar é bom.</summary>
        Debilitante,

        /// <summary>Efeito excêntrico, que não é simplesmente mais ou menos.</summary>
        Especial,
    }

    /// <summary>Como o modificador acaba.</summary>
    public enum Lifetime
    {
        /// <summary>Faz o que tem de fazer na hora e some. Reparo é assim.</summary>
        Instantaneo,

        /// <summary>Vale por alguns segundos.</summary>
        PorTempo,

        /// <summary>Vale por um número de cargas.</summary>
        PorUso,
    }

    [Header("Identidade")]
    public string displayName = "Modificador";

    [Tooltip("Uma linha dizendo o que ele faz, para a tela que mostrar isso.")]
    [TextArea(1, 3)] public string description = "";

    [Tooltip("Arte do item na pista e do quadradinho no HUD.")]
    public Sprite sprite;

    [Header("Natureza")]
    [Tooltip("Reforço aumenta algo da nave; Debilitante reduz; Especial é o que não é nem um nem " +
             "outro.\n\nNão é só rótulo: é daqui que sai a cor do item na pista, e é o que diz ao " +
             "jogador se aquilo se pega ou se desvia.")]
    public Category category = Category.Reforco;

    [Tooltip("Peso relativo no sorteio de qual modificador aparece. Maior, aparece mais.")]
    [Min(0f)] public float spawnWeight = 1f;

    [Header("Duração")]
    public Lifetime lifetime = Lifetime.PorTempo;

    [Tooltip("Só vale com duração PorTempo.")]
    [Min(0f)] public float durationSeconds = 5f;

    [Tooltip("Quantas CARGAS o modificador tem antes de se esvair. Só vale com duração PorUso: " +
             "cada uso gasta uma, e na última ele acaba.")]
    [Min(1)] public int charges = 1;

    /// <summary>
    /// A cor com que este modificador aparece na pista e no HUD. Sai da
    /// categoria, e não de um campo por ficha, **de propósito**: se cada arquivo
    /// escolhesse a própria cor, a décima ficha teria um verde ligeiramente
    /// diferente da primeira e a leitura de relance — a única que o jogador tem
    /// tempo de fazer — iria embora.
    /// </summary>
    public Color Color => ColorFor(category);

    public static Color ColorFor(Category category)
    {
        switch (category)
        {
            case Category.Debilitante: return new Color(1f, 0.42f, 0.38f, 1f);
            case Category.Especial: return new Color(0.78f, 0.55f, 1f, 1f);
            default: return new Color(0.45f, 1f, 0.68f, 1f);
        }
    }

    /// <summary>Nome da categoria em português, para tela e para log.</summary>
    public static string NameFor(Category category)
    {
        switch (category)
        {
            case Category.Debilitante: return "Debilitante";
            case Category.Especial: return "Especial";
            default: return "Reforço";
        }
    }

    /// <summary>
    /// A cópia viva deste modificador. Sobrescreva **só** se a filha precisar
    /// guardar algo além de tempo e cargas.
    /// </summary>
    public virtual ActiveLevelModifier CreateRuntime() => new ActiveLevelModifier();

    /// <summary>Acabou de ser pego. Efeito instantâneo (curar) acontece aqui.</summary>
    public virtual void OnGained(ActiveLevelModifier active) { }

    /// <summary>
    /// Foi pego **de novo**, com este ainda valendo. O tempo e as cargas já
    /// foram devolvidos ao cheio antes de este gancho rodar.
    ///
    /// Existe porque renovar não é o mesmo que pegar: um modificador que aplica
    /// um <see cref="StatModifier"/> no <see cref="OnGained"/> aplicaria um
    /// segundo aqui, e o efeito dobraria só por o jogador ter passado duas vezes
    /// em cima. Quem precisa mesmo reagir à renovação sobrescreve; quase ninguém
    /// precisa.
    /// </summary>
    public virtual void OnRenewed(ActiveLevelModifier active) { }

    /// <summary>Um quadro se passou, e o modificador ainda vale.</summary>
    public virtual void OnTick(ActiveLevelModifier active, float deltaTime) { }

    /// <summary>
    /// A nave vai tomar um golpe, e o modificador pode mexer nele: reduzir o
    /// dano, zerar, ou cancelar o custo de tempo da batida.
    ///
    /// **Já chega descontado pela defesa da nave.** Isso importa: o desconto da
    /// defesa tem piso de 1 de dano, de propósito, então quem queira segurar o
    /// golpe inteiro precisa rodar depois dele — que é aqui.
    /// </summary>
    public virtual void ModifyIncomingHit(ActiveLevelModifier active, ref ShipStats.Hit hit) { }

    /// <summary>
    /// A nave destruiu um obstáculo — a tiro, não na batida. Vem a ficha do que
    /// caiu, então dá para reagir só a um tipo.
    /// </summary>
    public virtual void OnObstacleDestroyed(ActiveLevelModifier active, ObstacleStats obstacle) { }

    /// <summary>
    /// Um obstáculo passou pela nave e saiu de cena — desviado, não destruído.
    /// É o que um modificador **limitado a X obstáculos** usa para gastar carga.
    /// </summary>
    public virtual void OnObstaclePassed(ActiveLevelModifier active, ObstacleStats obstacle) { }

    /// <summary>A nave mudou de faixa. Vem o número da faixa, não "esquerda ou direita".</summary>
    public virtual void OnLaneChanged(ActiveLevelModifier active, int lane) { }

    /// <summary>
    /// Um tiro acabou de sair. Para quem conta tiros.
    ///
    /// **Não serve para mudar dano ou cadência** — isso é modificador de
    /// atributo, e o tiro já sai com os números certos antes de este gancho
    /// rodar. Ver <see cref="ActiveLevelModifier.ApplyModifier"/>.
    /// </summary>
    public virtual void OnShotFired(ActiveLevelModifier active) { }

    /// <summary>Acabou — tempo esgotado, cargas gastas, ou a corrida terminou.</summary>
    public virtual void OnLost(ActiveLevelModifier active) { }
}
