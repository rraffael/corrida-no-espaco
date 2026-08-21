using UnityEngine;

/// <summary>
/// A ficha de um poder — a base de todos eles. Cada poder do jogo é uma **classe
/// filha** desta, com um arquivo em <c>Assets/Powers/</c>: a classe diz o que
/// aquele poder *faz*, e o arquivo diz com que números ele faz.
///
/// **O que fica aqui e o que fica na filha.** Aqui ficam as coisas que todo poder
/// tem — nome, arte, se é bom ou ruim, e quanto tempo dura. E ficam os
/// **ganchos**: os pontos da vida da nave em que um poder *pode* querer se meter
/// (ganhou, passou o tempo, tomou um golpe, acabou). Todos são vazios por padrão,
/// então uma filha só escreve o gancho que lhe interessa.
///
/// O **efeito**, esse é sempre da filha. A regra para decidir onde uma coisa vai:
/// se é um *momento* da partida, vira gancho aqui; se é *o que acontece* naquele
/// momento, fica na filha. Assim a base não cresce a cada poder novo — cresce
/// só quando o jogo ganha um momento novo.
///
/// **A ficha nunca guarda estado de partida.** Ela é um asset compartilhado, e
/// escrever nela mudaria o arquivo em disco. Quanto ainda falta de escudo, de
/// tempo, de carga — tudo isso vive no <see cref="ActivePowerUp"/>, que é a
/// cópia viva daquele poder nesta corrida. É a mesma separação da ficha da nave.
/// </summary>
public abstract class PowerUpDefinition : ScriptableObject
{
    /// <summary>Como o poder acaba.</summary>
    public enum Lifetime
    {
        /// <summary>Faz o que tem de fazer na hora e some. Reparo é assim.</summary>
        Instantaneo,

        /// <summary>Vale por alguns segundos. Tiro rápido é assim.</summary>
        PorTempo,

        /// <summary>Vale por um número de usos. Proteção que segura um golpe é assim.</summary>
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

    [Tooltip("Peso relativo no sorteio de qual poder aparece. Maior, aparece mais. " +
             "NINGUÉM LÊ ISTO AINDA — entra quando existir o sorteio, que depende de decidir " +
             "como o poder chega até a nave.")]
    [Min(0f)] public float spawnWeight = 1f;

    [Header("Duração")]
    public Lifetime lifetime = Lifetime.PorTempo;

    [Tooltip("Só vale com duração PorTempo.")]
    [Min(0f)] public float durationSeconds = 5f;

    [Tooltip("Quantas CARGAS o poder tem antes de se esvair. Só vale com duração PorUso: cada uso gasta " +
             "uma, e na última o poder acaba. Um escudo de 1 segura um golpe; de 3, segura três.")]
    [Min(1)] public int charges = 1;

    /// <summary>
    /// A cópia viva deste poder. Sobrescreva **só** se a filha precisar guardar
    /// algo além de tempo e cargas — por exemplo, o valor original de um atributo
    /// para devolver quando o poder acabar. É esta a saída para efeito específico
    /// que precisa de memória própria, sem sujar a base.
    /// </summary>
    public virtual ActivePowerUp CreateRuntime() => new ActivePowerUp();

    /// <summary>Acabou de ser pego. Efeito instantâneo (curar, por exemplo) acontece aqui.</summary>
    public virtual void OnGained(ActivePowerUp active) { }

    /// <summary>Um quadro se passou, e o poder ainda vale. Para efeito contínuo.</summary>
    public virtual void OnTick(ActivePowerUp active, float deltaTime) { }

    /// <summary>
    /// A nave vai tomar um golpe, e o poder pode mexer nele: reduzir o dano,
    /// zerar, ou cancelar o custo de tempo da batida.
    ///
    /// **Já chega descontado pela defesa da nave.** Isso importa: o desconto da
    /// defesa tem piso de 1 de dano, de propósito, então um poder que queira
    /// segurar o golpe inteiro precisa rodar depois dele — que é aqui.
    ///
    /// Se mais de um poder ativo mexe no golpe, todos são chamados, na ordem em
    /// que foram pegos. Um poder educado **confere se o golpe ainda não foi
    /// segurado** antes de gastar carga com ele.
    /// </summary>
    public virtual void ModifyIncomingHit(ActivePowerUp active, ref ShipStats.Hit hit) { }

    /// <summary>
    /// A nave destruiu um obstáculo — a tiro, não na batida. Vem a ficha do que
    /// caiu, então dá para reagir só a um tipo: "a cada Barcaça derrubada, cura".
    /// </summary>
    public virtual void OnObstacleDestroyed(ActivePowerUp active, ObstacleStats obstacle) { }

    /// <summary>
    /// A nave mudou de faixa. Serve para poder que cobra ou premia movimento —
    /// e para o dia em que a pista tiver largura variável, porque a faixa nova
    /// chega como número, não como "esquerda ou direita".
    /// </summary>
    public virtual void OnLaneChanged(ActivePowerUp active, int lane) { }

    /// <summary>
    /// Um tiro acabou de sair. Para poder por munição, por rajada, ou que faça
    /// algo a cada N tiros.
    ///
    /// **Não serve para mudar dano ou cadência** — isso é modificador de
    /// atributo, e o tiro já sai com os números certos antes de este gancho
    /// rodar. Ver <see cref="ActivePowerUp.ApplyModifier"/>.
    /// </summary>
    public virtual void OnShotFired(ActivePowerUp active) { }

    /// <summary>Acabou — tempo esgotado, cargas gastas, ou a corrida terminou. Para desfazer o efeito.</summary>
    public virtual void OnLost(ActivePowerUp active) { }
}
