using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Uma linha da aba de naves: nome, atributos, o poder ativo, a passiva, e o
/// destaque de qual está selecionada.
///
/// O <see cref="ShipSelectMenu"/> clona esta linha uma vez por nave do catálogo,
/// em vez de a montagem criar uma por nave na cena. Nave nova no catálogo aparece
/// no menu sem ninguém rodar ferramenta de novo — a mesma regra da seleção de
/// fase.
/// </summary>
public class ShipSelectRow : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] Image background;
    [SerializeField] Image marker;
    [SerializeField] TMP_Text nameLabel;
    [SerializeField] TMP_Text statsLabel;
    [SerializeField] TMP_Text activeLabel;
    [SerializeField] TMP_Text passiveLabel;

    [Header("Cores")]
    [SerializeField] Color selectedColor = new Color(0.16f, 0.42f, 0.75f, 0.95f);
    [SerializeField] Color idleColor = new Color(0.13f, 0.15f, 0.22f, 0.95f);

    public void Bind(ShipDefinition ship, bool selected, Action<ShipDefinition> onChosen)
    {
        if (ship == null)
            return;

        if (nameLabel != null)
            nameLabel.text = ship.displayName;

        if (marker != null)
        {
            marker.color = ship.color;
            marker.enabled = true;
        }

        if (statsLabel != null)
            statsLabel.text = Describe(ship);

        if (activeLabel != null)
            activeLabel.text = ship.intrinsicAbility != null
                ? $"<b>Ativo</b>  {ship.intrinsicAbility.displayName} — {Activation(ship.intrinsicAbility)}"
                : "<b>Ativo</b>  —";

        if (passiveLabel != null)
        {
            // "Nível 60" e não "—" quando não há passiva: o traço faria parecer
            // que a nave nunca vai ter uma, e o que é verdade é que ela ainda não
            // chegou lá. Ver ShipDefinition.passiveAbility.
            passiveLabel.text = ship.passiveAbility != null
                ? $"<b>Passivo</b>  {ship.passiveAbility.displayName}"
                : "<b>Passivo</b>  destrava no nível 60";
        }

        if (background != null)
            background.color = selected ? selectedColor : idleColor;

        if (button == null)
            return;

        // Religar do zero: a mesma linha é reaproveitada quando a seleção muda, e
        // um listener velho escolheria a nave da rodada anterior.
        button.onClick.RemoveAllListeners();

        // A selecionada continua clicável, e de propósito: um botão que apaga ao
        // ser usado deixa o jogador sem saber se o toque pegou.
        if (onChosen != null)
            button.onClick.AddListener(() => onChosen(ship));
    }

    /// <summary>
    /// Os números que diferenciam uma nave da outra, numa linha. Sem barra e sem
    /// comparação com as outras: enquanto são duas naves, o texto basta — e o dia
    /// de comparar é o dia da tela de oficina, que é outra coisa.
    /// </summary>
    static string Describe(ShipDefinition ship) =>
        $"Vida {ship.MaxHealth:0}   Dano {ship.Damage:0}   " +
        $"Cadência {ship.AttackSpeed:0.#}/s   Aceleração {ship.Acceleration:0.#}";

    /// <summary>Como o poder ativo se recarrega, na linguagem do jogador.</summary>
    static string Activation(ShipAbility ability)
    {
        string duration = ability.lifetime == ShipAbility.Lifetime.PorTempo
            ? $"{ability.durationSeconds:0.#}s"
            : $"{ability.charges} carga(s)";

        if (ability.HasUnlimitedUses)
            return $"{duration}, recarrega em {ability.cooldownSeconds:0}s";

        return ability.usesPerRace == 1
            ? $"{duration}, um uso por corrida"
            : $"{duration}, {ability.usesPerRace} usos por corrida";
    }
}
