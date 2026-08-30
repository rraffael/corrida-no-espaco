using UnityEngine;

/// <summary>
/// A ficha de um **poder da nave** — o que o jogador equipa fora da partida, ou
/// que a nave traz de fábrica, e **ativa** quando quer, com toque duplo. Cada
/// poder é uma classe filha desta, com um arquivo em <c>Assets/Powers/Nave/</c>.
///
/// **Isto não tem nada a ver com <see cref="LevelModifier"/>, e é de propósito**
/// *(decidido pelo Raffael em 22/08/2026)*. Os dois tipos de poder do jogo são
/// sistemas separados de ponta a ponta — classe base, ficha, componente na nave,
/// pasta e vocabulário. Modificador de fase se **pega**; poder de nave se **ativa**.
/// Nenhum código é compartilhado, então mexer num não tem como quebrar o outro,
/// e nenhuma ficha mostra campo que não vale para ela.
///
/// **Para que servem:** tornar a nave única, e dar ao jogador a ferramenta para
/// vencer uma fase específica. São duas decisões, e é isso que os diferencia do
/// modificador de fase: **o que levar**, antes da corrida, e **quando gastar**, durante.
///
/// **Os dois limites são independentes.** <see cref="usesPerRace"/> é quantas
/// vezes o jogador *liga* o poder; <see cref="charges"/> é quanto o efeito
/// aguenta *depois* de ligado. Um escudo de 2 usos e 1 carga liga duas vezes na
/// partida, e cada vez segura um golpe.
///
/// **A ficha nunca guarda estado de partida** — para isso existe o
/// <see cref="ActiveShipAbility"/>.
/// </summary>
public abstract class ShipAbility : ScriptableObject
{
    /// <summary>Como o efeito acaba, depois de ligado.</summary>
    public enum Lifetime
    {
        /// <summary>Faz o que tem de fazer na hora de ativar e some.</summary>
        Instantaneo,

        /// <summary>Vale por alguns segundos depois de ativado.</summary>
        PorTempo,

        /// <summary>Vale por um número de cargas depois de ativado.</summary>
        PorUso,
    }

    [Header("Identidade")]
    public string displayName = "Poder da nave";

    [Tooltip("Uma linha dizendo o que ele faz, para a tela de equipar e para o canto do HUD.")]
    [TextArea(1, 3)] public string description = "";

    [Tooltip("Ícone que aparece no canto da tela durante a corrida.")]
    public Sprite icon;

    public Color color = Color.white;

    [Header("Ativação")]
    [Tooltip("Quantas vezes o jogador pode ATIVAR este poder numa partida.\n\n" +
             "ZERO quer dizer SEM LIMITE: aí quem segura o poder é só a recarga. É a diferença " +
             "entre os dois desenhos que existem hoje — os Tiros teleguiados voltam a cada 30 s a " +
             "corrida toda, e a Super IA é uma carta só, que não volta.")]
    [Min(0)] public int usesPerRace = 1;

    /// <summary>Sem limite de ativações: quem segura é a recarga, e só ela.</summary>
    public bool HasUnlimitedUses => usesPerRace <= 0;

    [Tooltip("Segundos de espera entre um uso e o próximo. Zero: pode reativar assim que quiser.\n\n" +
             "A recarga começa na ATIVAÇÃO, e não no fim do efeito — assim o jogador sabe quando " +
             "pode contar com ele de novo sem acompanhar quanto o efeito anterior ainda dura.")]
    [Min(0f)] public float cooldownSeconds = 15f;

    [Header("Duração do efeito")]
    public Lifetime lifetime = Lifetime.PorTempo;

    [Tooltip("Só vale com duração PorTempo.")]
    [Min(0f)] public float durationSeconds = 5f;

    [Tooltip("Quantas cargas o efeito tem depois de ligado. Só vale com duração PorUso.")]
    [Min(1)] public int charges = 1;

    /// <summary>
    /// A cópia viva deste poder. Sobrescreva **só** se a filha precisar guardar
    /// algo além de tempo e cargas.
    /// </summary>
    public virtual ActiveShipAbility CreateRuntime() => new ActiveShipAbility();

    /// <summary>
    /// O jogador acabou de ativar. É o equivalente ao "acabou de ser pego" do
    /// modificador de fase — só que aqui foi escolha, e custou um uso.
    /// </summary>
    public virtual void OnActivated(ActiveShipAbility active) { }

    /// <summary>Um quadro se passou, e o efeito ainda vale.</summary>
    public virtual void OnTick(ActiveShipAbility active, float deltaTime) { }

    /// <summary>
    /// A nave vai tomar um golpe, e o poder pode mexer nele. Já chega descontado
    /// pela defesa — o desconto tem piso de 1 de dano, então quem quer segurar o
    /// golpe inteiro precisa rodar depois dele, que é aqui.
    /// </summary>
    public virtual void ModifyIncomingHit(ActiveShipAbility active, ref ShipStats.Hit hit) { }

    /// <summary>A nave destruiu um obstáculo a tiro.</summary>
    public virtual void OnObstacleDestroyed(ActiveShipAbility active, ObstacleStats obstacle) { }

    /// <summary>Um obstáculo passou pela nave e saiu de cena, desviado.</summary>
    public virtual void OnObstaclePassed(ActiveShipAbility active, ObstacleStats obstacle) { }

    /// <summary>A nave mudou de faixa. Vem o número da faixa.</summary>
    public virtual void OnLaneChanged(ActiveShipAbility active, int lane) { }

    /// <summary>Um tiro acabou de sair.</summary>
    public virtual void OnShotFired(ActiveShipAbility active) { }

    /// <summary>O efeito acabou — tempo esgotado, cargas gastas, ou fim da corrida.</summary>
    public virtual void OnEnded(ActiveShipAbility active) { }
}
