using UnityEngine;

/// <summary>
/// A ficha de um **poder condicional de nave**: dispara sozinho quando algo
/// acontece na corrida — sem toque duplo, sem uso e sem recarga. Cada um é uma
/// classe filha desta, com um arquivo em <c>Assets/Poderes/</c>.
///
/// **É o terceiro tipo de poder de nave, e separado dos outros dois de propósito**
/// *(decidido pelo Raffael em 26/09/2026, com o Reforço estrutural da
/// Predadora)*. Do <see cref="ShipAbility"/> ele não tem a ativação, a recarga
/// nem os usos; da <see cref="ShipPassive"/>, não tem o "vale desde a largada" —
/// ele reage. Herdar de qualquer um dos dois seria uma ficha cheia de campo
/// morto, que é o que a separação da passiva já evitava.
///
/// **O que fica aqui e o que fica na filha:** aqui, os momentos da corrida a que
/// um condicional pode reagir; na filha, o que ele faz. Condição nova que um
/// poder futuro precisar vira gancho aqui.
///
/// **A ficha nunca guarda estado de partida** — para isso existe o
/// <see cref="ActiveShipConditional"/>, um por corrida.
/// </summary>
public abstract class ShipConditional : ScriptableObject
{
    [Header("Identidade")]
    public string displayName = "Poder condicional";

    [Tooltip("Uma linha dizendo o que ele faz. É o que a aba de naves mostra.")]
    [TextArea(1, 3)] public string description = "";

    [Tooltip("O que faz ele disparar, na linguagem do jogador. Aparece na aba de naves.")]
    public string trigger = "";

    [Tooltip("Ícone do canto do poder durante a corrida.")]
    public Sprite icon;

    [Tooltip("Cor do canto do poder durante a corrida.")]
    public Color color = Color.white;

    /// <summary>
    /// Quantas cargas o condicional junta no máximo — o que o canto do HUD
    /// mostra como "de N". Zero: não conta carga, e o canto não mostra número.
    /// </summary>
    public virtual int MaxCount => 0;

    /// <summary>
    /// Em que estágio visual a nave está. 0 é o visual de largada; cada número
    /// acima é uma mudança. Quem desenha é o <see cref="ShipStageLook"/>, e quem
    /// decide quando muda é a filha.
    /// </summary>
    public virtual int Stage(ActiveShipConditional active) => 0;

    /// <summary>A corrida começou. Quase ninguém precisa; o efeito vem das condições.</summary>
    public virtual void OnRaceStarted(ActiveShipConditional active) { }

    /// <summary>A nave destruiu um obstáculo a tiro — não na batida.</summary>
    public virtual void OnObstacleDestroyed(ActiveShipConditional active, ObstacleStats obstacle) { }

    /// <summary>
    /// A corrida acabou. **Não precisa desfazer atributo:** o que foi aplicado
    /// pelo <see cref="ActiveShipConditional.ApplyModifier"/> sai sozinho.
    /// </summary>
    public virtual void OnRaceEnded(ActiveShipConditional active) { }
}
