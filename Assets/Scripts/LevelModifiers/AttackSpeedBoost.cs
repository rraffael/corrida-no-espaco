using UnityEngine;

/// <summary>
/// **Reforço — Aumento de Velocidade de Tiro.** Dobra a cadência da nave por
/// alguns segundos.
///
/// **O que ele compra não é dano por segundo, é liberdade de movimento**
/// *(consequência da regra aprovada em 30/08/2026)*. Desde que a nave para de
/// atirar enquanto troca de faixa, cadência alta significa que dá para sair da
/// faixa e voltar sem perder o abate — o custo da troca fica menor. É por isso
/// que este é um Reforço interessante e não só um número subindo.
/// </summary>
[CreateAssetMenu(fileName = "Reforco-CadenciaDobrada",
                 menuName = "Corrida no Espaço/Modificador de fase/Reforço — cadência")]
public class AttackSpeedBoost : LevelModifier
{
    [Header("Efeito")]
    [Tooltip("Quanto da cadência de BASE ele acrescenta. 1 = +100%, ou seja, o dobro.\n\n" +
             "Mede contra a base e não contra o valor de agora, que é a regra de todo " +
             "modificador de atributo do jogo: assim dois efeitos de cadência somam em vez de " +
             "se multiplicarem, e a ordem em que foram pegos nunca importa.")]
    [Min(0f)] public float extraAttackSpeed = 1f;

    public override void OnGained(ActiveLevelModifier active) =>
        active.ApplyModifier(StatModifier.Times(ShipStat.AttackSpeed, 1f + extraAttackSpeed));
}
