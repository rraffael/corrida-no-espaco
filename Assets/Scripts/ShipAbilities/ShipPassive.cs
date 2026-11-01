using UnityEngine;

/// <summary>
/// A ficha de uma **passiva de nave**: o que vale a corrida inteira, sem o
/// jogador ativar nada e sem gastar uso.
///
/// **Ninguém tem uma ainda, e a estrutura existe assim mesmo** *(decisão do
/// Raffael, 30/08/2026)*. O plano é que a passiva destrave no nível 60 da nave —
/// o último dos três patamares de evolução —, e não existe nível ainda. Deixar o
/// campo pronto agora significa que, no dia em que a primeira for desenhada, ela
/// é um arquivo: nem a nave, nem o menu, nem a corrida precisam mudar.
///
/// **Por que é uma classe separada do <see cref="ShipAbility"/>**, em vez de um
/// poder com duração infinita. Passiva não tem ativação, não tem recarga, não
/// tem uso e não aparece no canto do HUD — herdar tudo isso só para deixar tudo
/// desligado daria uma ficha cheia de campo morto, que é exatamente o que o
/// Inspector dos poderes foi escrito para evitar.
///
/// **A ficha nunca guarda estado de partida** — para isso existe o
/// <see cref="ActiveShipPassive"/>, um por corrida. Os ganchos abaixo são os
/// momentos da corrida a que uma passiva pode reagir; gancho novo entra aqui.
/// </summary>
public abstract class ShipPassive : ScriptableObject
{
    [Header("Identidade")]
    public string displayName = "Passiva";

    [Tooltip("Uma linha dizendo o que ela faz. É o que a aba de naves mostra.")]
    [TextArea(1, 3)] public string description = "";

    /// <summary>
    /// A corrida começou. É aqui que a passiva aplica o que vale desde a largada.
    /// O <see cref="StatModifier"/> aplicado pelo <see cref="ActiveShipPassive"/>
    /// sai sozinho no fim; um <see cref="ShipTrait"/> quem ligou desliga no
    /// <see cref="OnRaceEnded"/>.
    /// </summary>
    public virtual void OnRaceStarted(ActiveShipPassive active) { }

    /// <summary>Roda a cada quadro de corrida, para a passiva que conta tempo.</summary>
    public virtual void OnTick(ActiveShipPassive active, float deltaTime) { }

    /// <summary>A nave destruiu um obstáculo a tiro — não na batida.</summary>
    public virtual void OnObstacleDestroyed(ActiveShipPassive active, ObstacleStats obstacle) { }

    /// <summary>A nave acabou de chegar à velocidade de dobra e começou a carregar.</summary>
    public virtual void OnWarpEntered(ActiveShipPassive active) { }

    /// <summary>
    /// O texto curto que a passiva acrescenta ao canto do poder, com ela valendo.
    /// Vazio: não acrescenta nada.
    /// </summary>
    public virtual string HudBonus(ActiveShipPassive active) => string.Empty;

    /// <summary>A corrida acabou.</summary>
    public virtual void OnRaceEnded(ActiveShipPassive active) { }
}
