using UnityEngine;

/// <summary>
/// **Super IA** — o poder da Raffa. Por alguns segundos o jogo assume a nave e
/// desvia sozinho; o arraste do jogador é ignorado enquanto durar.
///
/// **Carga única e sem recarga**, por desenho: é a carta que se guarda para o
/// momento em que a fase apertou e o polegar não vai dar conta. Um poder que
/// joga por você não pode estar disponível o tempo todo — deixaria de ser alívio
/// e viraria a maneira de jogar.
///
/// Quem pilota é o <see cref="Autopilot"/>, que fica na nave e liga sozinho pelo
/// traço. Este arquivo só liga o traço: assim um segundo poder que queira
/// pilotar, ou um modificador de fase que tire o controle do jogador, não custa
/// uma linha de piloto nova.
/// </summary>
[CreateAssetMenu(fileName = "Poder-SuperIA",
                 menuName = "Corrida no Espaço/Poder da nave/Super IA")]
public class AutopilotAbility : ShipAbility
{
    public override void OnActivated(ActiveShipAbility active) =>
        active.ApplyTrait(ShipTrait.Autopilot);
}
