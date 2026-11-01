using UnityEngine;

/// <summary>
/// **Dobra assistida** — a passiva da Raffa, no nível 60 *(pedido do Raffael,
/// 01/11/2026; o nome é provisório)*. Ao chegar à velocidade de dobra, o poder
/// ativo dispara sozinho, se houver carga. É o trunfo da nave: chegar até a dobra
/// e deixar o poder terminar a fase.
///
/// **Gastar a carga antes tira o seguro**, e é essa a decisão que sobra para o
/// jogador. Se o efeito já estiver ligado e sobrar carga, dispara do mesmo jeito:
/// o <see cref="ShipAbilities.Activate"/> volta a duração ao máximo.
///
/// **Não vale na sem fim**, que não tem dobra.
/// </summary>
[CreateAssetMenu(fileName = "Passiva-DobraAssistida",
                 menuName = "Corrida no Espaço/Passiva da nave/Dobra assistida")]
public class AutoWarpPassive : ShipPassive
{
    public override void OnWarpEntered(ActiveShipPassive active)
    {
        if (active.Owner != null)
            active.Owner.Activate();
    }
}
