using UnityEngine;

/// <summary>
/// **Especial — Recarga de poder.** Recarrega o poder da nave na hora: a espera
/// zera e, se o poder for de usos limitados e já tiver gasto algum, volta um.
/// Nunca passa do que a corrida dá — com o poder pronto e cheio, pegar não faz
/// nada.
///
/// **Especial, e não Reforço** *(escolha do Raffael, 26/09/2026)*: não mexe em
/// atributo nenhum, e o quanto ele vale depende da nave e do momento — é um
/// segundo Tiros teleguiados para a Nay, uma segunda Super IA para a Raffa, e
/// nada para quem não tem poder ativo.
///
/// Não fala com o poder direto: pede à nave, que é quem conhece os dois
/// sistemas. Ver <see cref="ShipStats.RechargeAbility"/>.
/// </summary>
[CreateAssetMenu(fileName = "Especial-RecargaDePoder",
                 menuName = "Corrida no Espaço/Modificador de fase/Especial — recarga de poder")]
public class AbilityRecharge : LevelModifier
{
    public override void OnGained(ActiveLevelModifier active)
    {
        var stats = active.Owner != null ? active.Owner.Stats : null;
        if (stats != null)
            stats.RechargeAbility();
    }
}
