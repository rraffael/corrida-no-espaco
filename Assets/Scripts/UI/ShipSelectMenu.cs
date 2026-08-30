using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A aba de naves do menu: todas as naves do catálogo, com os atributos e os
/// poderes de cada uma, e a escolha de com qual jogar.
///
/// A lista sai do <see cref="ShipCatalog"/> em runtime, e não da cena: nave nova
/// no catálogo aparece aqui sozinha. Esta classe só desenha — quem guarda a
/// escolha é o <see cref="ShipSelection"/>.
///
/// **Todas destravadas, e é assim de propósito por enquanto** *(30/08/2026)*:
/// não existe nível de jogador nem economia, então não há de que destravar. O
/// cadeado desta tela é o dia da loja.
/// </summary>
public class ShipSelectMenu : MonoBehaviour
{
    [Tooltip("Onde as linhas nascem. Tem o VerticalLayoutGroup que as empilha.")]
    [SerializeField] RectTransform rowsRoot;

    [Tooltip("Linha modelo, desligada na cena. É clonada uma vez por nave.")]
    [SerializeField] ShipSelectRow rowTemplate;

    readonly List<ShipSelectRow> rows = new List<ShipSelectRow>();
    readonly List<ShipDefinition> ships = new List<ShipDefinition>();

    bool built;

    void OnEnable()
    {
        Build();
        Refresh();
    }

    /// <summary>Uma vez por sessão: a lista de naves não muda enquanto o jogo roda.</summary>
    void Build()
    {
        if (built)
            return;

        built = true;

        if (rowTemplate == null || rowsRoot == null)
        {
            Debug.LogError("[Naves] Falta a linha modelo ou o contêiner das linhas.", this);
            return;
        }

        rowTemplate.gameObject.SetActive(false);

        var catalog = ShipCatalog.Load();
        if (catalog == null)
            return;

        foreach (var ship in catalog.ships)
        {
            if (ship == null)
                continue;

            ships.Add(ship);

            var row = Instantiate(rowTemplate, rowsRoot);
            row.name = $"Nave {ship.displayName}";
            row.gameObject.SetActive(true);
            rows.Add(row);
        }

        if (ships.Count == 0)
            Debug.LogWarning("[Naves] O catálogo está vazio. A aba abre sem nenhuma nave.", this);
    }

    void Refresh()
    {
        var selected = ShipSelection.Ship;

        for (int i = 0; i < rows.Count; i++)
            rows[i].Bind(ships[i], ships[i] == selected, Choose);
    }

    void Choose(ShipDefinition ship)
    {
        ShipSelection.Choose(ship);

        // Redesenha tudo, e não só as duas linhas que mudaram: são poucas linhas,
        // e "redesenhar o que mudou" é onde nasce a linha que fica destacada por
        // engano depois da terceira nave.
        Refresh();
    }
}
