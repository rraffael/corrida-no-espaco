using UnityEngine;

/// <summary>
/// A ficha da nave: o que a nave **é**, em números. Cada nave é um arquivo em
/// <c>Assets/Ships/</c>, no mesmo espírito das fichas de obstáculo e de fase —
/// então nave nova é preencher campos, não escrever código.
///
/// **Por que ScriptableObject, e não campos no componente.** Enquanto os números
/// moravam no componente da cena, a ficha vivia pendurada num objeto: uma segunda
/// nave seria uma segunda cópia do objeto com os valores digitados de novo, e
/// evolução de atributo seria código. Como arquivo, a ficha existe fora da cena,
/// e o dia de ter cinco naves custa cinco arquivos.
///
/// **Ela é só leitura em tempo de jogo, e isso é de propósito.** Um asset é
/// compartilhado: escrever nele durante a partida mudaria o arquivo em disco no
/// Editor, e o valor mexido sobreviveria ao fim da corrida. Poder, evolução e
/// qualquer coisa que altere a nave no meio do jogo mexem no
/// <see cref="ShipStats"/>, que é a nave viva; aqui fica o valor de base.
/// </summary>
[CreateAssetMenu(fileName = "Nave", menuName = "Corrida no Espaço/Ficha de nave")]
public class ShipDefinition : ScriptableObject
{
    [Header("Identidade")]
    [Tooltip("Nome que a nave tem para o jogador. Ainda não aparece em tela — vai aparecer " +
             "quando existir escolha de nave.")]
    public string displayName = "Nave";

    [Header("Corrida")]
    [Tooltip("Velocidade de cruzeiro: o padrão para onde a nave acelera sozinha, em unidades por segundo.")]
    [SerializeField, Min(0f)] float cruiseSpeed = 8f;

    [Tooltip("Quanto a velocidade muda por segundo. Manda em quão rápido a nave se recupera " +
             "de uma freada. É o valor BASE: abaixo da velocidade de cruzeiro o RaceSpeed " +
             "multiplica isto pelo bônus de baixa velocidade (até 3x com a nave parada), " +
             "então 4 leva perto de 1s do zero ao cruzeiro, e não 2s.")]
    [SerializeField, Min(0.1f)] float acceleration = 4f;

    [Tooltip("Fração da aceleração da nave que cada obstáculo de PESO 1 rende ao ser destruído. " +
             "0,16 com aceleração 4 dá 0,64 u/s por abate, ou uns 4 segundos de ganho passivo. " +
             "Sai da aceleração, e não de um número solto: uma nave mais potente converte abate " +
             "em velocidade mais depressa, e o abate vale sempre os mesmos segundos de paciência.")]
    [SerializeField, Min(0f)] float killGainFactor = 0.16f;

    [Tooltip("Quanto uma batida custa nesta nave, como fator. 1 é o custo cheio; 0,5 é metade do " +
             "atraso, do trecho arrastado e da punição de velocidade; 0 é bater de graça.\n\n" +
             "Não mexe na profundidade da queda — o susto de bater é o mesmo. Mexe no PREÇO, que " +
             "é o tempo que se perde voltando.")]
    [SerializeField, Min(0f)] float crashCost = 1f;

    [Header("Combate")]
    [Tooltip("Dano de cada tiro.")]
    [SerializeField, Min(0f)] float damage = 25f;

    [Tooltip("Velocidade de ataque, em tiros por segundo.")]
    [SerializeField, Min(0.1f)] float attackSpeed = 3f;

    [Tooltip("Vida cheia da nave. Zero é derrota.")]
    [SerializeField, Min(1f)] float maxHealth = 100f;

    [Tooltip("Redução de dano, em porcentagem. O dano recebido é sempre arredondado para cima, " +
             "então nem 99% de defesa deixa a nave imune: um golpe sempre tira pelo menos 1.")]
    [SerializeField, Range(0f, 99f)] float defensePercent = 0f;

    public float CruiseSpeed => cruiseSpeed;
    public float Acceleration => acceleration;
    public float Damage => damage;
    public float AttackSpeed => attackSpeed;
    public float MaxHealth => maxHealth;
    public float DefensePercent => defensePercent;
    public float CrashCost => crashCost;

    /// <summary>
    /// Velocidade que um obstáculo de **peso 1** rende ao morrer. A ficha do
    /// obstáculo diz quanto ele vale em relação aos outros; esta ficha diz quanto
    /// a nave tira disso.
    ///
    /// É proporcional à aceleração porque o ganho passivo também é: assim um
    /// abate vale **os mesmos segundos de paciência** em qualquer nave, e a
    /// escolha entre atirar e desviar continua valendo a mesma coisa quando as
    /// naves mudarem. Destruir compensa, desviar também fecha a corrida — só
    /// mais devagar.
    /// </summary>
    public float KillSpeedGain => acceleration * killGainFactor;

    /// <summary>
    /// A fração crua, para quem precisa recalcular o ganho por abate sobre uma
    /// aceleração que não é a da ficha — é o caso do <see cref="ShipStats"/>
    /// quando um poder mexe na aceleração.
    /// </summary>
    public float KillGainFactor => killGainFactor;

    /// <summary>Segundos entre um tiro e o próximo.</summary>
    public float ShotInterval => 1f / Mathf.Max(0.1f, attackSpeed);
}
