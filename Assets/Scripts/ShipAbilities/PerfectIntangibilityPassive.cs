using UnityEngine;

/// <summary>
/// **Intangibilidade perfeita** — a passiva do Espreitador Espacial, no nível 60
/// *(pedido do Raffael, 18/10/2026)*. Enquanto a Intangibilidade dura, nada de
/// fora mexe na nave: o vácuo do Rastro não puxa e o modificador Debilitante
/// passa por dentro sem pegar.
///
/// **Ela não sabe quando o poder está ligado, e não precisa.** Liga o traço
/// <see cref="ShipTrait.PhasingImmunity"/> a corrida inteira; quem junta os dois
/// é o <see cref="ShipStats.IsUntouchable"/>, que só vale com o
/// <see cref="ShipTrait.Phasing"/> ligado também. Assim o poder e a passiva
/// continuam sem se conhecer.
/// </summary>
[CreateAssetMenu(fileName = "Passiva-IntangibilidadePerfeita",
                 menuName = "Corrida no Espaço/Passiva da nave/Intangibilidade perfeita")]
public class PerfectIntangibilityPassive : ShipPassive
{
    public override void OnRaceStarted(ShipStats ship)
    {
        if (ship != null)
            ship.AddTrait(ShipTrait.PhasingImmunity);
    }

    public override void OnRaceEnded(ShipStats ship)
    {
        if (ship != null)
            ship.RemoveTrait(ShipTrait.PhasingImmunity);
    }
}
