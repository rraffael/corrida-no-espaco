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
/// </summary>
public abstract class ShipPassive : ScriptableObject
{
    [Header("Identidade")]
    public string displayName = "Passiva";

    [Tooltip("Uma linha dizendo o que ela faz. É o que a aba de naves mostra.")]
    [TextArea(1, 3)] public string description = "";

    /// <summary>
    /// A corrida começou. É aqui que a passiva aplica o que ela faz — um
    /// <see cref="StatModifier"/>, um <see cref="ShipTrait"/>, o que for.
    /// Diferente do poder ativo, nada disto se desfaz sozinho: quem aplicou
    /// desfaz no <see cref="OnRaceEnded"/>, e na prática a cena recarrega antes.
    /// </summary>
    public virtual void OnRaceStarted(ShipStats ship) { }

    /// <summary>A corrida acabou.</summary>
    public virtual void OnRaceEnded(ShipStats ship) { }
}
