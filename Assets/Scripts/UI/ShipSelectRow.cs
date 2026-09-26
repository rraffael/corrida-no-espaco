using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Uma linha da aba de naves: nome e nível, atributos no nível atual, os
/// poderes, a passiva, o que o próximo nível dá e o botão de evoluir.
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
    [SerializeField] TMP_Text levelLabel;
    [SerializeField] TMP_Text statsLabel;
    [SerializeField] TMP_Text activeLabel;
    [SerializeField] TMP_Text passiveLabel;

    [Header("Evolução")]
    [SerializeField] TMP_Text nextLabel;

    [Tooltip("O próximo patamar e o que ele dá — upgrade do poder campo a campo, ou a passiva.")]
    [SerializeField] TMP_Text tierLabel;
    [SerializeField] Button evolveButton;
    [SerializeField] TMP_Text evolveLabel;

    [Tooltip("Botões de teste — zerar e ir ao máximo. Só aparecem no Editor e em build de " +
             "desenvolvimento: servem para testar os patamares sem tocar 59 vezes.")]
    [SerializeField] GameObject devButtons;
    [SerializeField] Button devResetButton;
    [SerializeField] Button devMaxButton;

    [Header("Cores")]
    [SerializeField] Color selectedColor = new Color(0.16f, 0.42f, 0.75f, 0.95f);
    [SerializeField] Color idleColor = new Color(0.13f, 0.15f, 0.22f, 0.95f);

    /// <param name="onChanged">Chamado quando o nível mudou — a aba redesenha tudo, saldo junto.</param>
    public void Bind(ShipDefinition ship, bool selected, Action<ShipDefinition> onChosen, Action onChanged)
    {
        if (ship == null)
            return;

        int level = ShipProgress.Level(ship);
        int max = ShipEvolution.MaxLevel(ship);

        if (nameLabel != null)
            nameLabel.text = ship.displayName;

        if (levelLabel != null)
            levelLabel.text = $"Nv {level}/{max}  ·  Patamar {ShipEvolution.Tier(ship, level)}";

        if (marker != null)
        {
            marker.color = ship.color;
            marker.enabled = true;
        }

        if (statsLabel != null)
            statsLabel.text = Describe(ship, level);

        if (activeLabel != null)
            activeLabel.text = Powers(ship, level);

        if (passiveLabel != null)
            passiveLabel.text = Passive(ship, level);

        BindEvolution(ship, level, max, onChanged);

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

    void BindEvolution(ShipDefinition ship, int level, int max, Action onChanged)
    {
        bool maxed = level >= max;

        if (nextLabel != null)
        {
            nextLabel.text = maxed
                ? "Nível máximo"
                : $"Próximo nível: {ShipEvolution.DescribeLevel(ship, level + 1)}";
        }

        if (tierLabel != null)
        {
            string tier = ShipEvolution.DescribeNextTier(ship, level);
            tierLabel.text = string.IsNullOrEmpty(tier)
                ? "<b>Patamares</b>  todos alcançados"
                : $"<b>Próximo patamar</b>  {tier}";
        }

        if (evolveButton != null)
        {
            evolveButton.onClick.RemoveAllListeners();
            evolveButton.gameObject.SetActive(!maxed);
            evolveButton.interactable = ShipProgress.CanEvolve(ship);
            evolveButton.onClick.AddListener(() =>
            {
                if (ShipProgress.TryEvolve(ship))
                    onChanged?.Invoke();
            });
        }

        if (evolveLabel != null && !maxed)
            evolveLabel.text = $"Evoluir  ·  {ShipProgress.NextCost(ship)} {Wallet.CurrencyName}";

        bool dev = Application.isEditor || Debug.isDebugBuild;
        if (devButtons != null)
            devButtons.SetActive(dev);

        if (!dev)
            return;

        Wire(devResetButton, () => ShipProgress.SetLevel(ship, 1), onChanged);
        Wire(devMaxButton, () => ShipProgress.SetLevel(ship, max), onChanged);
    }

    static void Wire(Button target, Action action, Action onChanged)
    {
        if (target == null)
            return;

        target.onClick.RemoveAllListeners();
        target.onClick.AddListener(() =>
        {
            action();
            onChanged?.Invoke();
        });
    }

    /// <summary>
    /// Os atributos **no nível atual**, em duas linhas: resistência e tiro em
    /// cima, velocidade embaixo. Mostra o número que vale na corrida, e não o da
    /// ficha — o que o jogador quer saber é o que a nave dele faz agora.
    /// </summary>
    static string Describe(ShipDefinition ship, int level)
    {
        string V(ShipStat stat) => ShipEvolution.Format(stat, ShipEvolution.Value(ship, stat, level));

        return $"Vida {V(ShipStat.MaxHealth)}   Defesa {V(ShipStat.DefensePercent)}   " +
               $"Dano {V(ShipStat.Damage)}   Cadência {V(ShipStat.AttackSpeed)}\n" +
               $"Cruzeiro {V(ShipStat.CruiseSpeed)}   Aceleração {V(ShipStat.Acceleration)}   " +
               $"Abate +{V(ShipStat.KillSpeedGain)}";
    }

    /// <summary>
    /// O ativo e o condicional, na versão do patamar atual. Nave com só um mostra
    /// só ele: "Ativo —" ao lado de um condicional faria parecer que a nave não
    /// tem poder.
    /// </summary>
    static string Powers(ShipDefinition ship, int level)
    {
        var ability = ShipEvolution.Ability(ship, level);
        string active = ability != null
            ? $"<b>Ativo</b>  {ability.displayName} — {Activation(ability)}"
            : null;

        var conditional = ShipEvolution.Conditional(ship, level);
        string triggered = conditional != null
            ? $"<b>Condicional</b>  {conditional.displayName} — {conditional.trigger}"
            : null;

        if (active != null && triggered != null)
            return active + "\n" + triggered;

        return active ?? triggered ?? "<b>Ativo</b>  —";
    }

    /// <summary>
    /// A passiva: ligada, trancada com o nível que a abre, ou ainda por desenhar.
    /// "A definir" e não "—": o traço faria parecer que a nave nunca vai ter uma.
    /// </summary>
    static string Passive(ShipDefinition ship, int level)
    {
        int at = ShipEvolution.Model(ship).passiveLevel;

        if (ship.passiveAbility == null)
            return $"<b>Passivo</b>  a definir (nível {at})";

        return ShipEvolution.Passive(ship, level) != null
            ? $"<b>Passivo</b>  {ship.passiveAbility.displayName}"
            : $"<b>Passivo</b>  {ship.passiveAbility.displayName} — destrava no nível {at}";
    }

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
