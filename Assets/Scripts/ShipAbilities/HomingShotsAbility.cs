using UnityEngine;

/// <summary>
/// **Tiros teleguiados** — o poder da Nay. Por alguns segundos os tiros curvam
/// atrás do obstáculo mais próximo, seja qual for a faixa dele.
///
/// **O que ele desfaz é a regra de 30/08:** desde que a nave cala a arma para
/// trocar de faixa, atirar e desviar são coisas que competem pelo mesmo comando.
/// Enquanto este poder vale, deixam de competir — dá para atravessar o corredor
/// inteiro e continuar acertando. É por isso que ele é curto e tem recarga longa:
/// é uma folga, não o jeito normal de jogar.
///
/// Ele **não faz o tiro acertar sempre.** O tiro ganha uma taxa de curva, e um
/// obstáculo que apareça de lado bem perto ainda escapa. Ver
/// <see cref="Projectile"/>.
/// </summary>
[CreateAssetMenu(fileName = "Poder-TirosTeleguiados",
                 menuName = "Corrida no Espaço/Poder da nave/Tiros teleguiados")]
public class HomingShotsAbility : ShipAbility
{
    public override void OnActivated(ActiveShipAbility active) =>
        active.ApplyTrait(ShipTrait.HomingShots);
}
